using Clio.Utilities;
using ff14bot.AClasses;
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

namespace Loader
{
    public class SparrowLoader : BotPlugin
    {
        private const string ProjectName = "Sparrow";

        private const string ProjectMainType = "Sparrow.Sparrow";

        private const string ProjectAssemblyName = "Sparrow.dll";

        private const string VersionUrl = "https://github.com/sodimm/RebornBuddy/blob/master/Downloads/Latest/Sparrow/version.txt?raw=true";

        private const string ZipUrl = "https://github.com/sodimm/RebornBuddy/blob/master/Downloads/Latest/Sparrow/Sparrow.zip?raw=true";

        private static readonly Color LogColor = Colors.Cyan;

        #region Meta Data

        private static readonly object Locker = new object();
        private static readonly string ProjectAssembly = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string GreyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static readonly string VersionPath = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}\version.txt");
        private static readonly string BaseDir = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{ProjectName}");
        private static readonly string ProjectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"Plugins");
        private static bool _updaterStarted, _updaterFinished, _loaded;

        protected SparrowLoader()
        {
            if (_updaterStarted) { return; }
            _updaterStarted = true;

            Task.Factory.StartNew(AutoUpdate);
        }

        #endregion

        #region MethodInfo

        private static object Plugin { get; set; }
        private static MethodInfo InitFunc { get; set; }
        private static MethodInfo ButtonFunc { get; set; }
        private static MethodInfo PulseFunc { get; set; }
        private static MethodInfo EnabledFunc { get; set; }
        private static MethodInfo DisabledFunc { get; set; }
        private static MethodInfo ShutDownFunc { get; set; }

        #endregion

        #region Overrides

        public override string Name => ProjectName;

        public override string Description => "Chocobo Manager.";

        public override string Author => "Sodimm";

        public override string ButtonText => "Settings";

        public override Version Version => new Version(1, 2, 3, 2);

        public override bool WantButton => true;

        public override void OnInitialize()
        {
            if (!_loaded && Plugin == null && _updaterFinished)
            {
                LoadPlugin();
            }

            if (Plugin != null)
            {
                InitFunc.Invoke(Plugin, null);
            }
        }

        public override void OnButtonPress()
        {
            if (!_loaded && Plugin == null && _updaterFinished)
            {
                LoadPlugin();
            }

            if (Plugin != null)
            {
                ButtonFunc.Invoke(Plugin, null);
            }
        }

        public override void OnPulse()
        {
            if (!_loaded && Plugin == null && _updaterFinished)
            {
                LoadPlugin();
            }

            if (Plugin != null)
            {
                PulseFunc.Invoke(Plugin, null);
            }
        }

        public override void OnEnabled()
        {
            if (!_loaded && Plugin == null && _updaterFinished)
            {
                LoadPlugin();
            }

            if (Plugin != null)
            {
                EnabledFunc.Invoke(Plugin, null);
            }
        }

        public override void OnDisabled()
        {
            if (!_loaded && Plugin == null && _updaterFinished)
            {
                LoadPlugin();
            }

            if (Plugin != null)
            {
                DisabledFunc.Invoke(Plugin, null);
            }
        }

        public override void OnShutdown()
        {
            if (!_loaded && Plugin == null && _updaterFinished)
            {
                LoadPlugin();
            }

            if (Plugin != null)
            {
                ShutDownFunc.Invoke(Plugin, null);
            }
        }

        #endregion

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(string name);

        public static bool Unblock(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }

        public static void RedirectAssembly()
        {
            ResolveEventHandler handler = (sender, args) =>
            {
                string name = Assembly.GetEntryAssembly().GetName().Name;
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

        private static string CompiledAssembliesPath => Path.Combine(Utilities.AssemblyDirectory, "CompiledAssemblies");
        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            if (!Directory.Exists(CompiledAssembliesPath))
            {
                Directory.CreateDirectory(CompiledAssembliesPath);
            }

            var t = DateTime.Now.Ticks;
            var name = $"{Path.GetFileNameWithoutExtension(path)}{t}.{Path.GetExtension(path)}";
            var pdbPath = path.Replace(Path.GetExtension(path), "pdb");
            var pdb = $"{Path.GetFileNameWithoutExtension(path)}{t}.pdb";
            var capath = Path.Combine(CompiledAssembliesPath, name);

            if (File.Exists(capath))
            {
                try
                {
                    File.Delete(capath);
                }
                catch (Exception)
                {
                    //
                }
            }

            if (File.Exists(pdb))
            {
                try
                {
                    File.Delete(pdb);
                }
                catch (Exception)
                {
                    //
                }
            }

            if (!File.Exists(capath))
            {
                File.Copy(path, capath);
            }

            if (!File.Exists(pdb) && File.Exists(pdbPath))
            {
                File.Copy(pdbPath, pdb);
            }

            Assembly assembly = null;
            Unblock(path);

            try
            {
                assembly = Assembly.LoadFrom(path);
            }
            catch (Exception e)
            {
                Logging.WriteException(e);
            }

            return assembly;
        }

        private static object Load()
        {
            RedirectAssembly();

            var assembly = LoadAssembly(ProjectAssembly);

            if (assembly == null)
            {
                return null;
            }

            Type baseType;
            try
            {
                baseType = assembly.GetType(ProjectMainType);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            object bb;
            try
            {
                bb = Activator.CreateInstance(baseType);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            if (bb != null)
            {
                Log($"{ProjectName} was loaded successfully.");
            }
            else
            {
                Log($"Could not load {ProjectName} This can be due to a new version of Rebornbuddy being released. An update should be ready soon.");
            }
            return bb;
        }

        private static void LoadPlugin()
        {
            lock (Locker)
            {
                if (Plugin != null)
                {
                    return;
                }

                Plugin = Load();
                _loaded = true;

                if (Plugin == null)
                {
                    return;
                }

                PulseFunc = Plugin.GetType().GetMethod("OnPulse");
                EnabledFunc = Plugin.GetType().GetMethod("OnEnabled");
                DisabledFunc = Plugin.GetType().GetMethod("OnDisabled");
                ShutDownFunc = Plugin.GetType().GetMethod("ShutDown");
                ButtonFunc = Plugin.GetType().GetMethod("OnButtonPress");
                InitFunc = Plugin.GetType().GetMethod("OnInitialize", new[] { typeof(int) });

                if (InitFunc != null)
                {
                    Log($"{ProjectName} loaded.");
                    InitFunc.Invoke(Plugin, new[] { (object)1 });
                }
            }
        }

        private static void Log(string message)
        {
            Logging.Write(LogColor, $"[Auto-Updater][{ProjectName}] {message}");
        }

        private static string GetLocalVersion()
        {
            if (!File.Exists(VersionPath))
            {
                return null;
            }
            try
            {
                var version = File.ReadAllText(VersionPath);
                return version;
            }
            catch
            {
                return null;
            }
        }

        private static void AutoUpdate()
        {
            var stopwatch = Stopwatch.StartNew();
            var local = GetLocalVersion();
            var responseMessage = GetLatestVersion().Result;
            var latest = responseMessage;

            if (local == latest || latest == null)
            {
                _updaterFinished = true;
                LoadPlugin();
                return;
            }

            Log($"Updating to {latest}.");
            var bytes = DownloadLatest(latest).Result;
            if (bytes == null || bytes.Length == 0)
            {
                return;
            }

            if (!Clean(BaseDir))
            {
                Log("Could not clean directory for update.");
                _updaterFinished = true;
                return;
            }

            Log("Extracting new files.");
            if (!Extract(bytes, ProjectTypeFolder))
            {
                Log("Could not extract new files.");
                _updaterFinished = true;
                return;
            }

            if (File.Exists(VersionPath))
            {
                File.Delete(VersionPath);
            }
            try
            {
                File.WriteAllText(VersionPath, latest);
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }

            stopwatch.Stop();
            Log($"Update complete in {stopwatch.ElapsedMilliseconds} ms.");
            _updaterFinished = true;
            LoadPlugin();
        }

        private static bool Clean(string directory)
        {
            foreach (var file in new DirectoryInfo(directory).GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    return false;
                }
            }

            foreach (var dir in new DirectoryInfo(directory).GetDirectories())
            {
                try
                {
                    dir.Delete(true);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Extract(byte[] files, string directory)
        {
            using (var stream = new MemoryStream(files))
            {
                var zip = new FastZip();
                try
                {
                    zip.ExtractZip(stream, directory, FastZip.Overwrite.Always, null, null, null, false, true);
                }
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
                try
                {
                    response = await client.GetAsync(VersionUrl);
                }
                catch (Exception e)
                {
                    Log(e.Message);
                    return null;
                }
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string responseMessageBytes;
                try
                {
                    responseMessageBytes = await response.Content.ReadAsStringAsync();
                }
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
                {
                    return null;
                }

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
    }
}
