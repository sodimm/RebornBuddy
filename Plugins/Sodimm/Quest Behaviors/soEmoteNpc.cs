using Clio.XmlEngine;
using ff14bot.Managers;
using ff14bot.Objects;
using OrderBotTags.Behaviors;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoEmoteNpc")]
    public class SoEmoteNpc : SoHuntBehavior
    {
        #region Attributes

        [XmlAttribute("Emote")]
        public string Emote { get; set; }

        #endregion

        protected override void OnHuntStart()
        {
            Log($"Started for {QuestName}.");
        }

        protected override void OnHuntDone()
        {
            Log($"Objectives completed for {QuestName}.");
        }

        private async Task<bool> DoEmote(GameObject obj, string emote)
        {
            if (obj.IsTargetable && obj.IsVisible)
            {
                if (!Core.Player.HasTarget)
                    obj.Target();

                ChatManager.SendChat("/" + emote);
            }

            return !await ShortCircuit(obj);
        }

        protected override async Task<bool> CustomLogic()
        {
            if (Target != null)
            {
                if (await MoveAndStop(Target.Location, Distance, $"Moving to {Target.Name}", true, 0, MountDistance)) return true;

                if (await Dismount()) return true;

                if (await DoEmote(Target, Emote)) return true;
            }

            return false;
        }
    }
}
