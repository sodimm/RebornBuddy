
using System;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using System.Windows.Media;

namespace Cyril
{
    public class CyrilPlugin : BotPlugin
    {
        #region BotPlugin Implementation

        public override string Author
        {
            get { return "Sodimm"; }
        }

        public override string Description
        {
            get { return "Repairs equipment when below 5%"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0, 0); }
        }

        public override string Name
        {
            get { return "Cyril"; }
        }

        public override bool WantButton
        {
            get { return true; }
        }

        public override string ButtonText
        {
            get { return "Debug"; }
        }

        private static bool Debug = false;
        public override void  OnButtonPress()
        {
            if (Debug)
            {
                Debug = false;
                Log("Exiting Debug Mode");
            }
            else
            {
                Debug = true;
                Log("Entered Debug Mode");
            }
        }

        private Composite Root = new ActionRunCoroutine(cr => Main());

        public static Vector3 InitialLocation;

        public static uint CurrentZone = 0;

        public static bool HasRepaired = false;

        private static void Log(string p)
        {
            Logging.Write(Colors.Teal, "[Cyril] " + p);
        }

        #endregion

        #region Init

        public override void OnPulse() { }

        public override void OnInitialize()
        {
            VendorLocations.Populate();
            Root = new ActionRunCoroutine(cr => Main());
        }

        public override void OnShutdown() { }

        public override void OnEnabled()
        {
            Log("v" + Version.ToString() + ". Adding Hooks");
            CurrentZone = WorldManager.ZoneId;
            TreeHooks.Instance.AddHook("TreeStart", Root);
            TreeHooks.Instance.OnHooksCleared += OnHooksCleared;
        }

        public override void OnDisabled()
        {
            Log("v" + Version.ToString() + ". Removing Hooks");
            TreeHooks.Instance.OnHooksCleared -= OnHooksCleared;
            TreeHooks.Instance.RemoveHook("TreeStart", Root);
        }

        private void OnHooksCleared(object sender, EventArgs args)
        {
            TreeHooks.Instance.AddHook("TreeStart", Root);
        }

        #endregion

        #region Main

        private static async Task<bool> Main()
        {
            if (WorldManager.ZoneId != CurrentZone)
                CurrentZone = WorldManager.ZoneId;
            
            if (WorldManager.ZoneId == CurrentZone)
            {
                if (NeedRepair && !HasRepaired)
                {
                    if (Poi.Current.Type == PoiType.None || Poi.Current.Type == PoiType.Wait || (Poi.Current.Type == PoiType.Fate && !FateManager.WithinFate))
                    {
                        InitialLocation = Core.Player.Location;
                        Poi.Clear("[Cyril] Damnit! My stuffs buggered, let's sort it out");
                        Poi.Current = new Poi(new HotSpot(VendorLocations.Closest, 0), PoiType.Vendor);
                    }

                    if (Poi.Current.Type == PoiType.Vendor)
                    {
                        if (Vector3.Distance(Core.Player.Location, Poi.Current.Location) > 3.24f)
                        {
                            await Mount();
                            Navigator.MoveTo(Poi.Current.Location, "[Cyril] Legging it");
                        }

                        if (MovementManager.IsMoving && Vector3.Distance(Core.Player.Location, Poi.Current.Location) <= 3.24f)
                            Navigator.PlayerMover.MoveStop();

                        if (!MovementManager.IsMoving && Vector3.Distance(Core.Player.Location, Poi.Current.Location) <= 3.24f)
                        {
                            if (!Core.Player.HasTarget)
                            {
                                if (Core.Player.IsMounted)
                                    await CommonTasks.StopAndDismount();
                                Npc.Target();
                                await Coroutine.Sleep(100);
                            }

                            if (!SelectYesno.IsOpen && !Repair.IsOpen && Core.Player.HasTarget)
                            {
                                Npc.Interact();
                                await Coroutine.Sleep(100);
                            }

                            if (SelectYesno.IsOpen)
                            {
                                SelectYesno.ClickYes();
                                await Coroutine.Sleep(100);
                                HasRepaired = true;
                                if (Debug)
                                {
                                    Log("Debug Loop Completed, Exiting Debug");
                                    Debug = false;
                                }
                            }

                            if (Repair.IsOpen)
                            {
                                Repair.RepairAll();
                                await Coroutine.Sleep(100);
                            }

                            if (SelectIconString.IsOpen)
                            {
                                ClickRepair();
                                await Coroutine.Sleep(100);
                            }
                        }
                    }
                }

                if (!NeedRepair && HasRepaired)
                {
                    if (Repair.IsOpen)
                        Repair.Close();

                    if (Poi.Current.Type == PoiType.Vendor)
                    {
                        Poi.Clear("[Cyril] Damn that fella was cheap, who needs Dark Matter?");

                        if (BotManager.Current.Name != "Fate Bot" && BotManager.Current.Name != "Order Bot")
                            Poi.Current = new Poi(new HotSpot(InitialLocation, 0), PoiType.Hotspot);
                        else
                            HasRepaired = false;
                    }

                    if (Poi.Current.Type == PoiType.Hotspot && BotManager.Current.Name != "Fate Bot" && BotManager.Current.Name != "Order Bot")
                    {
                        if (Vector3.Distance(Core.Player.Location, Poi.Current.Location) >= 3.24f)
                        {
                            await Mount();
                            Navigator.MoveTo(Poi.Current.Location, "[Cyril] Legging it back...");
                        }

                        if (Vector3.Distance(Core.Player.Location, Poi.Current.Location) < 3.24f)
                        {
                            Navigator.PlayerMover.MoveStop();
                            Poi.Clear("[Cyril] We're back, now where was I?");
                            HasRepaired = false;
                        }
                    }
                }
            }

            return false;
        }

        private static GameObject Npc
        {
            get
            {
                var units = GameObjectManager.GameObjects;
                foreach (var unit in units.OrderBy(r => r.Distance2D(Core.Player.Location)))
                {
                    foreach (var id in Vendor.NpcIds)
                    {
                        if (id == unit.NpcId && unit.Distance2D(Core.Player.Location) < 10)
                            return GameObjectManager.GetObjectByNPCId(id);
                    }
                }

                return null;
            }
        }

        private static bool NeedRepair
        {
            get
            {
                if (Debug)
                {
                    Log("Overriding NeedRepair");
                    return true;
                }
                else
                {
                    var items = InventoryManager.EquippedItems.Where(r => r.Condition < 5f).ToList();

                    if (items.Count() >= 1)
                        return true;

                    return false;
                }
            }
        }

        private static async Task Mount()
        {
            if (!WorldManager.InSanctuary && Actionmanager.CanMount == 0)
            {
                await CommonTasks.MountUp();
                await Coroutine.Wait(3000, () => Core.Player.IsMounted);
            }
        }

        private static void ClickRepair()
        {
            var lines = SelectIconString.Lines();

            int line = lines.Count - 2;

            SelectIconString.ClickSlot((uint)line);
        }

        #endregion
    }
}
