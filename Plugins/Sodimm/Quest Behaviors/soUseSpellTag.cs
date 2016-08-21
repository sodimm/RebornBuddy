using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
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
                if (!IgnoreCombat)
                    return false;
                else
                    return true;
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
            Log("Started");
        }

        protected override void OnHuntDone()
        {
            if (DismountAfter)
                CommonTasks.StopAndDismount();

            Log("Done");
        }

        protected override async Task<bool> CustomLogic()
        {
            if (Target != null)
            {
                await MoveAndStop(Target.Location, Distance, "Moving to " + Target.Name, true);

                if (InPosition(Target.Location, Distance))
                {
                    if (Target.IsTargetable && Target.IsVisible)
                    {
                        Target.Face();
                        Log("Using {0} on {1}.", Spell.LocalizedName, Target.Name);
                        StatusText = "Using " + Spell.LocalizedName + " on " + Target.Name;

                        if (!Actionmanager.DoAction(Spell.Id, Target) && Actionmanager.CanCast(Spell, Target))
                            Actionmanager.DoAction(Spell.Id, Target);
                        else
                            if (!Actionmanager.DoActionLocation(Spell.Id, Target.Location) && Actionmanager.CanCastLocation(Spell, Target.Location))
                            Actionmanager.DoActionLocation(Spell.Id, Target.Location);

                        await ShortCircuit(Target, PersistentObject, IgnoreCombat, 5000);
                    }
                }
            }

            return false;
        }
    }
}
