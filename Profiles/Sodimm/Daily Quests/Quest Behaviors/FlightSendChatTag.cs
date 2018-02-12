using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Managers;
using System.ComponentModel;
using System.Threading.Tasks;
using TreeSharp;
using System;
using Clio.Utilities;
using ff14bot.Behavior;
using System.Linq;
using OrderBotTags.Behaviors;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("FlightSendChat")]
    [XmlElement("FlightEnabledSendChat")]
    public class FlightEnabledSendChatTag : FlightEnabledProfileBehavior
    {
        public override bool IsDone
        {
            get
            {
                if (QuestId > 0 && StepId > 0) { return IsStepComplete; }

                return _done;
            }
        }

        [XmlAttribute("NpcId")]
        [DefaultValue(0)]
        public int NpcId { get; set; }

        [XmlAttribute("QuestItem")]
        [DefaultValue(0)]
        public int QuestItem { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ
        {
            get { return Position; }
            set { Position = value; }
        }
        public Vector3 Position;

        [XmlAttribute("GearSet")]
        [DefaultValue(0)]
        public int GearSet { get; set; }

        [XmlAttribute("RemoveAura")]
        [DefaultValue(0)]
        public int Aura { get; set; }

        [XmlAttribute("Say")]
        [DefaultValue(null)]
        public string Say { get; set; }

        [XmlAttribute("Emote")]
        [DefaultValue(null)]
        public string Emote { get; set; }

        [XmlAttribute("DoAction")]
        [DefaultValue(null)]
        public string DoAction { get; set; }

        [XmlAttribute("DoActionTarget")]
        [DefaultValue(null)]
        public string DoActionTarget { get; set; }

        [XmlAttribute("Delay")]
        [DefaultValue(1500)]
        public int Delay { get; set; }

        private string currentPrefRoutine = null;
        protected override void OnStart()
        {
            if (GearSet > 0)
            {
                if (RoutineManager.PreferedRoutine != null)
                {
                    currentPrefRoutine = RoutineManager.PreferedRoutine;
                }

                RoutineManager.PickRoutineFired += OnPickRoutineFired;
            }
        }

        private void OnPickRoutineFired(object sender, EventArgs e)
        {
            RoutineManager.PreferedRoutine = "Kupo";
        }

        private bool _done;
        private async Task<bool> Main()
        {
            if (Core.Player.IsMounted && !await CommonTasks.StopAndDismount()) return false;

            if (!await Common.AwaitCombat()) return false;

            await Coroutine.Sleep(Delay);

            if (GearSet > 0)
            {
                if (GearsetManager.ActiveGearset.Index == GearSet)
                {
                    Log("Desired Gearset is already active");
                    _done = true;
                }

                foreach (var gs in GearsetManager.GearSets)
                {
                    if (gs.Index == GearSet)
                    {
                        Log($"Changing your Gearset to {gs.Class}.");
                        gs.Activate();
                    }
                }
            }

            if (Aura > 0)
            {
                string thisAura = null;
                var auraId = Core.Player.GetAuraById((uint)Aura);
                if (Core.Player.HasAura((uint)Aura))
                {
                    thisAura = auraId.LocalizedName;
                }
                else
                {
                    _done = true;
                    return false;
                }

                Log($"Removing Aura {thisAura}.");
                ChatManager.SendChat("/statusoff \"" + thisAura + "\"");
            }

            if (DoAction != null)
            {
                Log($"Using {DoAction} on Player.");
                ChatManager.SendChat("/ac \"" + DoAction + "\" <me>");
            }

            if (DoActionTarget != null)
            {
                Log($"Using {DoActionTarget} on Current Target.");
                ChatManager.SendChat("/ac \"" + DoActionTarget + "\" <target>");
            }

            if (Say != null)
            {
                Log($"Saying {Say}.");
                ChatManager.SendChat("/s " + Say);
            }

            if (Emote != null)
            {
                if (NpcId > 0)
                {
                    var obj = GameObjectManager.GetObjectByNPCId((uint)NpcId);
                    Log($"Targeting {obj.EnglishName}.");
                    obj.Target();
                }

                Log($"Using the {Emote} Emote.");
                ChatManager.SendChat("/" + Emote);
            }

            if (QuestItem > 0 && XYZ != null)
            {
                BagSlot item = InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == QuestItem);
                Log($"Using {item.EnglishName} on {XYZ.ToString()}.");
                ActionManager.DoActionLocation(Enums.ActionType.KeyItem, (uint)QuestItem, XYZ);
                await Coroutine.Wait(10000, () => !Core.Player.IsCasting);
            }

            await Coroutine.Sleep(Delay);

            if (QuestId > 0 && StepId > 0)
            {
                return false;
            }
            else
            {
                _done = true;
            }

            return false;
        }

        protected override Composite CreateBehavior() => new ActionRunCoroutine(cr => Main());

        protected override void OnDone()
        {
            if (GearSet > 0)
            {
                RoutineManager.PreferedRoutine = currentPrefRoutine;
                RoutineManager.PickRoutineFired -= OnPickRoutineFired;
            }
        }

        protected override void OnResetCachedDone() { }
    }
}
