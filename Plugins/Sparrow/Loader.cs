using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using ff14bot.AClasses;
using ff14bot.Helpers;
using ff14bot.Managers;
using ICSharpCode.SharpZipLib.Zip;
using Action = System.Action;

namespace Sparrow
{
    public class Loader : BotPlugin
    {
        private const string PROJECT_NAME = "Sparrow";
        private const string PROJECT_MAIN_TYPE = "Sparrow.Sparrow";
        private const string PROJECT_ASSEMBLY_NAME = "Sparrow.dll";
        private const string VERSION_URL = "https://github.com/sodimm/RebornBuddy/blob/master/Downloads/Latest/Sparrow/version.txt?raw=true";
        private const string DATA_URL = "https://github.com/sodimm/RebornBuddy/blob/master/Downloads/Latest/Sparrow/Sparrow.zip?raw=true";
        private static readonly Color _logColor = Colors.Cyan;

        public override bool WantButton => true;

        private static readonly object _locker = new object();
        private static readonly string _versionPath = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{PROJECT_NAME}\version.txt");
        private static readonly string _projectAssembly = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{PROJECT_NAME}\{PROJECT_ASSEMBLY_NAME}");
        private static readonly string _greyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static readonly string _projectDir = Path.Combine(Environment.CurrentDirectory, $@"Plugins\{PROJECT_NAME}");
        private static readonly string _projectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"Plugins");
        private static bool _updated;
        private static Action _onButtonPress, _onDisabled, _onEnabled, _onShutdown, _onPulse;

        public object Plugin { get; set; }

        public Loader()
        {
            lock (_locker)
            {
                if (_updated) { return; }
                _updated = true;
            }

            var dispatcher = Dispatcher.CurrentDispatcher;
            Task.Run(async () => { await Update(); Load(dispatcher); });
        }

        public override string Name => PROJECT_NAME;
        public override string Author => "Sodimm";
        public override string ButtonText => "Settings";
        public override Version Version => new Version(1, 2, 3, 3);
        public override string Description => "Chocobo Manager.";

        public override void OnButtonPress() => _onButtonPress?.Invoke();
        public override void OnDisabled() => _onDisabled?.Invoke();
        public override void OnEnabled() => _onEnabled?.Invoke();
        public override void OnShutdown() => _onShutdown?.Invoke();
        public override void OnPulse() => _onPulse?.Invoke();

        private void Load(Dispatcher dispatcher)
        {
            RedirectAssembly();

            var assembly = LoadAssembly(_projectAssembly);
            if (assembly == null) { return; }

            Type baseType;
            try { baseType = assembly.GetType(PROJECT_MAIN_TYPE); }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }

            dispatcher.BeginInvoke(new Action(() =>
            {
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
                _onShutdown = (Action)type.GetProperty("OnShutdownAction")?.GetValue(Plugin);
                _onPulse = (Action)type.GetProperty("OnPulseAction")?.GetValue(Plugin);
                _onButtonPress = (Action)type.GetProperty("OnButtonPressAction")?.GetValue(Plugin);
                _onEnabled = (Action)type.GetProperty("OnEnabledAction")?.GetValue(Plugin);
                _onDisabled = (Action)type.GetProperty("OnDisabledAction")?.GetValue(Plugin);

                var version = GetLocalVersion();
                Log($"Loaded.");

                if (PluginManager.GetEnabledPlugins().Any(p => p == PROJECT_NAME)) { _onEnabled?.Invoke(); }
            }));
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
                return requestedAssembly.Name != "GreyMagic" ? null : Assembly.LoadFrom(_greyMagicAssembly);
            };

            AppDomain.CurrentDomain.AssemblyResolve += greyMagicHandler;
        }

        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path)) { return null; }

            Assembly assembly = null;
            try { assembly = Assembly.LoadFrom(path); }
            catch (Exception e) { Logging.WriteException(e); }

            return assembly;
        }

        private static async Task Update()
        {
            var local = GetLocalVersion();
            var data = await TryUpdate(local);
            if (data == null) { return; }

            try { Clean(_projectDir); }
            catch (Exception e) { Log(e.ToString()); }

            try { Extract(data, _projectTypeFolder); }
            catch (Exception e) { Log(e.ToString()); }
        }

        private static void Extract(byte[] files, string directory)
        {
            using (var stream = new MemoryStream(files))
            {
                var zip = new FastZip();
                zip.ExtractZip(stream, directory, FastZip.Overwrite.Always, null, null, null, false, true);
            }
        }

        private static void Clean(string directory)
        {
            foreach (var file in new DirectoryInfo(directory).GetFiles())
            {
                file.Delete();
            }

            foreach (var dir in new DirectoryInfo(directory).GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private static void Log(string message)
        {
            Logging.Write(_logColor, $"[{PROJECT_NAME}] {message}");
        }

        private static string GetLocalVersion()
        {
            if (!File.Exists(_versionPath)) { return null; }
            try
            {
                var version = File.ReadAllText(_versionPath);
                return version;
            }
            catch { return null; }
        }

        public static async Task<byte[]> TryUpdate(string localVersion)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var stopwatch = Stopwatch.StartNew();
                    var version = await client.GetStringAsync(VERSION_URL);
                    if (string.IsNullOrEmpty(version) || version == localVersion) { return null; }

                    Log($"Local: {localVersion} | Latest: {version}");
                    using (var response = await client.GetAsync(DATA_URL))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            Log($"[Error] Could not download {PROJECT_NAME}: {response.StatusCode}");
                            return null;
                        }

                        using (var inputStream = await response.Content.ReadAsStreamAsync())
                        using (var memoryStream = new MemoryStream())
                        {
                            await inputStream.CopyToAsync(memoryStream);

                            stopwatch.Stop();
                            Log($"Download took {stopwatch.ElapsedMilliseconds} ms.");

                            return memoryStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log($"[Error] {e}");
                return null;
            }
        }
    }
}
