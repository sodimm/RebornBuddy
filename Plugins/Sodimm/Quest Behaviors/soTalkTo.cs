using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoTalkTo")]
    public class SoTalkTo : SoProfileBehavior
    {
        protected override void OnTagStart()
        {
            Log($"Talking to {NpcName} for {QuestName}.");
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

        protected async override Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            if (await MoveAndStop(Destination, Distance, $"Moving to {NpcName}", true, (ushort)MapId, MountDistance)) return true;

            if (await Interact()) return true;

            return false;
        }
    }
}
