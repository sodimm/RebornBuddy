using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoHandOver")]
    public class SoHandOver : SoProfileBehavior
    {
        public override bool HighPriority { get { return true; } }

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
            Log("Delivering Items for {0}.", QuestName);
        }

        protected async override Task Main()
        {
            await CommonTasks.HandleLoading();

            await GoThere();

            await MoveAndStop(Destination, Distance, "Moving to Hand Over to " + NpcName);

            if (InPosition(Destination, Distance))
                await Interact();

            await Coroutine.Yield();
        }
    }
}