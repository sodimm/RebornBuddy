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
            Log($"Completing {QuestName}.");
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
