using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoRegisterDailies")]
    public class SoRegisterDailiesTag : SoProfileBehavior
    {
        public override bool HighPriority { get { return true; } }

        public override bool IsDone
        {
            get
            {
                return _done;
            }
        }

        [XmlAttribute("QuestIds")]
        public int[] QuestIds { get; set; }

        protected override void OnTagStart()
        {
            Log("Registering Daily Quests");
        }

        protected override async Task Main()
        {
            await CommonTasks.HandleLoading();

            QuestLogManager.RegisterDailies(QuestIds);

            Log("Registration Complete");

            _done = true;
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
        }
    }
}
