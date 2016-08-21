using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles
{
    [XmlElement("SoTurnIn")]
    public class SoTurnIn : SoProfileBehavior
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
                if (!IsOnTurnInStep)
                    return true;
                if (!HasItems)
                    return true;

                return false;
            }
        }

        protected override void OnTagStart()
        {
            Log("Completing {0}.", QuestName);
        }

        protected async override Task Main()
        {
            await CommonTasks.HandleLoading();

            await GoThere();

            await MoveAndStop(Destination, Distance, "Moving to Turn In " + QuestName);

            if (InPosition(Destination, Distance))
                await Interact();

            await Coroutine.Yield();
        }
    }
}
