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

        [XmlAttribute("ItemId")]
        public uint ItemId { get; set; }

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

        private GrindArea GrindArea;
        private void CreateGrindArea()
        {
            if (NeoProfileManager.CurrentGrindArea == null)
            {
                Log("Creating GrindArea");
                Hotspots.Add(new HotSpot(XYZ, Radius));
                Hotspots.IsCyclic = true;
                Hotspots.Index = 0;

                GrindArea = new GrindArea()
                {
                    Hotspots = Hotspots.ToList(),
                    TargetMobs = NpcIds.Select(r => new TargetMob() { Id = r }).ToList(),
                    Name = "SoGrindTag generated GrindArea"
                };

                NeoProfileManager.CurrentProfile.KillRadius =  (Me.CombatReach * Me.CombatReach);
                NeoProfileManager.UpdateGrindArea();
                NeoProfileManager.UpdateCurrentProfileBehavior();
            }
        }

        protected override void OnTagStart()
        {
            GameEvents.OnPlayerDied += onPlayerDied;
            HotspotManager.Clear();
            NeoProfileManager.CurrentGrindArea = null;

            if (ItemTarget != 0 && LacksAuraId > 0)
                TreeHooks.Instance.AddHook("Combat", new ActionRunCoroutine(cr => UseItem()));

            Log("Started");
        }

        void onPlayerDied(object sender, EventArgs e)
        {
            NeoProfileManager.CurrentGrindArea = null;
        }

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
                    if (Poi.Current.BattleCharacter.NpcId == _obj.NpcId && Poi.Current.BattleCharacter.HasAura(LacksAuraId))
                        return false;
                    else
                        return true;
                }
                else return false;
            }
        }

        private BagSlot Item { get { return InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemId); } }
        internal async Task UseItem()
        {
            if (_obj != null)
            {
                while (CanUseItem)
                {
                    if (Item != null)
                    {
                        if (Poi.Current.BattleCharacter.NpcId == ItemTarget)
                        {
                            Navigator.Stop();
                            Log("Using {0} on {1}", Item.Name, _obj.Name);
                            Item.UseItem(_obj);
                        }
                    }
                    await Coroutine.Yield();
                }
            }
            else return;
         }

        protected override async Task Main()
        {
            await CommonTasks.HandleLoading();

            await GoThere();

            if (NeoProfileManager.CurrentGrindArea == null)
            {
                CreateGrindArea();

                NeoProfileManager.CurrentGrindArea = GrindArea;

                // Debug
                foreach (var mob in GrindArea.TargetMobs)
                    Log("Added NpcId {0} with Weight {1} to the GrindArea.", mob.Id, mob.Weight);
            }

            if (!Hotspots.Current.WithinHotSpot(Me.Location))
                await MoveAndStop(XYZ, Radius / 5f, "Moving to HotSpot");

            await DoHook();
        }

        protected Task DoHook()
        {
           return CommonTasks.ExecuteCoroutine(Hook());
        }

        protected Composite Hook()
        {
            return new PrioritySelector(
                new HookExecutor("HotspotPoi"),
                new HookExecutor("SetCombatPoi"),
                new ActionAlwaysSucceed()
            );
        }

        protected override void OnTagDone()
        {
            if (ItemTarget != 0 && LacksAuraId > 0)
                TreeHooks.Instance.RemoveHook("Combat", new ActionRunCoroutine(cr => UseItem()));

            GameEvents.OnPlayerDied -= onPlayerDied;

            NeoProfileManager.CurrentGrindArea = null;

            KillCounter = 0;
        }
    }
}
