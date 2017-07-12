using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using TreeSharp;
using Action = System.Action;
using System.Runtime.InteropServices;

namespace ArtemisLoader
{
    public class ArtemisLoader : BotBase
    {
        private const string ProjectName = "Artemis";

        private const string ProjectMainType = "Artemis.Artemis";

        private const string ProjectAssemblyName = "Artemis.dll";

        private const string VersionUrl = "https://github.com/sodimm/RebornBuddy/blob/master/BotBases/Artemis/version.txt?raw=true";

        private const string ZipUrl = "https://github.com/sodimm/RebornBuddy/blob/master/BotBases/Artemis/Artemis.zip?raw=true";

        private static readonly Color _logColor = Colors.Cyan;

        public override PulseFlags PulseFlags => PulseFlags.All;
        public override bool IsAutonomous => true;
        public override bool WantButton => false;
        public override bool RequiresProfile => false;

        #region Meta Data

        // Don't touch anything else below from here!
        private static readonly object Locker = new object();
        private static readonly string VersionPath = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\version.txt");
        private static readonly string ProjectAssembly = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string GreyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static readonly string ProjectDirectory = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}");
        private static readonly string ProjectTypeFolder = Path.Combine(Environment.CurrentDirectory, @"BotBases");
        private static bool _updated;

        private static readonly Composite FailsafeRoot = new TreeSharp.Action(c =>
        {
            Log($"{ProjectName} is not loaded correctly.");
            TreeRoot.Stop();
        });

        #endregion

        public ArtemisLoader()
        {
            lock (Locker)
            {
                if (_updated) { return; }
                _updated = true;
            }

            Task.Factory.StartNew(Update);
        }

        #region Overrides

        public override string Name => "Artemis";

        public override Composite Root
        {
            get
            {
                if (Project == null) { Load(); }
                return Project != null ? (Composite)RootFunc.Invoke(Project, null) : FailsafeRoot;
            }
        }

        public override void OnButtonPress() => ButtonFunc?.Invoke(Project, null);

        public override void Start() => StartFunc?.Invoke(Project, null);

        public override void Stop() => StopFunc?.Invoke(Project, null);

        #endregion

        #region Injections

        static object Project;
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
                Project = Activator.CreateInstance(baseType);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }

            if (Project == null)
            {
                Log("[Error] Could not load main type.");
                return;
            }

            StartFunc = Project.GetType().GetMethod("Start");
            StopFunc = Project.GetType().GetMethod("Stop");
            ButtonFunc = Project.GetType().GetMethod("OnButtonPress");
            RootFunc = Project.GetType().GetMethod("GetRoot");
            InitFunc = Project.GetType().GetMethod("Initialize", new[] { typeof(int) });
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

        #region Automatic Updates

        private static async Task Update()
        {
            var local = GetLocalVersion();
            var data = await TryUpdate(local);
            if (data == null) { return; }

            try { Clean(ProjectDirectory); }
            catch (Exception e) { Log(e.ToString()); }

            try { Extract(data, ProjectTypeFolder); }
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

        private static string GetLocalVersion()
        {
            if (!File.Exists(VersionPath)) { return null; }
            try
            {
                var version = File.ReadAllText(VersionPath);
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
                    var version = await client.GetStringAsync(VersionUrl);
                    if (string.IsNullOrEmpty(version) || version == localVersion) { return null; }

                    Log($"Local: {localVersion} | Latest: {version}");
                    using (var response = await client.GetAsync(ZipUrl))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            Log($"[Error] Could not download {ProjectName}: {response.StatusCode}");
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

        #endregion

        #region Helpers

        private static void Log(string message)
        {
            message = $"[{ProjectName}] {message}";
            Logging.Write(_logColor, message);
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
