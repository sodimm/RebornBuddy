using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Loader
{
    public class ArtemisLoader : BotBase
    {
        private const string BotBaseName = "Artemis";
        private const string BotBaseClass = "Artemis.Artemis";
        private static readonly object ObjectLock = new object();
        private static readonly string botBaseAssembly = Path.Combine(Environment.CurrentDirectory, @"BotBases\Artemis\Artemis.dll");
        private static readonly string botBaseDir = Path.Combine(Environment.CurrentDirectory, @"BotBases\Artemis");
        private static readonly string botBaseTypeFolder = Path.Combine(Environment.CurrentDirectory, @"BotBases");
        private static readonly Color logColor = Colors.Cyan;
        private static volatile bool _loaded;

        public ArtemisLoader() { }

        public static object BotBase { get; set; }

        private static MethodInfo BotBaseStart { get; set; }

        private static MethodInfo BotBaseStop { get; set; }

        private static MethodInfo BotBaseButton { get; set; }

        private static MethodInfo BotBaseRoot { get; set; }

        public override string Name
        {
            get
            { return "Artemis"; }
        }

        public override PulseFlags PulseFlags
        {
            get
            { return PulseFlags.All; }
        }

        public override bool IsAutonomous
        {
            get
            { return true; }
        }

        public override bool WantButton
        {
            get
            { return true; }
        }

        public override bool RequiresProfile
        {
            get
            { return false; }
        }

        public override Composite Root
        {
            get
            {
                if (!_loaded && BotBase == null)
                {
                    LoadBotBase();
                }

                return BotBase != null ? (Composite)BotBaseRoot.Invoke(BotBase, null) : new Action();
            }
        }

        public override void OnButtonPress()
        {
            if (!_loaded && BotBase == null)
            {
                LoadBotBase();
            }

            if (BotBase != null)
            {
                BotBaseButton.Invoke(BotBase, null);
            }
        }

        public override void Start()
        {
            if (!_loaded && BotBase == null)
            {
                LoadBotBase();
            }

            if (BotBase != null)
            {
                BotBaseStart.Invoke(BotBase, null);
            }
        }

        public override void Stop()
        {
            if (!_loaded && BotBase == null)
            {
                LoadBotBase();
            }

            if (BotBase != null)
            {
                BotBaseStop.Invoke(BotBase, null);
            }
        }

        public static void RedirectAssembly()
        {
            ResolveEventHandler handler = (sender, args) =>
            {
                return new AssemblyName(args.Name).Name != Assembly.GetEntryAssembly().GetName().Name ? null : Assembly.GetEntryAssembly();
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;
        }

        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                Assembly assembly = Assembly.LoadFrom(path);
            }
            catch (Exception e)
            {
                Logging.WriteException(e);
            }

            return null;
        }

        private static object Load()
        {
            RedirectAssembly();

            if (LoadAssembly(botBaseAssembly) == null)
            {
                return null;
            }

            Type baseType;
            try
            {
                baseType = LoadAssembly(botBaseAssembly).GetType(BotBaseClass);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            object _botBase;
            try
            {
                _botBase = Activator.CreateInstance(baseType);
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }

            if (_botBase != null)
            {
                Log($"{BotBaseName} was loaded successfully.");
            }
            else
            {
                Log($"Could not load {BotBaseName}. This can be due to a new version of Rebornbuddy being released. An update should be ready soon.");
            }

            return _botBase;
        }

        private static void LoadBotBase()
        {
            lock (ObjectLock)
            {
                if (BotBase != null)
                {
                    return;
                }

                BotBase = Load();

                _loaded = true;

                if (BotBase == null)
                {
                    return;
                }

                BotBaseStart = BotBase.GetType().GetMethod("Start");
                BotBaseStop = BotBase.GetType().GetMethod("Stop");
                BotBaseButton = BotBase.GetType().GetMethod("OnButtonPress");
                BotBaseRoot = BotBase.GetType().GetMethod("GetRoot");
            }
        }

        private static void Log(string message)
        {
            message = $"[{BotBaseName}] {message}";
            Logging.Write(logColor, message);
        }
    }
}
