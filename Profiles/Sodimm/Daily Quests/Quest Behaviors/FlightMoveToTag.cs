using Clio.Utilities;
using Clio.XmlEngine;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("FlightMoveTo")]
    [XmlElement("FlightEnabledMoveTo")]
    public class FlightEnabledMoveToTag : FlightEnabledProfileBehavior
    {
        public bool _done;
        public override bool IsDone { get { return _done; } }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }
 
        protected override void OnStart() { }

        public async Task<bool> Main()
        {
            if (!await Movement.MoveTo(XYZ, Land)) { return false; }

            return _done = true;
        }

        protected override Composite CreateBehavior() => new ActionRunCoroutine(cr => Main());

        protected override void OnDone() { }

        protected override void OnResetCachedDone() { _done = false; }
    }
}
