using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoHandOver")]
    public class SoHandOver : SoProfileBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return true;
            }
        }
        public override bool IsDone
        {
            get
            {
                if (!HasQuest)
                    return true;

                if (IsQuestComplete)
                    return true;

                if (IsStepComplete)
                    return true;

                if (IsObjectiveCountComplete)
                    return true;

                if (IsObjectiveComplete)
                    return true;

                if (!HasItems)
                    return true;

                return false;
            }
        }

        protected override void OnTagStart()
        {
            Log($"Delivering Items for {QuestName}.");
        }

        protected async override Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            if (await MoveAndStop(Destination, Distance, $"Moving to {NpcName}", true, (ushort)MapId, MountDistance)) return true;

            if (await Interact()) return true;

            return false;
        }
    }
}
