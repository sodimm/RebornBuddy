using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using OrderBotTags.Behaviors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ff14bot.NeoProfiles
{
    [XmlElement("SoFate")]
    public class SoFate : SoProfileBehavior
    {
        [XmlAttribute("FateId")]
        public int FateId { get; set; }

        [XmlAttribute("Delay")]
        [DefaultValue(20)]
        public int Delay { get; set; }

        private IndexedList<HotSpot> hotSpots { get; set; }

        protected SoFate()
        {
            hotSpots = new IndexedList<HotSpot>();
        }

        public override bool IsDone
        {
            get
            {
                if (!HasQuest)
                    return true;

                if (HasItems)
                    return true;

                return false;
            }
        }

        protected override void OnTagStart()
        {
            hotSpots.Clear();
            NeoProfileManager.CurrentGrindArea = null;
            Log("Started");
        }

        private int BossId;

        private readonly HashSet<uint> HiddenBossFates = new HashSet<uint>()
        {
            // Watch Your Tongue
            575,
        };

        private void GetBoss()
        {
            //Watch Your Tongue
            if (FateId == 575)
                BossId = 343;
        }

        private FateData ThisFate
        {
            get
            {
                foreach (FateData f in FateManager.ActiveFates)
                    if (f.Id == FateId)
                        return f;
                return null;
            }
        }

        private bool FateActive
        {
            get
            {
                try
                {
                    if (FateManager.GetFateById((uint)FateId).IsValid)
                        return true;
                }
                catch
                {
                }
                return false;
            }
        }

        private GrindArea FateGrindArea;

        private async Task CreateGrindArea()
        {
            if (NeoProfileManager.CurrentGrindArea == null)
            {
                if (Delay > 0)
                {
                    // start time irregularity, sleep for a second for the fatedata to populate
                    await Coroutine.Sleep(1000);

                    await WaitDelay();
                }

                Log("Creating GrindArea for " + ThisFate.Name);

                GetBoss();

                hotSpots.Add(new HotSpot(ThisFate.Location, ThisFate.Radius));

                FateGrindArea = new GrindArea()
                {
                    Hotspots = hotSpots.ToList(),

                    TargetMobs = GameObjectManager.GetObjectsOfType<BattleCharacter>(true, false).Where(obj => obj.FateId == ThisFate.Id).Select(r => (int)r.NpcId).Distinct().Select(x => new TargetMob() { Id = x }).ToList(),

                    Name = ThisFate.Name.ToString()
                };

                if (ThisFate.Icon == FateIconType.Boss && HiddenBossFates.Contains(ThisFate.Id))
                {
                    Log("Fate contains a hidden Boss. Adding NpcId {1} as a TargetMob.", ThisFate.Name, BossId);

                    FateGrindArea.TargetMobs.Add(new TargetMob() { Id = BossId, Weight = 1 });
                }

                NeoProfileManager.CurrentProfile.KillRadius = 80f;

                NeoProfileManager.UpdateGrindArea();
            }
        }

        private bool ShouldWait
        {
            get
            {
                return ThisFate.Started.AddSeconds(Delay) > DateTime.Now.ToUniversalTime();
            }
        }

        private async Task WaitDelay()
        {
            while (ShouldWait)
            {
                StatusText = "Waiting " + Delay + "s before Creating GrindArea";
                await Coroutine.Yield();
            }
        }

        private static Task HookExecutor()
        {
            return CommonTasks.ExecuteCoroutine(new HookExecutor("HotspotPoi", null, new HookExecutor("SetCombatPoi")));
        }

        protected override async Task Main()
        {
            await CommonTasks.HandleLoading();

            await GoThere();

            if (FateActive)
            {
                if (NeoProfileManager.CurrentGrindArea == null)
                {
                    if (ThisFate.Within2D(Me.Location))
                    {
                        await CreateGrindArea();

                        NeoProfileManager.CurrentGrindArea = FateGrindArea;

                        // Debug
                        foreach (var mob in FateGrindArea.TargetMobs)
                            Log("Added NpcId {0} with Weight {1} to the GrindArea.", mob.Id, mob.Weight);
                    }
                    else
                        await MoveAndStop(ThisFate.Location, ThisFate.Radius / 5f, "Moving to " + ThisFate.Name);
                }
                else
                {
                    if (ThisFate.Within2D(Me.Location) && !Me.IsLevelSynced && ThisFate.MaxLevel < Me.ClassLevel)
                    {
                        RemoteWindows.ToDoList.LevelSync();
                        await Coroutine.Wait(1000, () => Me.IsLevelSynced);
                    }

                    if (NeoProfileManager.CurrentGrindArea == FateGrindArea)
                        await HookExecutor();
                }
            }
            else
                await MoveAndStop(Destination, Distance * Distance, "Moving to Hotspot");
        }

        protected override void OnTagDone() { NeoProfileManager.CurrentGrindArea = null; }
    }
}
