using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoSendChat")]
    public class SoSendChat : ProfileBehavior
    {
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

        [XmlAttribute("SwitchClass")]
        [DefaultValue(null)]
        public string SwitchClass { get; set; }

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
            if (!string.IsNullOrWhiteSpace(SwitchClass))
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

        public override bool IsDone
        {
            get
            {
                if (QuestId > 0 && StepId > 0)
                {
                    return IsStepComplete;
                }

                return _done;
            }
        }

        private async Task<bool> Main()
        {
            if (Core.Player.IsMounted)
            {
                await CommonTasks.StopAndDismount();
            }

            if (Core.Player.InCombat)
            {
                await Coroutine.Wait(5000, () => !Core.Player.InCombat);
                return true;
            }

            await Coroutine.Sleep(Delay);

            if (!string.IsNullOrWhiteSpace(SwitchClass))
            {
                ClassJobType _job;
                Enum.TryParse(SwitchClass, true, out _job);
                if (GearsetManager.ActiveGearset.Class == _job)
                {
                    Log("Desired Gearset is already active");
                    _done = true;
                    return false;
                }

                foreach (var gs in GearsetManager.GearSets)
                {
                    if (gs.Class == _job)
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

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(cr => Main());
        }

        protected override void OnDone()
        {
            if (!string.IsNullOrWhiteSpace(SwitchClass))
            {
                RoutineManager.PreferedRoutine = currentPrefRoutine;
                RoutineManager.PickRoutineFired -= OnPickRoutineFired;
            }
        }
    }
}