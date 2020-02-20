using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoEmoteNpc")]
    public class SoEmoteNpc : HuntBehavior
    {
        public override bool HighPriority
        {
            get
            {
                return true;
            }
        }

        public override string StatusText
        {
            get
            {
                return $"Using {Emote} for {QuestName}.";
            }
        }

        [XmlAttribute("Emote")]
        public string Emote { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("DialogOption")]
        public int DialogOption { get; set; }

        public override Composite CustomLogic
        {
            get
            {
                return
                    new Decorator(r => (r as GameObject) != null,
                        new PrioritySelector(
                            new Decorator(ret => SelectYesno.IsOpen,
                                new Action(r =>
                                {
                                    SelectYesno.ClickYes();
                                })
                            ),
                            new ActionRunCoroutine(r => MoveAndStop(((GameObject)r).Location, UseDistance, false, StatusText)),
                            new ActionRunCoroutine(r => CreateEmoteObject(((GameObject)r), Emote))
                         )
                   );
            }
        }

        private async Task<bool> MoveAndStop(Vector3 location, float distance, bool stopInRange = false, string destinationName = null)
        {
            return await CommonTasks.MoveAndStop(new Pathing.MoveToParameters(location, destinationName), distance, stopInRange);
        }

        private async Task<bool> CreateEmoteObject(GameObject obj, string emote)
        {
            if (ShortCircut(obj))
            {
                return false;
            }

            if (obj.IsTargetable && obj.IsVisible)
            {
                if (SelectString.IsOpen)
                {
                    SelectString.ClickSlot((uint)DialogOption);
                    await Coroutine.Sleep(10);
                }

                if (Core.Player.IsMounted)
                {
                    return await CommonTasks.StopAndDismount();
                }

                obj.Target();

                ChatManager.SendChat("/" + emote);
                await Coroutine.Sleep(500);

                if (BlacklistAfter)
                {
                    Blacklist.Add(obj, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(BlacklistDuration), "BlacklistAfter");
                    await Coroutine.Sleep(500);
                }

                await Coroutine.Wait(5000, () => ShortCircut(obj));
            }

            return false;
        }

        protected override void OnStartHunt()
        {

        }

        protected override void OnDoneHunt()
        {

        }
    }
}