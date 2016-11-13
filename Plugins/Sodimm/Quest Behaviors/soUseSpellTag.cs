using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using OrderBotTags.Behaviors;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseSpell")]
    public class SoUseSpell : SoHuntBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return !IgnoreCombat ? false : true;
            }
        }

        [XmlAttribute("SpellId")]
        public uint SpellId { get; set; }

        [XmlAttribute("DismountAfter")]
        [DefaultValue(true)]
        public bool DismountAfter { get; set; }

        [XmlAttribute("IgnoreCombat")]
        [DefaultValue(true)]
        public bool IgnoreCombat { get; set; }

        private SpellData Spell
        {
            get
            {
                return DataManager.GetSpellData(SpellId);
            }
        }

        protected override void OnHuntStart()
        {
            Log($"Started for {QuestName}.");
        }

        protected override void OnHuntDone()
        {
            if (DismountAfter)
                CommonTasks.StopAndDismount();

            Log($"Objectives completed for {QuestName}.");
        }

        private async Task<bool> DoSpellCast(GameObject obj, SpellData sp)
        {
            if (obj.IsTargetable && obj.IsVisible)
            {
                Navigator.Stop();

                obj.Face();

                if (!Actionmanager.DoAction(sp.Id, obj) && Actionmanager.CanCast(sp, obj))
                    Actionmanager.DoAction(sp.Id, obj);
                else if (!Actionmanager.DoActionLocation(sp.Id, obj.Location) && Actionmanager.CanCastLocation(sp, obj.Location))
                    Actionmanager.DoActionLocation(sp.Id, obj.Location);
            }

            return !await ShortCircuit(obj);
        }

        protected override async Task<bool> CustomLogic()
        {
            if (Target != null)
            {
                if (await MoveAndStop(Target.Location, Distance, $"Moving to {Target.Name}", true)) return true;

                if (await DoSpellCast(Target, Spell)) return true;
            }

            //if (DismountAfter && await Dismount()) return true;

            return false;
        }
    }
}
