using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseTransport")]
    public class SoUseTransport : SoProfileBehavior
    {
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

        protected async override Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            if (!IsDone)
            {
                if (await MoveAndStop(Destination, Distance, $"Moving to use transport option {DialogOption} at {NpcName}", true, (ushort)MapId, MountDistance)) return true;

                if (await Interact()) return true;

                await Coroutine.Sleep(5000);

                _done = true;
            }

            return false;
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
        }
    }
}
