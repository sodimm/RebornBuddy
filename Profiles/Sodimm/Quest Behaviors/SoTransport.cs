using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoTransport")]
    public class SoTransport : ProfileBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return true;
            }
        }

        private bool _done;

        public override bool IsDone
        {
            get
            {
                if (IsStepComplete)
                {
                    return true;
                }

                return _done;
            }
        }

        public override string StatusText
        {
            get
            {
                return $"Transporting for {QuestName}.";
            }
        }

        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }

        [XmlAttribute("InteractDistance")]
        [DefaultValue(5f)]
        public float InteractDistance { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }

        [XmlAttribute("BlacklistAfter")]
        [DefaultValue(false)]
        public bool BlacklistAfter { get; set; }

        public GameObject NPC
        {
            get
            {
                var npc = GameObjectManager.GetObjectsByNPCId((uint)NpcId).FirstOrDefault(r => r.IsVisible && r.IsTargetable);
                return npc;
            }
        }

        protected override Composite CreateBehavior()
        {
            return
                new PrioritySelector(ctx => NPC,
                    new ActionRunCoroutine(r => MoveAndStop(XYZ, InteractDistance, false, StatusText)),
                    new ActionRunCoroutine(r => CreateInteract(((GameObject)r)))
               );
        }

        private async Task<bool> MoveAndStop(Vector3 location, float distance, bool stopInRange = false, string destinationName = null)
        {
            return await CommonTasks.MoveAndStop(new Pathing.MoveToParameters(location, destinationName), distance, stopInRange);
        }

        private async Task<bool> CreateInteract(GameObject obj)
        {
            if (ShortCircuit(obj))
            {
                return true;
            }

            if (obj.IsTargetable && obj.IsVisible)
            {
                Navigator.PlayerMover.MoveStop();

                obj.Face();

                obj.Interact();

                if (BlacklistAfter)
                {
                    _done = true;
                }
                else
                {
                    await Coroutine.Wait(5000, () => ShortCircuit(obj));
                }
            }

            return false;
        }

        protected bool ShortCircuit(GameObject obj)
        {
            if (!obj.IsValid || !obj.IsTargetable || !obj.IsVisible)
            {
                return true;
            }

            if (Core.Player.InCombat && !InCombat)
            {
                return true;
            }

            if (Talk.DialogOpen)
            {
                return true;
            }

            return false;
        }

        protected override void OnStart()
        {

        }

        protected override void OnDone()
        {
            BlacklistAfter = false;
        }

        protected override void OnResetCachedDone()
        {
            BlacklistAfter = false;
        }
    }
}