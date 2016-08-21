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
        public override bool HighPriority
        {
            get
            {
                return false;
            }
        }

        #region 

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

        protected async override Task Main()
        {
            if (GearSet > 0)
            {
                Log("Changing Gear Set in {0} ms.", Delay);

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/gs change " + GearSet);

                await Coroutine.Sleep(500);

                Log("Changed Gear Set to {0}.", Me.CurrentJob.ToString());

                _done = true;
            }

            if (Aura > 0)
            {
                var auraId = Me.GetAuraById((uint)Aura);

                string thisAura = null;

                if (Me.HasAura(Aura))
                    thisAura = auraId.LocalizedName.ToString();
                else
                    _done = true;

                Log("Clearing Aura {0}in {1} ms.", thisAura, Delay);

                await Coroutine.Sleep(Delay);


                ChatManager.SendChat("/statusoff \"" + thisAura + "\"");

                await Coroutine.Sleep(500);

                _done = true;
            }

            if (Raw != null)
            {
                Log("Sending command [{0}] in {1} ms.", Raw, Delay);

                await Coroutine.Sleep(Delay);

                ChatManager.SendChat("/" + Raw);

                await Coroutine.Sleep(500);

                _done = true;
            }
        }
    }
}
