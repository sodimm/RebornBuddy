using Clio.XmlEngine;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseObject")]
    public class SoUseObject : SoHuntBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return false;
            }
        }

        protected override void OnHuntStart()
        {
            Log("Started");
        }

        protected override void OnHuntDone()
        {
            Log("Done");
        }

        protected override async Task<bool> CustomLogic()
        {
            if (Target != null)
            {
                await MoveAndStop(Target.Location, Distance, "Moving to " + Target.Name);

                if (ItemId > 0)
                    await UseItem();

                if (InPosition(Target.Location, Distance))
                {
                    if (Target.IsTargetable && Target.IsVisible)
                    {
                        await Dismount();
                      //  StatusText = "Using " + Target.Name;
                        Log("Using {0}", Target.Name);
                        Target.Interact();
                        await ShortCircuit(Target, persistentObject: PersistentObject, mSecsPassed: 10000);
                    }
                }
            }
            return false;
        }
    }
}
