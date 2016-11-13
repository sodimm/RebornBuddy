using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles
{
    [XmlElement("SoMoveTo")]
    public class SoMoveTo : SoProfileBehavior
    {
        protected override void OnTagStart()
        {
            Log("Moving to Destination");
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

                return _done;
            }
        }

        protected async override Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            if (UseMesh)
            {
                if (await MoveAndStop(Destination, Distance, "Moving to Destination", true, (ushort)MapId, MountDistance)) return true;
                _done = true;
            }
            else
            {
                Core.Player.Face(Destination);
                MovementManager.MoveForwardStart();

                if (!Navigator.InPosition(Core.Player.Location, Destination, Distance))
                    return true;
                else
                    _done = true;
            }

            return false;
        }

        protected override void OnTagDone()
        {
            Navigator.PlayerMover.MoveStop();
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
        }
    }
}
