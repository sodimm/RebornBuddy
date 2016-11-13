namespace OrderBotTags.Behaviors
{
    using Buddy.Coroutines;
    using Clio.Utilities;
    using Clio.XmlEngine;
    using ff14bot;
    using ff14bot.Behavior;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.Navigation;
    using ff14bot.NeoProfiles;
    using ff14bot.Objects;
    using ff14bot.RemoteAgents;
    using ff14bot.RemoteWindows;
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using TreeSharp;

    public abstract class SoHuntBehavior : SoProfileBehavior
    {
        #region Defaults
        protected SoHuntBehavior()
        {
            Hotspots = new IndexedList<HotSpot>();
        }
        #endregion

        #region Attributes
        [XmlAttribute("UseTimes")]
        [DefaultValue(0)]
        public int UseTimes { get; set; }

        [XmlAttribute("NpcIds")]
        public int[] NpcIds { get; set; }

        [XmlAttribute("ItemId")]
        public uint ItemId { get; set; }

        [XmlElement("HotSpots")]
        public IndexedList<HotSpot> Hotspots { get; set; }

        [XmlAttribute("Radius")]
        [DefaultValue(50f)]
        public float Radius { get; set; }

        [XmlAttribute("ItemTarget")]
        [DefaultValue(0)]
        public int ItemTarget { get; set; }

        [XmlAttribute("LacksAura")]
        [DefaultValue(0)]
        public int LacksAuraId { get; set; }

        [XmlAttribute("UseHealthPercent")]
        [DefaultValue(20)]
        public float UseHealthPercent { get; set; }

        #endregion

        public sealed override bool IsDone
        {
            get
            {
                if (UseTimes > 0)
                {
                    var trgt = Target;
                    if (NumberOfTimesCompleted >= UseTimes)
                        return true;

                    GC.KeepAlive(trgt);
                }
                if (Conditional != null)
                {
                    var cond = !Conditional();
                    return cond;
                }
                if (!HasQuest)
                    return true;
                if (IsQuestComplete)
                    return true;
                if (IsStepComplete)
                    return true;
                if (IsObjectiveCountComplete)
                    return true;
                if (IsObjectiveComplete)
                    return true;

                return false;
            }
        }

        protected virtual void OnHuntStart() { }

        protected virtual void OnHuntDone() { }

        protected sealed override void OnTagStart()
        {
            FlightCheck();
            GetQuestData();
            SetupConditional();

            if (Hotspots.Count == 0)
            {
                if (XYZ == Vector3.Zero)
                {
                    LogError("No hotspots and no XYZ provided, this is an invalid combination for this behavior");
                    return;
                }
                Hotspots.Add(new HotSpot(XYZ, Radius));
            }

            Hotspots.IsCyclic = true;
            Hotspots.Index = 0;

            if (ItemTarget != 0 && LacksAuraId > 0)
                TreeHooks.Instance.AddHook("Combat", new ActionRunCoroutine(cr => UseItem()));

            OnHuntStart();
        }

        protected sealed override void OnTagDone()
        {
            if (ItemTarget != 0 && LacksAuraId > 0)
                TreeHooks.Instance.RemoveHook("Combat", new ActionRunCoroutine(cr => UseItem()));

            NeoProfileManager.CurrentGrindArea = null;

            OnHuntDone();
        }

        public HotSpot Position { get { return Hotspots.CurrentOrDefault; } }

        private GameObject _target;
        private int NumberOfTimesCompleted;
        public GameObject Target
        {
            get
            {
                if (_target != null)
                {
                    if (!_target.IsValid || !_target.IsTargetable || !_target.IsVisible)
                    {
                        NumberOfTimesCompleted++;
                    }
                    else
                    {
                        return _target;
                    }
                }
                _target = GetObject();
                return _target;
            }
        }

        protected async Task<bool> ShortCircuit(GameObject obj)
        {
            while (obj.IsTargetable && obj.IsVisible || QuestLogManager.InCutscene)
            {
                if (IsDone)
                    return false;

                if (Talk.DialogOpen)
                {
                    Talk.Next();
                    await Coroutine.Sleep(100);
                }

                if (QuestLogManager.InCutscene)
                {
                    if (AgentCutScene.Instance.CanSkip && !SelectString.IsOpen)
                    {
                        AgentCutScene.Instance.PromptSkip();
                        if (await Coroutine.Wait(600, () => SelectString.IsOpen))
                        {
                            SelectString.ClickSlot(0);
                            await Coroutine.Sleep(1000);
                        }
                    }
                }

                await Coroutine.Yield();
            }

            return false;
        }

        protected abstract Task<bool> CustomLogic();

        protected sealed override async Task<bool> Main()
        {
            await CommonTasks.HandleLoading();

            if (Target != null && await CustomLogic()) return true;

            if (ItemTarget != 0 && await UseItem()) return true;

            if (!Position.WithinHotSpot2D(Core.Player.Location, Distance * Distance) && 
                await MoveAndStop(Position, Distance, "Moving to HotSpot", true, (ushort)MapId, MountDistance)) return true;

            if (Hotspots.Count != 0 && Navigator.InPosition(Position, Core.Player.Location, 5f))
                Hotspots.Next();

            return false;
        }

        #region UseItem In Combat

        private BagSlot Item
        {
            get
            {
                return InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemId);
            }
        }

        private GameObject _obj
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
                if (_obj != null && _obj.IsValid)
                {
                    if (LacksAuraId > 0 && Poi.Current.BattleCharacter.NpcId == _obj.NpcId && Poi.Current.BattleCharacter.HasAura(LacksAuraId))
                        return false;
                    else
                        return true;
                }
                else return false;
            }
        }

        public async Task<bool> UseItem()
        {
            if (_obj != null)
            {
                while (CanUseItem)
                {
                    if (Item != null)
                    {
                        if (_obj.NpcId == ItemTarget)
                        {
                            Navigator.PlayerMover.MoveStop();

                            if (await Dismount()) return true;

                            if (!Actionmanager.DoAction(Item.ActionType, Item.RawItemId, _obj) && Item.CanUse(_obj))
                            {
                                Log("Using {0} on {1}.", Item.Name, _obj.Name);
                                Item.UseItem(_obj);
                            }
                        }
                        return await ShortCircuit(_obj);
                    }
                }
            }

            return false;
        }

        #endregion

        protected sealed override Composite CreateBehavior() { return new ActionRunCoroutine(cr => Main()); }

        private BlacklistFlags SoHuntBehaviorFlag = (BlacklistFlags)0x200000;
        protected virtual GameObject GetObject()
        {
            var possible = GameObjectManager.GetObjectsOfType<GameObject>(true, false).Where(obj => obj.IsVisible && obj.IsTargetable && NpcIds.Contains((int)obj.NpcId) && !Blacklist.Contains(obj.ObjectId)).OrderBy(obj => obj.DistanceSqr(Core.Player.Location));

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
    }
}
