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

        private void CreateGrindArea()
        {
            if (Hotspots.Count == 0)
            {
                Hotspots.Add(new HotSpot(XYZ, Radius));
                Hotspots.IsCyclic = true;
                Hotspots.Index = 0;
            }

            var grindArea = new GrindArea()
            {
                TargetMobs = NpcIds.Select(r => new TargetMob() { Id = r }).ToList(),
                Hotspots = Hotspots.ToList(),
                Name = QuestName
            };

            NeoProfileManager.CurrentGrindArea = grindArea;
            NeoProfileManager.UpdateGrindArea();
        }

        protected override void OnTagStart()
        {
            HotspotManager.Clear();
            CreateGrindArea();
            //Log($"Started for {QuestName}.");
        }

        protected override async Task<bool> Main()
        {
            await CommonTasks.HandleLoading();
            if (MapId > 0 && WorldManager.ZoneId != MapId && await CreateTeleportBehavior(0, (ushort)MapId)) return true;
            return await CommonTasks.ExecuteCoroutine(new HookExecutor("HotspotPoi"));
        }

        protected override void OnTagDone()
        {
            NeoProfileManager.CurrentGrindArea = null;
            KillCounter = 0;
        }
    }
}
