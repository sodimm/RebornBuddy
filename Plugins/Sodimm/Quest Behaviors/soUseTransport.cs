using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseTransport")]
    public class SoUseTransport : SoProfileBehavior
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
            Log("Moving to Use Transport.");
        }

        public override bool IsDone
        {
            get
            {
                if (Conditional != null)
                {
                    var cond = !Conditional();
                    return cond;
                }

                return _done;
            }
        }

        protected async override Task Main()
        {
            await CommonTasks.HandleLoading();

            if (!IsDone)
            {
                await GoThere();

                await MoveAndStop(Destination, Distance, "Moving to Use Transport");

                if (InPosition(Destination, Distance))
                {
                    await Interact();

                    await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => !WindowsOpen());

                    _done = true;
                }
            }
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
        }
    }
}
