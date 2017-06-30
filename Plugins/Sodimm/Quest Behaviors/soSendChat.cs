using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Managers;
using OrderBotTags.Behaviors;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles
{
    [XmlElement("SoSendCommand")]
    public class SoSendCommand : SoProfileBehavior
    {
        #region Attributes

        [XmlAttribute("GearSet")]
        [DefaultValue(0)]
        public int GearSet { get; set; }

        [XmlAttribute("RemoveAura")]
        [DefaultValue(0)]
        public int Aura { get; set; }

        [XmlAttribute("Raw")]
        [DefaultValue(null)]
        public string Raw { get; set; }

        [XmlAttribute("Delay")]
        [DefaultValue(2500)]
        public int Delay { get; set; }

        #endregion

        public override bool IsDone
        {
            get
            {
                if (QuestId > 0 & !HasQuest)
                    return true;

                return _done;
            }
        }

        protected async override Task<bool> Main()
        {
            if (GearSet > 0)
            {
                Log($"Changing Gear Set in {Delay} ms.");

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/gs change " + GearSet);

                await Coroutine.Sleep(500);

                Log($"Changed Gear Set to {Core.Player.CurrentJob.ToString()}");

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

                Log($"Clearing {thisAura} in {Delay} ms.");

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/statusoff \"" + thisAura + "\"");

                await Coroutine.Sleep(500);

                _done = true;
            }

            if (Raw != null)
            {
                Log($"Sending command [{Raw}] in {Delay} ms.");

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/" + Raw);

                await Coroutine.Sleep(500);

                _done = true;
            }

            return false;
        }
    }
}
