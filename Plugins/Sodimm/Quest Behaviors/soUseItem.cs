using Clio.XmlEngine;
using ff14bot.Managers;
using ff14bot.Objects;
using OrderBotTags.Behaviors;
using System.Linq;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseItem")]
    public class SoUseItem : SoHuntBehavior
    {
        protected override void OnHuntStart()
        {
            Log($"Started for {QuestName}.");
        }

        protected override void OnHuntDone()
        {
            Log($"Objectives completed for {QuestName}.");
        }

        private BagSlot Item
        {
            get
            {
                return InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemId);
            }
        }

        private async Task<bool> DoUseItem(GameObject obj, BagSlot item)
        {
            if (item != null)
            {
                if (Core.Player.HasTarget)
                    obj.Target();

                if (item.Item.IsGroundTargeting)
                    item.UseItem(obj.Location);
                else
                    item.UseItem(obj);
            }

            return !await ShortCircuit(obj);
        }

        protected override async Task<bool> CustomLogic()
        {
            if (Target != null)
            {
                if (await MoveAndStop(Target.Location, Distance, $"Moving to {Target.Name}", true, 0, MountDistance)) return true;

                if (await Dismount()) return true;

                if (await DoUseItem(Target, Item)) return true;
            }

            return false;
        }
    }
}
