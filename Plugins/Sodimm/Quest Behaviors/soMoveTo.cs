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

        protected async override Task Main()
        {
            await CommonTasks.HandleLoading();

            await GoThere();

            if (UseMesh)
            {
                await MoveAndStop(Destination, Distance, "Moving to Location", true);
                if (InPosition(Destination, Distance))
                    _done = true;
            }
            else
            {
                Me.Face(Destination);
                MovementManager.MoveForwardStart();
                if (InPosition(Destination, Distance))
                    _done = true;
            }
        }

        protected override void OnTagDone()
        {
            Navigator.PlayerMover.MoveStop();
        }

        protected override void OnResetCachedDone() { _done = false; }
    }
}