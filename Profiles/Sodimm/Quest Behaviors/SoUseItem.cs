using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using System;
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

        protected override void OnStartHunt() { }

        protected override void OnDoneHunt() { }

        public override Composite CustomLogic
        {
            get
            {
                return new Decorator(r => (r as GameObject) != null,
                    new PrioritySelector(
                        CommonBehaviors.MoveAndStop(r => ((GameObject)r).Location, r => UseDistance, false, r => null),
                            new ActionRunCoroutine(async r => await UseItem((r as GameObject)))
                        )
                    );
            }
        }

        public override Composite CustomCombatLogic
        {
            get
            {
                return new PrioritySelector(r => Poi.Current.BattleCharacter,
                    new ActionRunCoroutine(async r => await UseItem(Poi.Current.BattleCharacter))
                    );
            }
        }

        private BagSlot Item => InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemId) ?? null;
        private async Task<bool> UseItem(GameObject who)
        {
            if (Item == null || who == null) { return false; }

            if (Core.Me.IsMounted) { await CommonTasks.StopAndDismount(); }

            if (who.IsTargetable) { who.Target(); }

            if (Item.Item.IsGroundTargeting) { Item.UseItem(who.Location); } else { Item.UseItem(who); }

            if (await Coroutine.Wait(5000, () => Core.Me.IsCasting))
            {
                if (await Coroutine.Wait(15000, () => !Core.Me.IsCasting))
                {
                    if (await Coroutine.Wait(5000, () => ShortCircut(who))) { return false; }
                }
            }
            else
            {
                Log("We are not using the item for some reason!");
                return false;
            }

            if (WaitTime > 0) { await Coroutine.Sleep(WaitTime); }

            if (BlacklistAfter) { Blacklist.Add(who, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(BlacklistDuration), "BlacklistAfter"); }

            return true;
        }
    }
}
