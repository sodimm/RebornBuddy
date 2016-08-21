using Clio.XmlEngine;
using ff14bot.Managers;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoEmoteNpc")]
    public class SoEmoteNpc : SoHuntBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return false;
            }
        }

        #region Attributes

        [XmlAttribute("Emote")]
        public string Emote { get; set; }

        #endregion

        protected override void OnHuntStart()
        {
            Log("Started");
        }

        protected override void OnHuntDone()
        {
            Log("Done");
        }

        protected override async Task<bool> CustomLogic()
        {
            if (Target != null)
            {
                await MoveAndStop(Target.Location, Distance, "Moving to " + Target.EnglishName);

                if (InPosition(Target.Location, Distance))
                {
                    if (Target.IsTargetable && Target.IsVisible)
                    {
                        await Dismount();
                       // StatusText = "Using emote [" + Emote + "] on " + Target.Name;
                        Log("Emoting {0} on {1}.", Emote, Target.Name);
                        if (!Me.HasTarget)
                            Target.Target();
                        ChatManager.SendChat("/" + Emote);
                        await ShortCircuit(Target, persistentObject: PersistentObject, mSecsPassed: 10000);
                    }
                }
            }
            return false;
        }
    }
}
