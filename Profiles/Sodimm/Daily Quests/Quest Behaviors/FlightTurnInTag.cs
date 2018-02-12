using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Managers;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("FlightTurnIn")]
    [XmlElement("FlightTurnin")]
    [XmlElement("FlightEnabledTurnIn")]
    [XmlElement("FlightEnabledTurnin")]
    public class FlightEnabledTurnInQuestTag : FlightEnabledProfileBehavior
    {
        private bool HasQuest => QuestLogManager.HasQuest(QuestId);
        public override bool IsDone
        {
            get
            {
                if (HasQuest) { return false; }

                return true;
            }
        }

        protected override void OnStart() { }

        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }

        [XmlAttribute("DialogOption")]
        public int DialogOption { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }

        public async Task<bool> Main()
        {
            if (!await Movement.MoveTo(XYZ, true, true)) { return false; }

            if (!await Dialog.Interact(NpcId, QuestId, DialogOption)) { return false; }

            return true;
        }

        protected override Composite CreateBehavior() => new ActionRunCoroutine(cr => Main());

        protected override void OnDone() { }

        protected override void OnResetCachedDone() { }
    }
}
