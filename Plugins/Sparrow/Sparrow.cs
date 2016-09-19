
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;

namespace Sparrow
{
    public class SparrowPlugin : BotPlugin
    {
        #region BotPlugin Implementation

        public override string Author
        {
            get { return "Sodimm"; }
        }

        public override string Description
        {
            get { return "Everything Chocobo"; }
        }

        public override Version Version
        {
            get { return new Version(0, 0, 0, 1); }
        }

        public override string Name
        {
            get { return "Sparrow"; }
        }

        public override bool WantButton
        {
            get { return true; }
        }

        public override string ButtonText
        {
            get { return "Settings"; }
        }

        private Composite Root;

        private SparrowForm _Form;
        
        public override void OnButtonPress()
        {
            if (_Form == null || _Form.IsDisposed || _Form.Disposing)
            {
                _Form = new SparrowForm();
            }

            _Form.ShowDialog();
        }

        private static void Log(string p)
        {
            Logging.Write(Colors.Teal, "[Sparrow] " + p);
        }

        #endregion

        #region Init

        public override void OnPulse() { }

        public override void OnInitialize()
        {
            Root = new ActionRunCoroutine(cr => Main());
        }

        public override void OnShutdown() { }

        public override void OnEnabled()
        {
            Log("v" + Version.ToString() + ". Adding Hooks");
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

        public static SparrowSettings settings = SparrowSettings.Instance;

        #endregion

        #region Main

        private static BattleCharacter Companion
        {
            get
            {
                foreach (PartyMember p in PartyManager.AllMembers)
                {
                    if (!p.IsMe)
                        return (p.GameObject as BattleCharacter);
                }
                return null;
            }
        }

        private static bool CompanionHasAura(params int[] ids)
        {
            int idCount = 0;

            foreach (int id in ids)
            {
                if (Companion.HasAura(id))
                    idCount++;
            }

            if (idCount > 0)
                return true;
            else
                return false;
        }

        private static BagSlot Feed(int id)
        {
            var i = InventoryManager.FilledSlots.ToArray();

            BagSlot item = i.FirstOrDefault(r => r.RawItemId == id);

            if (item != null)
                return item;
            else
                return null;
        }

        private static async Task<bool> Main()
        {
            if (!Chocobo.Summoned && Chocobo.CanSummon)
            {
                Log("Summoning your Companion");
                Chocobo.Summon();
                await Coroutine.Wait(10000, () => Chocobo.Summoned);
            }

            if (Chocobo.Summoned)
            {
                if (settings.Stance != "Free" && !Core.Player.IsMounted)
                {
                    if (Chocobo.Stance != CompanionStance.Attacker && settings.Stance == "Attacker")
                    {
                        Log("Switching Companion stance to Attacker");
                        Chocobo.AttackerStance();
                        await Coroutine.Wait(5000, () => Chocobo.Stance == CompanionStance.Attacker);
                    }

                    if (Chocobo.Stance != CompanionStance.Defender && settings.Stance == "Defender")
                    {
                        Log("Switching Companion stance to Defender");
                        Chocobo.DefenderStance();
                        await Coroutine.Wait(5000, () => Chocobo.Stance == CompanionStance.Defender);
                    }

                    if (Chocobo.Stance != CompanionStance.Healer && settings.Stance == "Healer")
                    {
                        Log("Switching Companion stance to Healer");
                        Chocobo.HealerStance();
                        await Coroutine.Wait(5000, () => Chocobo.Stance == CompanionStance.Healer);
                    }
                }
                else if (Chocobo.Stance != CompanionStance.Free && settings.Stance == "Free" && !Core.Player.IsMounted)
                {
                    Log("Switching Companion stance to Free");
                    Chocobo.FreeStance();
                    await Coroutine.Wait(5000, () => Chocobo.Stance == CompanionStance.Free);
                }
            }

            if (Companion != null && Chocobo.TimeLeft.TotalMinutes > settings.OnlyFeedWhenTimeAbove)
            {
                if (settings.Feed != "None" && !Core.Player.IsMounted)
                {
                    if (settings.Feed == "Curiel Root" && !CompanionHasAura(537,536))
                    {
                        if (Feed(7894) != null && Feed(7894).CanUse())
                        {
                            Feed(7894).UseItem();
                            Log("Feeding companion a Curiel Root");
                            await Coroutine.Wait(5000, () => CompanionHasAura(537,536));
                        }
                    }

                    if (settings.Feed == "Mimett Gourd" && !CompanionHasAura(541,540))
                    {
                        if (Feed(7897) != null && Feed(7897).CanUse())
                        {
                            Feed(7897).UseItem();
                            Log("Feeding companion a Mimett Gourd");
                            await Coroutine.Wait(5000, () => CompanionHasAura(541,540));
                        }
                    }

                    if (settings.Feed == "Pahsana Fruit" && !CompanionHasAura(545,544))
                    {
                        if (Feed(7900) != null && Feed(7900).CanUse())
                        {
                            Feed(7900).UseItem();
                            Log("Feeding companion a Pahsana Fruit");
                            await Coroutine.Wait(5000, () => CompanionHasAura(545,544));
                        }
                    }

                    if (settings.Feed == "Sylkis Bud" && !CompanionHasAura(539,538))
                    {
                        if (Feed(7895) != null && Feed(7895).CanUse())
                        {
                            Feed(7895).UseItem();
                            Log("Feeding companion a Sylkis Bud");
                            await Coroutine.Wait(5000, () => CompanionHasAura(539,538));
                        }
                    }

                    if (settings.Feed == "Tantalplant" && !CompanionHasAura(543,542))
                    {
                        if (Feed(7898) != null && Feed(7898).CanUse())
                        {
                            Feed(7898).UseItem();
                            Log("Feeding companion a Tantalplant");
                            await Coroutine.Wait(5000, () => CompanionHasAura(543,542));
                        }
                    }
                }
            }

            return false;
        }

        #endregion
    }
}
