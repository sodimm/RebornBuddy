using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Navigation;
using OrderBotTags.Behaviors;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("FlightUseItem")]
    [XmlElement("FlightEnabledUseItem")]
    public class FlightEnabledUseItemTag : FlightEnabledProfileBehavior
    {
        public sealed override bool IsDone
        {
            get
            {
                if (IsQuestComplete) { return true; }

                if (IsTodoCountComplete) { return true; }

                if (IsObjectiveComplete) { return true; }

                if (IsStepComplete) { return true; }

                if (Conditional != null)
                {
                    var cond = !Conditional();
                    return cond;
                }

                return false;
            }
        }

        [XmlAttribute("ItemId")]
        public int ItemId { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; } = Vector3.Zero;

        [XmlAttribute("NpcIds")]
        [XmlAttribute("NpcId")]
        public int[] NpcIds { get; set; }

        [XmlElement("HotSpots")]
        public List<HotSpot> Hotspots { get; set; } = new List<HotSpot>();

        [XmlAttribute("InCombat")]
        [DefaultValue(false)]
        public new bool InCombat { get; set; }

        [XmlAttribute("HealthPercent")]
        [DefaultValue(40)]
        public int HealthPercent { get; set; }

        [XmlAttribute("LacksAuraIds")]
        [DefaultValue(null)]
        public int[] Auras { get; set; }


        private Composite _combatlogic;
        protected sealed override void OnStart()
        {
            SetupConditional();

            if (Hotspots.Count == 0)
            {
                if (XYZ == Vector3.Zero)
                {
                    LogError("No HotSpots, NpcIds or XYZ are provided, this is an invalid combination for this behavior.");
                    return;
                }

                Hotspots.Add(new HotSpot(XYZ, 50f));
            }

            if (InCombat)
            {
                _combatlogic = new ActionRunCoroutine(cr => Common.UseItem(ItemId, Poi.Current?.BattleCharacter, inCombat: true, hasAnyAura: Auras, healthPercent: HealthPercent));
                TreeHooks.Instance.InsertHook("PreCombatLogic", 0, _combatlogic);
            }
        }

        public async Task<bool> Main()
        {
            foreach (var o in Hotspots.OrderBy(r => r.Position.DistanceSqr(Core.Me.Location)))
            {
                if (IsDone) { break; }

                if (!await Common.AwaitCombat()) { return false; }

                if (!InCombat && !await Movement.MoveTo(o.Position, ignoreIndoors: IgnoreIndoors)) { return false; }

                if (InCombat && !await Movement.MoveTo(o.Position, true, false, IgnoreIndoors)) { return false; }

                if (!Common.Exists(o.Position, NpcIds))
                {
                    await Common.Sleep(500);
                    continue;
                }

                var obj = Common.GetClosest(NpcIds);

                if (!await Movement.MoveTo(obj.Location, true, true, IgnoreIndoors)) { return false; }

                if (!await Common.UseItem(ItemId, obj, BlacklistAfter, BlacklistDuration)) { return false; }

                await Common.Sleep(500);

                await Dialog.Skip();

                await Common.Sleep(Wait);
            }

            return true;
        }

        protected sealed override Composite CreateBehavior() => new ActionRunCoroutine(cr => Main());

        protected sealed override void OnDone() { }

        protected override void OnResetCachedDone() { }
    }
}
