using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoCarriage")]
    public class SoCarriageTag : SoProfileBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return true;
            }
        }

        #region Attributes

        [XmlAttribute("EvacXYZ")]
        [XmlAttribute("SafeSpot")]
        public Vector3 SafeSpot { get { return _evac; } set { _evac = value; } }
        private Vector3 _evac;

        [XmlAttribute("BlacklistAfter")]
        public bool BlacklistAfter { get; set; }

        #endregion

        public override bool IsDone
        {
            get
            {
                if (!HasQuest)
                    return true;
                if (IsStepComplete)
                    return true;
                if (_done)
                    return true;

                return false;
            }
        }

        protected override void OnTagStart()
        {
            Log("Moving to Destination.");
        }

        private bool doneInteract = false;

        protected async override Task Main()
        {
            await CommonTasks.HandleLoading();

            await MoveAndStop(Destination, Distance, "Moving to Destination.");

            if (!doneInteract && InPosition(Destination, Distance))
            {
                await Interact();

                doneInteract = true;

                if (BlacklistAfter)
                    _done = true;
            }

            if (SafeSpot != Vector3.Zero && doneInteract)
            {
                await MoveAndStop(SafeSpot, Distance, "Moving to Safe Spot");

                if (InPosition(SafeSpot, Distance))
                    _done = true;
            }

            await Coroutine.Yield();
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
            doneInteract = false;
        }
    }
}
