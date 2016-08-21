namespace OrderBotTags.Behaviors
{
    using Buddy.Coroutines;
    using Clio.Utilities;
    using Clio.XmlEngine;
    using ff14bot.Behavior;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.Navigation;
    using ff14bot.NeoProfiles;
    using ff14bot.Objects;
    using ff14bot.RemoteWindows;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
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

        [XmlAttribute("PersistentObject")]
        [DefaultValue(false)]
        public bool PersistentObject { get; set; }

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

            if (ItemTarget !=0 && LacksAuraId > 0)
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

        protected static async Task<bool> ShortCircuit(GameObject obj, bool persistentObject = false, bool ignoreCombat = false, int mSecsPassed = 0)
        {
            var Timer = new Stopwatch();

            Timer.Start();

            while (obj.IsTargetable && obj.IsVisible || QuestLogManager.InCutscene)
            {
                if (mSecsPassed > 0 && Timer.ElapsedMilliseconds >= mSecsPassed)
                {
                    Timer.Stop();
                    return false;
                }

                if (persistentObject && !Me.IsCasting)
                {
                    Blacklist.Add(Me.CurrentTarget.ObjectId, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(30), "Persistent Object");
                    await Coroutine.Sleep(1000);
                    return false;
                }

                if (!ignoreCombat && Me.InCombat)
                {
                    await Coroutine.Sleep(1000);
                    return false;
                }

                if (Talk.DialogOpen)
                {
                    Talk.Next();
                    await Coroutine.Sleep(100);
                }

                await Coroutine.Yield();
            }

            Timer.Stop();
            return false;
        }

        protected abstract Task<bool> CustomLogic();

        private BagSlot Item { get { return InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemId); } }
        protected sealed override async Task Main()
        {
            if (!IsDone)
            {
                await CommonTasks.HandleLoading();

                await GoThere();

                if (Target != null && !Blacklist.Contains(Target.ObjectId))
                    await CustomLogic();
                else
                {
                    if (ItemTarget != 0)
                        await UseItem();

                    if (!Position.WithinHotSpot2D(Me.Location, 5f))
                        await MoveAndStop(Position, Distance, "Searching for a Target for " + QuestName);

                    if (Hotspots.Count != 0 && Position.WithinHotSpot2D(Me.Location, 5f))
                        Hotspots.Next();
                }

                await Coroutine.Yield();
            }
        }

        #region UseItem In Combat
        internal GameObject _obj
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

        internal async Task UseItem()
        {
            if (_obj != null)
            {
                while (CanUseItem)
                {
                    if (Item != null)
                    {
                        if (_obj.NpcId == ItemTarget)
                        {
                            Navigator.Stop();

                            if (Me.IsMounted)
                                await Dismount();

                            if (!Actionmanager.DoAction(Item.ActionType, Item.RawItemId, _obj) && Item.CanUse(_obj))
                            {
                                Log("Using {0} on {1}.", Item.Name, _obj.Name);
                                Item.UseItem(_obj);
                                await ShortCircuit(_obj, false, false, 5000);
                            }
                        }
                    }
                    await Coroutine.Yield();
                }
            }
            else return;
        }
        #endregion

        protected sealed override Composite CreateBehavior() { return new ActionRunCoroutine(cr => Main()); }

        private BlacklistFlags SoHuntBehaviorFlag = (BlacklistFlags)0x200000;
        protected virtual GameObject GetObject()
        {
            var possible = GameObjectManager.GetObjectsOfType<GameObject>(true, false).Where(obj => obj.IsVisible && obj.IsTargetable && NpcIds.Contains((int)obj.NpcId) && !Blacklist.Contains(obj.ObjectId)).OrderBy(obj => obj.DistanceSqr(Me.Location));

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