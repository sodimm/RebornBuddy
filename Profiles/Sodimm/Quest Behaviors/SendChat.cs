using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Managers;
using System.ComponentModel;
using System.Threading.Tasks;
using TreeSharp;
using System;

namespace ff14bot.NeoProfiles
{
    [XmlElement("SendChat")]
    public class SendChat : ProfileBehavior
    {
        [XmlAttribute("GearSet")]
        [DefaultValue(0)]
        public int GearSet { get; set; }

        [XmlAttribute("RemoveAura")]
        [DefaultValue(0)]
        public int Aura { get; set; }

        [XmlAttribute("DoAction")]
        [DefaultValue(null)]
        public string DoAction { get; set; }

        [XmlAttribute("DoActionTarget")]
        [DefaultValue(null)]
        public string DoActionTarget { get; set; }

        [XmlAttribute("Delay")]
        [DefaultValue(1500)]
        public int Delay { get; set; }

        private string currentPrefRoutine;
        protected override void OnStart()
        {
            if (GearSet > 0)
            {
                if (RoutineManager.PreferedRoutine != null)
                {
                    Log("Saving users current Prefered Routine setting.");
                    currentPrefRoutine = RoutineManager.PreferedRoutine;
                }

                Log("Adding Routine Fired Event Handler.");
                RoutineManager.PickRoutineFired += OnPickRoutineFired;
            }
        }

        void OnPickRoutineFired(object sender, EventArgs e)
        {
            Log("Temporarily setting Kupo as Prefered Routine.");
            RoutineManager.PreferedRoutine = "Kupo";
        }

        private bool _done;
        public override bool IsDone
        {
            get
            {
                return _done;
            }
        }

        private async Task<bool> Main()
        {
            if (GearSet > 0)
            {
                Log($"Sending /gearset chat command in {Delay}ms.");

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/gs change " + GearSet);

                await Coroutine.Sleep(Delay);

                _done = true;
            }

            if (Aura > 0)
            {
                var auraId = Core.Player.GetAuraById((uint)Aura);

                string thisAura = null;

                if (Core.Player.HasAura((uint)Aura))
                    thisAura = auraId.LocalizedName.ToString();
                else
                    _done = true;

                Log($"Sending /statusoff chat command for {thisAura} in {Delay}ms.");

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/statusoff \"" + thisAura + "\"");

                await Coroutine.Sleep(Delay);

                _done = true;
            }

            if (DoAction != null)
            {
                Log($"Sending /action <me> command {DoAction} in {Delay}ms.");

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/ac \"" + DoAction + "\" <me>");

                await Coroutine.Sleep(Delay);

                _done = true;
            }

            if (DoActionTarget != null)
            {
                Log($"Sending /action <target> command {DoActionTarget} in {Delay}ms.");

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/ac \"" + DoActionTarget + "\" <target>");

                await Coroutine.Sleep(Delay);

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
            if (GearSet > 0)
            {
                Log("Restoring users Prefered Routine setting.");
                RoutineManager.PreferedRoutine = currentPrefRoutine;
                Log("Removing Prefered Routine Event Handler.");
                RoutineManager.PickRoutineFired -= OnPickRoutineFired;
            }
        }

    }
}
