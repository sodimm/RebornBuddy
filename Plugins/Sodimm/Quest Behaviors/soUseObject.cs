using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Objects;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseObject")]
    public class SoUseObject : SoHuntBehavior
    {
        protected override void OnHuntStart()
        {
            Log($"Started for {QuestName}.");
        }

        protected override void OnHuntDone()
        {
            Log($"Objectives completed for {QuestName}.");
        }

        private async Task<bool> DoUseObject(GameObject obj)
        {
            if (obj.IsTargetable && obj.IsVisible)
            {
                obj.Interact();
                await Coroutine.Wait(3000, () => Core.Player.IsCasting);

                //if (!Core.Player.IsCasting) return false;
            }

            return !await ShortCircuit(obj, 10000);
        }

        protected override async Task<bool> CustomLogic()
        {
            if (Target != null)
            {
                if (await MoveAndStop(Target.Location, Distance, $"Moving to {Target.Name}", true, 0, MountDistance)) return true;

                if (await Dismount()) return true;

                if (ItemId > 0 && await UseItem()) return true;

                if (await DoUseObject(Target)) return true;
            }

            return false;
        }
    }
}
