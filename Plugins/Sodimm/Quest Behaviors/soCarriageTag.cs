using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Navigation;
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

        [XmlAttribute("SafeSpot")]
        public Vector3 SafeSpot
        {
            get
            {
                return _evac;
            }
            set
            {
                _evac = value;
            }
        }
        private Vector3 _evac;

        #endregion

        public override bool IsDone
        {
            get
            {
                if (!HasQuest)
                    return true;

                if (IsStepComplete)
                    return true;

                return false;
            }
        }

        protected override void OnTagStart()
        {
            Log("Moving to Destination.");
        }

        protected async override Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            if (await MoveAndStop(Destination, Distance, $"Moving to {NpcName}", true)) return true;

            if (await Interact()) return true;

            if (SafeSpot != Vector3.Zero && await MoveAndStop(SafeSpot, Distance, "Moving to Safe Spot")) return true;

            return false;
        }

        protected override void OnTagDone()
        {
            Navigator.PlayerMover.MoveStop();
        }
    }
}
