using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media;
using ff14bot.AClasses;
using ff14bot.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Clio.Utilities;

namespace SparrowLoader
{
    public class SparrowLoader : BotPlugin
    {
        private const string ProjectName = "Sparrow";

        private const string ProjectMainType = "Sparrow.Sparrow";

        private const string ProjectAssemblyName = "Sparrow.dll";

        private const string VersionUrl = "https://github.com/sodimm/RebornBuddy/blob/master/Downloads/Latest/Sparrow/version.txt?raw=true";

        private const string ZipUrl = "https://github.com/sodimm/RebornBuddy/blob/master/Downloads/Latest/Sparrow/Sparrow.zip?raw=true";

        private static readonly Color LogColor = Colors.Cyan;

        public override string Description => "Everything Chocobo";

        public override string Author => "Sodimm";

        public override string ButtonText => "Settings";

        public override Version Version => new Version(1, 2, 3, 1);

        public override bool WantButton => true;

        private static readonly object locker = new object();
        private static readonly string projectAssembly = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string greyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static readonly string VersionPath = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}\version.txt");
        private static readonly string baseDir = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}");
        private static readonly string projectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"Plugins");
        private static Action onInitialize, onShutdown, onEnabled, onDisabled, onButtonPress, onPulse;
        private static bool updated;

        private static string CompiledAssembliesPath => Path.Combine(Utilities.AssemblyDirectory, "CompiledAssemblies");


        public object Plugin { get; set; }

        #region Overrides

        public override string Name => "Sparrow";

        public override void OnInitialize() => onInitialize?.Invoke();
        public override void OnShutdown() => onShutdown?.Invoke();
        public override void OnEnabled() => onEnabled?.Invoke();
        public override void OnDisabled() => onDisabled?.Invoke();
        public override void OnButtonPress() => onButtonPress?.Invoke();
        public override void OnPulse() => onPulse?.Invoke();

        #endregion

        public SparrowLoader()
        {
            lock (locker)
            {
                if (updated) { return; }
                updated = true;
            }

            Task.Factory.StartNew(Update);
        }

        private void Load()
        {
            Log("Loading...");

            RedirectAssembly();

            var assembly = LoadAssembly(projectAssembly);
            if (assembly == null) { return; }

            Type baseType;
            try { baseType = assembly.GetType(ProjectMainType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }

            try { Plugin = Activator.CreateInstance(baseType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }

            if (Plugin == null)
            {
                Log("[Error] Could not load main type.");
                return;
            }

            var type = Plugin.GetType();
            onInitialize = (Action)type.GetProperty("OnInitialize").GetValue(Plugin);
            onShutdown = (Action)type.GetProperty("OnShutdown").GetValue(Plugin);
            onEnabled = (Action)type.GetProperty("OnEnabled").GetValue(Plugin);
            onDisabled = (Action)type.GetProperty("OnDisabled").GetValue(Plugin);
            onButtonPress = (Action)type.GetProperty("OnButtonPress").GetValue(Plugin);
            onPulse = (Action)type.GetProperty("OnPulseAction").GetValue(Plugin);

            Log($"{ProjectName} loaded.");
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(string name);

        public static bool Unblock(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }

        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path))
            {
                Log($"Could not find Assembly: {path}");
                return null;
            }

            try
            {
                Unblock(path);
            }
            catch (Exception)
            {
                // ignored
            }
            Assembly assembly = null;
            try { assembly = Assembly.LoadFrom(path); }
            catch (Exception e) { Logging.WriteException(e); }

            return assembly;
        }

        private void Update()
        {
            var stopwatch = Stopwatch.StartNew();
            var local = GetLocalVersion();
            var responseMessage = GetLatestVersion().Result;
            var latest = responseMessage;

            if (latest == null || local == latest)
            {
                Load();
                return;
            }

            Log($"Updating to {latest}.");
            var bytes = DownloadLatest(latest).Result;

            if (bytes == null || bytes.Length == 0)
            {
                Log("[Error] Bad product data returned.");
                return;
            }

            if (!Clean(baseDir))
            {
                Log("[Error] Could not clean directory for update.");
                return;
            }

            if (!Extract(bytes, projectTypeFolder))
            {
                Log("[Error] Could not extract new files.");
                return;
            }

            if (File.Exists(VersionPath)) { File.Delete(VersionPath); }
            try { File.WriteAllText(VersionPath, latest); }
            catch (Exception e) { Log(e.ToString()); }

            stopwatch.Stop();
            Log($"Update complete in {stopwatch.ElapsedMilliseconds} ms.");
            Load();
        }

        private static string GetLocalVersion()
        {
            if (!File.Exists(VersionPath)) { return null; }
            try
            {
                return File.ReadAllText(VersionPath);
            }
            catch { return null; }
        }

        private static bool Clean(string directory)
        {
            foreach (var file in new DirectoryInfo(directory).GetFiles())
            {
                try { file.Delete(); }
                catch { return false; }
            }

            foreach (var dir in new DirectoryInfo(directory).GetDirectories())
            {
                try { dir.Delete(true); }
                catch { return false; }
            }

            return true;
        }

        private static bool Extract(byte[] files, string directory)
        {
            using (var stream = new MemoryStream(files))
            {
                var zip = new FastZip();

                try { zip.ExtractZip(stream, directory, FastZip.Overwrite.Always, null, null, null, false, true); }
                catch (Exception e)
                {
                    Log(e.ToString());
                    return false;
                }
            }

            return true;
        }

        private static async Task<string> GetLatestVersion()
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response;
                try { response = await client.GetAsync(VersionUrl); }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }
                if (!response.IsSuccessStatusCode)
                    return null;

                string responseMessageBytes;
                try { responseMessageBytes = await response.Content.ReadAsStringAsync(); }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }
                return responseMessageBytes;
            }
        }

        private static async Task<byte[]> DownloadLatest(string version)
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(ZipUrl);
                }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }
                if (!response.IsSuccessStatusCode)
                    return null;

                byte[] responseMessageBytes;
                try
                {
                    responseMessageBytes = await response.Content.ReadAsByteArrayAsync();
                }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }

                return responseMessageBytes;
            }
        }

        private static void Log(string message)
        {
            Logging.Write(LogColor, $"[Auto-Updater][{ProjectName}] {message}");
        }

        public static void RedirectAssembly()
        {
            ResolveEventHandler handler = (sender, args) =>
            {
                var name = Assembly.GetEntryAssembly().GetName().Name;
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != name ? null : Assembly.GetEntryAssembly();
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;

            ResolveEventHandler greyMagicHandler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != "GreyMagic" ? null : Assembly.LoadFrom(greyMagicAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += greyMagicHandler;
        }
    }
}
