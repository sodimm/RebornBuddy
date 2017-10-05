using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles.Tags;
using ff14bot.Objects;
using System;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
    [XmlElement("SoUseSpell")]

    public class SoUseSpellTag : HuntBehavior
    {
        public override bool HighPriority => true;

        private SpellData Spell => DataManager.GetSpellData(SpellId);
        public override string StatusText => $"Using ability {Spell.LocalizedName} for {QuestName}.";

        [XmlAttribute("SpellId")]
        public uint SpellId { get; set; }

        public override Composite CustomLogic
        {
            get
            {
                return
                    new Decorator(r => (r as GameObject) != null,
                        new PrioritySelector(
                            new Decorator(r => Core.Player.Location.Distance(((GameObject)r).Location) > UseDistance,
                                new ActionRunCoroutine(r => MoveAndStop(((GameObject)r).Location, UseDistance, false, StatusText))
                            ),
                            new ActionRunCoroutine(r => CreateUseSpell(((GameObject)r), Spell))
                         )
                   );
            }
        }

        private async Task<bool> MoveAndStop(Vector3 location, float distance, bool stopInRange = false, string destinationName = null)
        {
            return await CommonTasks.MoveAndStop(new Pathing.MoveToParameters(location, destinationName), distance, stopInRange);
        }

        private async Task<bool> CreateUseSpell(GameObject obj, SpellData spell)
        {
            if (ShortCircut(obj))
            {
                return false;
            }

            if (obj.IsTargetable && obj.IsVisible)
            {
                Navigator.PlayerMover.MoveStop();

                obj.Face();

                if (ActionManager.DoAction(Spell, obj))
                {
                    ActionManager.DoAction(Spell, obj);
                }

                if (BlacklistAfter)
                {
                    Blacklist.Add(obj, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(BlacklistDuration), "BlacklistAfter");
                }

                await Coroutine.Wait(5000, () => ShortCircut(obj));
            }

            return false;
        }

        private async Task<bool> FlightLogic()
        {
            if (Core.Player.IsMounted && !MovementManager.IsFlying)
            {
                return await CommonTasks.TakeOff();
            }

            return false;
        }

        private Composite _flightLogic;
        protected override void OnStartHunt()
        {
            _flightLogic = new ActionRunCoroutine(cr => FlightLogic());
            Log("Injecting Mount Logic.");
            TreeHooks.Instance.InsertHook("TreeStart", 0, _flightLogic);
            Log("Started");
        }

        protected override void OnDoneHunt()
        {
            if (_flightLogic != null)
            {
                Log("Removing Mount Logic.");
                TreeHooks.Instance.RemoveHook("TreeStart", _flightLogic);
            }

            Log("Finished");
        }
    }
}
