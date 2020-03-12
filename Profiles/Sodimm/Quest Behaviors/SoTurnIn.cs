using Clio.XmlEngine;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using System.Collections.Generic;
using System.ComponentModel;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoTurnIn")]
    public class SoTurnIn : TurnInTag
    {
        [DefaultValue(new int[0])]
        [XmlAttribute("DialogOption")]
        public int[] DialogOption { get; set; }

        [DefaultValue("")]
        [XmlAttribute("Emote")]
        public string Emote { get; set; }

        private bool doneEmote;
        private readonly Queue<int> selectStringIndex = new Queue<int>();

        protected override void OnStart()
        {
            if (DialogOption.Length > 0)
            {
                foreach (var i in DialogOption) 
                {
                    selectStringIndex.Enqueue(i);
                }
            }

            base.OnStart();
        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => !doneEmote && !string.IsNullOrWhiteSpace(Emote),
                    new Action(r =>
                    {
                        GameObjectManager.GetObjectByNPCId((uint)NpcId).Target();
                        ChatManager.SendChat("/" + Emote);
                        doneEmote = true;
                    })
                ),
                new Decorator(ret => SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(ret => SelectString.IsOpen,
                    new Action(r =>
                    {
                        if (selectStringIndex.Count > 0) { SelectString.ClickSlot((uint)selectStringIndex.Dequeue()); }
                        else { SelectString.ClickSlot(0); }
                    })
                ),
                base.CreateBehavior()
            );
        }

        protected override void OnDone()
        {
            doneEmote = false;
            base.OnDone();
        }

        protected override void OnResetCachedDone()
        {
            doneEmote = false;
            base.OnResetCachedDone();
        }
    }
}