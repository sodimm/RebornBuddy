using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using OrderBotTags.Behaviors;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
    [XmlElement("SoGrind")]
    public class SoGrindTag : SoProfileBehavior
    {
        protected SoGrindTag()
        {
            Hotspots = new IndexedList<HotSpot>();
        }

        [XmlAttribute("NpcIds")]
        public int[] NpcIds { get; set; }

        [XmlElement("HotSpots")]
        public IndexedList<HotSpot> Hotspots { get; set; }

        [XmlAttribute("Radius")]
        [DefaultValue(50f)]
        public float Radius { get; set; }

        [XmlAttribute("LacksAura")]
        [DefaultValue(0)]
        public int LacksAuraId { get; set; }

        [XmlAttribute("ItemTarget")]
        [DefaultValue(0)]
        public int ItemTarget { get; set; }

        [XmlAttribute("ItemId")]
        public uint ItemId { get; set; }

        [XmlAttribute("KillCount")]
        [DefaultValue(0)]
        public int KillCount { get; set; }
        private int KillCounter = 0;

        private uint lastKill;
        public override bool IsDone
        {
            get
            {
                if (KillCount > 0)
                {
                    var trgt = Poi.Current.BattleCharacter;
                    if (trgt != null && NpcIds.Contains((int)trgt.NpcId) && (trgt as Character).IsDead && lastKill != trgt.ObjectId)
                    {
                        lastKill = trgt.ObjectId;
                        KillCounter++;
                        Log("KillCount updated to {0}", KillCounter);
                    }
                    else
                    if (KillCounter >= KillCount)
                        return true;

                    GC.KeepAlive(trgt);
                }
                if (IsStepComplete)
                    return true;
                if (!HasQuest)
                    return true;
                if (IsObjectiveComplete)
                    return true;
                if (IsObjectiveCountComplete)
                    return true;

                return false;
            }
        }

        public HotSpot Position
        {
            get
            {
                return Hotspots.CurrentOrDefault;
            }
        }

        private void CreateHotSpot()
        {
            if (Hotspots.Count == 0)
            {
                Hotspots.Add(new HotSpot(XYZ, Radius));
                Hotspots.IsCyclic = true;
                Hotspots.Index = 0;
                NeoProfileManager.UpdateCurrentProfileBehavior();

            }
            else return;
        }

        protected override void OnTagStart()
        {
            if (ItemTarget != 0 && LacksAuraId > 0)
                TreeHooks.Instance.AddHook("Combat", new ActionRunCoroutine(cr => UseItem()));

            HotspotManager.Clear();
            CreateHotSpot();
            Log("Started");
        }

        #region UseItem In Combat

        private BagSlot Item { get { return InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemId); } }
        private GameObject TargetForUseItem
        {
            get
            {
                try
                {
                    var obj = GameObjectManager.GetObjectByNPCId((uint)ItemTarget);

                    if (obj != null && !(obj as BattleCharacter).TappedByOther)
                        return obj;
                }
                catch { }
                return null;
            }
        }

        private bool CanUseItem
        {
            get
            {
                if (TargetForUseItem != null && TargetForUseItem.IsValid)
                {
                    if (LacksAuraId > 0 && Poi.Current.BattleCharacter.NpcId == TargetForUseItem.NpcId && Poi.Current.BattleCharacter.HasAura(LacksAuraId))
                        return false;
                    else
                        return true;
                }
                else return false;
            }
        }

        private async Task UseItem()
        {
            if (TargetForUseItem != null)
            {
                while (CanUseItem)
                {
                    if (Item != null)
                    {
                        if (TargetForUseItem.NpcId == ItemTarget)
                        {
                            Navigator.Stop();

                            if (Me.IsMounted)
                                await Dismount();

                            if (!Actionmanager.DoAction(Item.ActionType, Item.RawItemId, TargetForUseItem) && Item.CanUse(TargetForUseItem))
                            {
                                Log("Using {0} on {1}.", Item.Name, TargetForUseItem.Name);
                                Item.UseItem(TargetForUseItem);
                                await Coroutine.Wait(6000, () => !Me.IsCasting && (TargetForUseItem as BattleCharacter).HasAura(LacksAuraId));
                            }
                        }
                    }
                    await Coroutine.Yield();
                }
            }
            else return;
        }

        #endregion

        protected override async Task Main()
        {
            await CommonTasks.HandleLoading();

            await GoThere();

            if (Target != null && Poi.Current.Type == PoiType.None)
            {
                //await MoveAndStop(Target.Location, Me.CombatReach, "Found " + Target.EnglishName);
                Poi.Current = new Poi(Target, PoiType.Kill);
            }
            else
            {
                if (!Position.WithinHotSpot2D(Me.Location, 5f))
                    await MoveAndStop(Position, Distance, "Searching for a Target for " + QuestName);

                //if (Hotspots.Count != 0 && Position.WithinHotSpot2D(Me.Location, 5f))
                //    Hotspots.Next();
            }
        }

        private BattleCharacter _target;
        public BattleCharacter Target
        {
            get
            {
                if (_target != null && _target.IsAlive)
                    return _target;

                _target = GetTarget();
                return _target;
            }
        }

        protected BattleCharacter GetTarget()
        {
            var possible = GameObjectManager.GetObjectsOfType<BattleCharacter>(true, false).Where(obj => obj.IsVisible && obj.IsTargetable && obj.CanAttack && !obj.IsDead && !obj.TappedByOther && NpcIds.Contains((int)obj.NpcId)).OrderBy(obj => obj.DistanceSqr(Me.Location));

            float closest = float.MaxValue;
            foreach (var obj in possible)
            {
                if (obj.DistanceSqr() < 1)
                    return obj;

                HotSpot target = null;
                foreach (var hotspot in Hotspots)
                {
                    if (hotspot.WithinHotSpot2D(obj.Location))
                    {
                        var dist = hotspot.Position.DistanceSqr(obj.Location);
                        if (dist < closest)
                        {
                            closest = dist;
                            target = hotspot;
                        }
                    }
                }

                if (target != null)
                {
                    while (Hotspots.Current != target)
                    {
                        Hotspots.Next();
                    }
                    return obj;
                }
            }
            return null;
        }

        protected override void OnTagDone()
        {
            if (ItemTarget != 0 && LacksAuraId > 0)
                TreeHooks.Instance.RemoveHook("Combat", new ActionRunCoroutine(cr => UseItem()));

            KillCounter = 0;
        }
    }
}
