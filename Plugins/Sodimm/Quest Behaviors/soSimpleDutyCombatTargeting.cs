using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using ff14bot.NeoProfiles.Tags;
using ff14bot.Objects;

namespace Quest_Behaviors
{
    public class SoSmallDutyCombatTargetingProvider : ITargetingProvider
    {
        public readonly HashSet<int> IgnoredIds;
        public SoSmallDutyCombatTargetingProvider(int[] idstoignore)
        {
            IgnoredIds = new HashSet<int>(idstoignore);
        }

        private BattleCharacter[] _aggroedBattleCharacters;
        /// <summary> Gets the objects by weight. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        /// <returns> The objects by weight. </returns>
        public List<BattleCharacter> GetObjectsByWeight()
        {
            BattleCharacter[] allUnits = GameObjectManager.GetObjectsOfType<BattleCharacter>().ToArray();

            _aggroedBattleCharacters = GameObjectManager.Attackers.ToArray();

            var ga = NeoProfileManager.CurrentGrindArea;
            HotSpot[] hotSpots = null;
            if (ga != null && ga.Hotspots != null)
            {
                hotSpots = ga.Hotspots.ToArray();
            }


            bool InCombat = Core.Player.InCombat;

            BattleCharacter guardianNpc = GameObjectManager.GetObjectByNPCId<BattleCharacter>(SoSimpleDutyTag.StaticGuardianNPCId);


            List<Score> hostileUnits = allUnits.Where((u, i) => IsValidUnit(InCombat, u, guardianNpc, hotSpots)).
                Select(
                n => new Score
                {
                    Unit = n,
                    Weight = (n.Distance() * -100d) + 5000d
                }).ToList();

            foreach (Score s in hostileUnits)
                s.Weight = GetScoreForUnit(s);


            /*var nav = (Navigator.NavigationProvider as GaiaNavigator);
            var navReq = hostileUnits.Select(r => new CanFullyNavigateTarget() {Id = r.Unit.ObjectId, Position = r.Unit.Location});
            if (navReq.Any())
            {
                var results = nav.CanFullyNavigateTo(navReq).Where(r=>r.CanNavigate == 1).Select(r=>r.Id);
                hostileUnits.RemoveAll(r => !results.Contains(r.Unit.ObjectId));

            }*/

            // Order by weight (descending). Then grab the NWCritter version of it. :D
            return new List<BattleCharacter>(hostileUnits.OrderByDescending(s => s.Weight).Select(s => s.Unit).ToList());
        }

        /// <summary> Query if 'unit' is valid unit. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        private bool IsValidUnit(bool incombat, BattleCharacter unit, BattleCharacter guardian, HotSpot[] hotspots)
        {
            if (!unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable || unit.CurrentHealthPercent <= 0)
                return false;
            if (IgnoredIds.Contains((int)unit.NpcId))
                return false;
            // Ignore blacklisted mobs if they're in combat with us!
            if (Blacklist.Contains(unit.ObjectId, BlacklistFlags.Combat))
                return false;
            //Make sure we always return true for units inside our aggro list
            if (_aggroedBattleCharacters.Contains(unit))
                return true;

            if (!unit.CanAttack)
                return false;

            if (guardian != null)
            {
                if (guardian.DistanceSqr(unit.Location) <= SoSimpleDutyTag.StaticSearchDistance * SoSimpleDutyTag.StaticSearchDistance)
                {
                    return true;
                }
            }

            if (hotspots != null)
            {
                if (hotspots.Any(b => b.WithinHotSpot2D(unit.Location, b.Radius)))
                    return true;
            }

            return false;
        }

        /// <summary> Gets score for a unit. </summary>
        /// <remarks> Nesox, 2013-06-29. </remarks>
        /// <param name="unit"> The unit. </param>
        /// <returns> The score for unit. </returns>
        private double GetScoreForUnit(Score score)
        {
            double weight = score.Weight;
            var unit = score.Unit;

            if (unit.Pointer == Core.Player.PrimaryTargetPtr)
            {
                // Little extra weight on current targets.
                weight += 25;
            }

            if (unit.CurrentTargetId == Core.Player.ObjectId)
            {
                weight += 25;
            }

            //weight -= (int)npc.Toughness * 50;

            // Force 100 weight on any in-combat NPCs.
            if (_aggroedBattleCharacters.Contains(unit))
                weight += 100;

            // Less weight on out of combat targets.
            if (!unit.InCombat)
                weight -= 100;


            if (NeoProfileManager.CurrentGrindArea != null && NeoProfileManager.CurrentGrindArea.TargetMobs != null)
            {
                var prio = NeoProfileManager.CurrentGrindArea.TargetMobs.FirstOrDefault(p => p.Id == score.Unit.NpcId);
                if (prio != null)
                {
                    weight *= prio.Weight;
                }
            }

            return weight;
        }

        private class Score
        {
            public BattleCharacter Unit;
            public double Weight;
        }
    }

}
