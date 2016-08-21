using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoTalkTo")]
    public class SoTalkTo : SoProfileBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return false;
            }
        }

        protected override void OnTagStart()
        {
            Log("Talking to {0} for {1}", NpcName, QuestName);
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
                return false;
            }
        }

        protected async override Task Main()
        {
            await CommonTasks.HandleLoading();

            await GoThere();

            await MoveAndStop(Destination, Distance, "Moving to Talk to " + NpcName);

            if (InPosition(Destination, Distance))
                await Interact();
        }
    }
}
