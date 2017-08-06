using Clio.XmlEngine;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoSimpleDuty")]

    class SoSimpleDuty : SimpleDutyTag
    {
        [XmlAttribute("ItemIds")]
        [XmlAttribute("ItemId")]
        public int[] ItemIds { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("UseItem")]
        public bool UseItem { get; set; }

        private string ItemNames;
        protected override void OnStart()
        {
            usedSlots = new HashSet<BagSlot>();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ItemIds.Length; i++)
            {
                var item = DataManager.GetItem((uint)ItemIds[i]);

                if (i == ItemIds.Length - 1)
                {
                    sb.Append($"{item.CurrentLocaleName}");
                }
            }
            ItemNames = sb.ToString();

            GameEvents.OnPlayerDied += OnPlayerDeathEvent;

            base.OnStart();
        }

        private bool doneUseItem;
        private HashSet<BagSlot> usedSlots;
        public GameObject Target => GameObjectManager.GetObjectByNPCId(InteractNpcId);
        public BagSlot Item => InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemIds.FirstOrDefault());
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(r => Request.IsOpen,
                    new Action(r =>
                    {
                        var items = InventoryManager.FilledInventoryAndArmory.ToArray();
                        for (int i = 0; i < ItemIds.Length; i++)
                        {
                            BagSlot item;
                            item = items.FirstOrDefault(z => z.RawItemId == ItemIds[i] && !usedSlots.Contains(z));

                            if (item == null)
                            {
                                LogError($"We don't have any items with an id of {ItemIds[i]}.");
                            }
                            else
                            {
                                item.Handover();
                                usedSlots.Add(item);
                            }
                        }

                        usedSlots.Clear();
                        Request.HandOver();
                    })
                ),
                new Decorator(r => Core.Player.HasTarget && UseItem && !doneUseItem,
                    new Action(r =>
                    {
                        var targetNpc = GameObjectManager.GetObjectByNPCId(InteractNpcId);
                        foreach (BagSlot slot in InventoryManager.FilledSlots)
                        {
                            if (slot.RawItemId == ItemIds.FirstOrDefault())
                            {
                                Log($"Using {slot.EnglishName} on {targetNpc.EnglishName}.");
                                slot.UseItem(targetNpc);
                            }
                        }

                        if (Core.Player.IsCasting)
                        {
                            doneUseItem = true;
                        }

                        if (SelectYesno.IsOpen)
                        {
                            doneUseItem = true;
                        }
                    })
                ),
                base.CreateBehavior()
            );
        }

        protected override void OnDone()
        {
            doneUseItem = false;
            GameEvents.OnPlayerDied -= OnPlayerDeathEvent;
            base.OnDone();
        }

        void OnPlayerDeathEvent(object sender, EventArgs e)
        {
            doneUseItem = false;
        }

        protected override void OnResetCachedDone()
        {
            doneUseItem = false;
            base.OnResetCachedDone();
        }
    }
}
