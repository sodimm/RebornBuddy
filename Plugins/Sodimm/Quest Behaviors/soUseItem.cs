using Clio.XmlEngine;
using ff14bot.Managers;
using OrderBotTags.Behaviors;
using System.Linq;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseItem")]
    public class SoUseItem : SoHuntBehavior
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

        private BagSlot Item
        {
            get
            {
                return InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemId);
            }
        }

        protected override async Task<bool> CustomLogic()
        {
            if (Target != null)
            {
                await MoveAndStop(Target.Location, Distance, "Moving to " + Target.Name);

                if (InPosition(Target.Location, Distance))
                {
                    if (Target.IsTargetable && Target.IsVisible)
                    {
                        Target.Target();

                        if (Item != null)
                        {
                            await Dismount();

                            Log("Using {0} on {1}.", Item.Name, Target.Name);
                            StatusText = "Using " + Item.Name + " on " + Target.Name;
                            if (Item.Item.IsGroundTargeting)
                                Item.UseItem(Target.Location);
                            else
                                Item.UseItem(Target);
                        }
                        await ShortCircuit(Target, persistentObject: PersistentObject, mSecsPassed: 10000);
                    }
                }
            }
            return false;
        }
    }
}
