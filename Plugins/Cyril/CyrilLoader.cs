using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using ff14bot.AClasses;
using ff14bot.Helpers;
using ff14bot.Managers;

namespace Loader
{
    public class CyrilLoader : BotPlugin
    {
        #region Meta Data

        private const string PluginClass = "Cyril.CyrilPlugin";
        private static readonly string PluginAssembly = Path.Combine(Environment.CurrentDirectory, @"Plugins\Cyril\Cyril.dll");
        private static readonly object ObjLock = new object();
        private static volatile bool _loaded;

        #endregion

        #region Overrides

        public override void OnInitialize()
        {
            if (!_loaded && Plugin == null)
            {
                LoadPlugin();
            }
            if (Plugin != null)
            {
                InjectedOnInitialize?.Invoke(Plugin, null);
            }
        }

        public override void OnEnabled()
        {
            if (!_loaded && Plugin == null)
            {
                LoadPlugin();
            }
            if (Plugin != null)
            {
                InjectedOnEnabled?.Invoke(Plugin, null);
            }
        }

        public override void OnDisabled()
        {
            if (!_loaded && Plugin == null)
            {
                LoadPlugin();
            }
            if (Plugin != null)
            {
                InjectedOnDisabled?.Invoke(Plugin, null);
            }
        }

        public override void OnButtonPress()
        {
            if (!_loaded && Plugin == null)
            {
                LoadPlugin();
            }
            if (Plugin != null)
            {
                InjectedOnButtonPress?.Invoke(Plugin, null);
            }
        }

        public override string Author
        {
            get
            {
                if (!_loaded && Plugin == null)
                {
                    LoadPlugin();
                }
                if (Plugin != null && InjectedAuthor != null)
                {
                    return (string)InjectedAuthor.GetValue(Plugin);
                }
                return string.Empty;
            }
        }

        public override Version Version
        {
            get
            {
                if (!_loaded && Plugin == null)
                {
                    LoadPlugin();
                }
                if (Plugin != null && InjectedVersion != null)
                {
                    return (Version)InjectedVersion.GetValue(Plugin);
                }
                return new Version();
            }
        }

        public override string Name
        {
            get
            {
                if (!_loaded && Plugin == null)
                {
                    LoadPlugin();
                }
                if (Plugin != null && InjectedName != null)
                {
                    return (string)InjectedName.GetValue(Plugin);
                }
                return string.Empty;
            }
        }

        public override bool WantButton
        {
            get
            {
                if (!_loaded && Plugin == null)
                {
                    LoadPlugin();
                }
                if (Plugin != null && InjectedWantButton != null)
                {
                    return (bool)InjectedWantButton.GetValue(Plugin);
                }
                return true;
            }
        }

        public override string ButtonText
        {
            get
            {
                if (!_loaded && Plugin == null)
                {
                    LoadPlugin();
                }
                if (Plugin != null && InjectedButtonText != null)
                {
                    return (string)InjectedButtonText.GetValue(Plugin);
                }
                return string.Empty;
            }
        }

        #endregion

        #region Injections

        private static object Plugin { get; set; }
        private static MethodInfo InjectedOnInitialize { get; set; }
        private static MethodInfo InjectedOnEnabled { get; set; }
        private static MethodInfo InjectedOnDisabled { get; set; }
        private static MethodInfo InjectedOnButtonPress { get; set; }
        private static PropertyInfo InjectedAuthor { get; set; }
        private static PropertyInfo InjectedVersion { get; set; }
        private static PropertyInfo InjectedName { get; set; }
        private static PropertyInfo InjectedWantButton { get; set; }
        private static PropertyInfo InjectedButtonText { get; set; }

        #endregion

        #region Inject Methods

        private static object Load()
        {
            RedirectAssembly();

            var assembly = LoadAssembly(PluginAssembly);
            if (assembly == null)
            {
                return null;
            }

            Type baseType;
            try
            {
                baseType = assembly.GetType(PluginClass);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            object plugin;
            try
            {
                plugin = Activator.CreateInstance(baseType);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            Log(plugin != null
                ? "Loaded successfully."
                : "Could not load. This can be due to a new version of Rebornbuddy being released. An update should be ready soon.");

            return plugin;
        }

        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            Assembly assembly = null;
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

        private static void LoadPlugin()
        {
            lock (ObjLock)
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

                InjectedOnInitialize = Plugin.GetType().GetMethod("OnInitialize");
                InjectedOnEnabled = Plugin.GetType().GetMethod("OnEnabled");
                InjectedOnDisabled = Plugin.GetType().GetMethod("OnDisabled");
                InjectedOnButtonPress = Plugin.GetType().GetMethod("OnButtonPress");
                InjectedAuthor = Plugin.GetType().GetProperty("Author");
                InjectedVersion = Plugin.GetType().GetProperty("Version");
                InjectedName = Plugin.GetType().GetProperty("Name");
                InjectedWantButton = Plugin.GetType().GetProperty("WantButton");
                InjectedButtonText = Plugin.GetType().GetProperty("ButtonText");

                InjectedOnInitialize.Invoke(Plugin, null);
                if (PluginManager.GetEnabledPlugins().Any(n => n == "Cyril"))
                {
                    InjectedOnEnabled.Invoke(Plugin, null);
                }
            }
        }

        #endregion

        #region Helper Methods

        private static void Log(string message)
        {
            Logging.Write(Colors.Teal, $"[Cyril] {message}");
        }

        private static void RedirectAssembly()
        {
            ResolveEventHandler handler = (sender, args) =>
            {
                var name = Assembly.GetEntryAssembly().GetName().Name;

                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != name ? null : Assembly.GetEntryAssembly();
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;
        }

        #endregion
    }
}