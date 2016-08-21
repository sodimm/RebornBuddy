using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoPickUpQuest")]
    [XmlElement("SoPickUpDailyQuest")]
    public class SoPickUpQuest : SoProfileBehavior
    {
        public override bool HighPriority { get { return true; } }

        protected override void OnTagStart()
        {
            Log("Picking up {0} from {1}.", QuestName, QuestGiver);
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

        protected async override Task Main()
        {
            await CommonTasks.HandleLoading();

            await GoThere();

            await MoveAndStop(Destination, Distance, "Moving to pick up " + QuestName + ".");

            if (InPosition(Destination, Distance))
            {
                if (!HasQuest && !IsQuestAcceptQualified)
                    _done = true;
                else
                    await Interact();
            }

            await Coroutine.Yield();
        }

        protected override void OnResetCachedDone() { _done = false; }
    }
}