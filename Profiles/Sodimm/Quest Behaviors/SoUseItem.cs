using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseItem")]
    public class SoUseItemTag : HuntBehavior
    {
        [XmlAttribute("ItemId")]
        public uint ItemId { get; set; }

        [DefaultValue(new int[0])]
        [XmlAttribute("DialogOption")]
        public int[] DialogOption { get; set; }

        private static readonly Queue<int> selectStringIndex = new Queue<int>();

        protected override void OnStartHunt()
        {
            if (DialogOption.Length > 0)
            {
                foreach (var i in DialogOption) 
                {
                    selectStringIndex.Enqueue(i);
                }
            }
        }

        protected override void OnDoneHunt()
        {

        }

        public override Composite CustomLogic
        {
            get
            {
                return new Decorator(r => (r as GameObject) != null,
                    new PrioritySelector(
                        CommonBehaviors.MoveAndStop(r => ((GameObject)r).Location, r => UseDistance, false, r => null),
                            new ActionRunCoroutine(async r => await UseItem(ItemId, (r as GameObject), WaitTime, BlacklistAfter, BlacklistDuration))
                        )
                    );
            }
        }

        public override Composite CustomCombatLogic
        {
            get
            {
                return new PrioritySelector(r => Poi.Current.BattleCharacter,
                    new ActionRunCoroutine(async r => await UseItem(ItemId, Poi.Current.BattleCharacter, WaitTime, BlacklistAfter, BlacklistDuration))
                    );
            }
        }

        public static async Task<bool> UseItem(uint itemId, GameObject obj, int waitTime = 0, bool blacklistAfter = false, int blacklistDuration = 30)
        {
            var bagSlot = InventoryManager.FilledSlots.FirstOrDefault(s => s.RawItemId == itemId);

            if (bagSlot == null)
            {
                return false;
            }

            if (obj == null)
            { 
                return false;
            }

            while (true)
            {
                if (Core.Me.IsDead)
                {
                    return false;
                }

                if (SelectString.IsOpen)
                {
                    if (selectStringIndex.Count > 0)
                    {
                        SelectString.ClickSlot((uint)selectStringIndex.Dequeue());
                    }
                    else 
                    { 
                        SelectString.ClickSlot(0); 
                    }
                }

                if (Talk.DialogOpen)
                {
                    Talk.Next();
                    await Coroutine.Sleep(300);
                    continue;
                }

                if (MovementManager.IsMoving)
                {
                    await CommonTasks.StopMoving();
                    await Coroutine.Sleep(300);
                    continue;
                }

                if (Core.Me.IsMounted)
                {
                    await CommonTasks.StopAndDismount();
                    await Coroutine.Sleep(300);
                    continue;
                }

                if (Core.Me.CurrentTarget != obj)
                {
                    obj.Target();
                    await Coroutine.Sleep(300);
                    continue;
                }

                if (bagSlot.Item.IsGroundTargeting)
                {
                    bagSlot.UseItem(obj.Location);
                }
                else
                {
                    bagSlot.UseItem(obj);
                }

                if (await Coroutine.Wait(1000, () => Core.Me.IsCasting))
                {
                    if (await Coroutine.Wait(15000, () => !Core.Me.IsCasting))
                    {
                        if (await Coroutine.Wait(5000, () => !obj.IsValid || !obj.IsTargetable || !obj.IsVisible || !bagSlot.CanUse(obj)))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            if (waitTime > 0)
            { 
                await Coroutine.Sleep(waitTime);
            }

            if (blacklistAfter)
            {
                Blacklist.Add(obj, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(blacklistDuration), "BlacklistAfter");
            }

            return true;
        }
    }
}