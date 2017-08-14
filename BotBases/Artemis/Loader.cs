using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;

namespace ArtemisLoader
{
    public class ArtemisLoader : BotBase
    {
        private const string ProjectName = "Artemis";

        private const string ProjectMainType = "Artemis.Artemis";

        private const string ProjectAssemblyName = "Artemis.dll";

        private const string VersionUrl = "https://github.com/sodimm/RebornBuddy/blob/master/Downloads/Latest/Artemis/version.txt?raw=true";

        private const string ZipUrl = "https://github.com/sodimm/RebornBuddy/blob/master/Downloads/Latest/Artemis/Artemis.zip?raw=true";

        private static readonly Color LogColor = Colors.Cyan;
        public override PulseFlags PulseFlags => PulseFlags.All;
        public override bool IsAutonomous => true;
        public override bool WantButton => true;
        public override bool RequiresProfile => false;

        #region Meta Data

        // Don't touch anything else below from here!
        private static readonly object Locker = new object();
        private static readonly string ProjectAssembly = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string GreyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static readonly string VersionPath = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\version.txt");
        private static readonly string BaseDir = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}");
        private static readonly string ProjectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"BotBases");
        private static bool _updated;
        private static readonly Composite FailsafeRoot = new TreeSharp.Action(c =>
        {
            Log($"{ProjectName} is not loaded correctly.");
            TreeRoot.Stop();
        });

        #endregion

        #region Constructor

        public ArtemisLoader()
        {
            lock (Locker)
            {
                if (_updated) { return; }
                _updated = true;
            }

            Task.Factory.StartNew(Update);
        }

        #endregion

        #region Overrides

        public override string Name => "Artemis";

        public override Composite Root
        {
            get
            {
                if (Plugin == null) { Load(); }
                return Plugin != null ? (Composite)RootFunc.Invoke(Plugin, null) : FailsafeRoot;
            }
        }

        public override void OnButtonPress() => ButtonFunc?.Invoke(Plugin, null);

        public override void Start() => StartFunc?.Invoke(Plugin, null);

        public override void Stop() => StopFunc?.Invoke(Plugin, null);

        #endregion

        #region Injections

        static object Plugin;
        private static MethodInfo StartFunc { get; set; }
        private static MethodInfo StopFunc { get; set; }
        private static MethodInfo ButtonFunc { get; set; }
        private static MethodInfo RootFunc { get; set; }
        private static MethodInfo InitFunc { get; set; }

        #endregion

        #region Injection Methods

        private static void Load()
        {
            RedirectAssembly();

            var assembly = LoadAssembly(ProjectAssembly);
            if (assembly == null)
            {
                return;
            }

            Type baseType;
            try
            {
                baseType = assembly.GetType(ProjectMainType);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }

            try
            {
                Plugin = Activator.CreateInstance(baseType);
            }
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

            StartFunc = Plugin.GetType().GetMethod("Start");
            StopFunc = Plugin.GetType().GetMethod("Stop");
            ButtonFunc = Plugin.GetType().GetMethod("OnButtonPress");
            RootFunc = Plugin.GetType().GetMethod("get_Root");
            InitFunc = Plugin.GetType().GetMethod("Initialize", new[] { typeof(int) });
            if (InitFunc != null)
            {
                Log($"{ProjectName}64 loaded.");
                InitFunc.Invoke(Plugin, new[] {(object)2});
            }

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

        #endregion

        #region Automatic Update Methods

        private static void Update()
        {
            var stopwatch = Stopwatch.StartNew();
            var local = GetLocalVersion();
            var responseMessage = GetLatestVersion().Result;
            var latest = responseMessage;

            if (local == latest || latest == null)
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

            if (!Clean(BaseDir))
            {
                Log("[Error] Could not clean directory for update.");
                return;
            }

            if (!Extract(bytes, ProjectTypeFolder))
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

        #endregion

        #region Helpers

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
                return requestedAssembly.Name != "GreyMagic" ? null : Assembly.LoadFrom(GreyMagicAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += greyMagicHandler;

        }
        #endregion

    }
}