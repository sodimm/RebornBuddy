using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoPickUpQuest")]
    public class SoPickUpQuest : SoProfileBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return true;
            }
        }

        protected override void OnTagStart()
        {
            Log($"Picking up {QuestName} from {QuestGiver}.");
        }

        public override bool IsDone
        {
            get
            {
                if (HasQuest)
                    return true;

                if (IsQuestComplete)
                    return true;

                return _done;
            }
        }

        protected async override Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            if (await MoveAndStop(Destination, Distance, $"Moving to {NpcName}", true, (ushort)MapId, MountDistance)) return true;

            if (!IsQuestAcceptQualified) { _done = true; return false; }

            if (await Interact()) return true;

            return false;
        }

        protected override void OnResetCachedDone() { _done = false; }
    }
}
