using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using OrderBotTags.Behaviors;
using System.Collections.Generic;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("FlightGetTo")]
    [XmlElement("FlightEnabledGetTo")]
    public class FlightEnabledGetToTag : FlightEnabledProfileBehavior
    {
        public override bool HighPriority { get { return true; } }

        private bool _done;
        public override bool IsDone { get { return _done; } }

        [XmlAttribute("ZoneId")]
        public int ZoneId { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; } = Vector3.Zero;

        protected sealed override void OnStart() { }

        private Queue<NavGraph.INode> thisPath;
        public async Task<bool> Main()
        {
            if (WorldManager.ZoneId != ZoneId)
            {
                thisPath = await NavGraph.GetPathAsync((ushort)ZoneId, XYZ);

                if (thisPath == null || thisPath.Count == 0) { return false; }

                while (WorldManager.ZoneId != ZoneId && await NavGraph.NavGraphConsumer(ctx => thisPath).ExecuteCoroutine()) { await Coroutine.Yield(); }
            }

            if (!await Movement.MoveTo(XYZ, Land, Dismount, IgnoreIndoors)) { return false; }

            await Coroutine.Wait(4000, () => !Core.Me.InCombat);

            return _done = true;
        }

        protected override Composite CreateBehavior() => new ActionRunCoroutine(cr => Main());

        protected override void OnDone() { }

        protected override void OnResetCachedDone() { _done = false; }
    }
}
