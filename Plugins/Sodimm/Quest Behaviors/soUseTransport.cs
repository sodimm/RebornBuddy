using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseTransport")]
    public class SoUseTransport : SoProfileBehavior
    {
        protected override void OnTagStart()
        {
            thisMap = WorldManager.SubZoneId;
            Log($"Moving to Use Transport from {thisMap} at {NpcName}.");
        }

        public override bool IsDone
        {
            get
            {
                if (!HasQuest)
                    return true;

                if (IsStepComplete)
                    return true;

                if (Conditional != null)
                {
                    var cond = !Conditional();
                    return cond;
                }

                return _done;
            }
        }

        uint thisMap = 0;
        protected async override Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            if (thisMap == WorldManager.SubZoneId)
            {
                if (await MoveAndStop(Destination, Distance, $"Moving to use transport option {DialogOption} at {NpcName}", true, (ushort)MapId, MountDistance)) return true;

                if (await Interact()) return true;
            }

            await Coroutine.Wait(15000, () => thisMap != WorldManager.SubZoneId);

            _done = true;

            return false;
        }

        protected override void OnResetCachedDone()
        {
            thisMap = 0;
            _done = false;
        }
    }
}
