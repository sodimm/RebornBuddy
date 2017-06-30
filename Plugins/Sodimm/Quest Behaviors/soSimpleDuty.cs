using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using Quest_Behaviors;
using TreeSharp;
using Action = TreeSharp.Action;
using OrderBotTags.Behaviors;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoSimpleDuty")]

    public class SoSimpleDutyTag : SoProfileBehavior
    {

        public bool initialized = false;

        private ITargetingProvider CachedProvider;

        private Vector3 _position;
        //Stuff we are intrested in....
        [XmlAttribute("XYZ")]
        public Vector3 ObjectXYZ
        {
            get { return _position; }
            set { _position = value; }
        }

        [DefaultValue(3)]
        [XmlAttribute("InteractDistance")]
        public int InteractDistance { get; set; }

        [XmlElement("HotSpots")]
        public IndexedList<HotSpot> Hotspots { get; set; }

        [XmlElement("TargetMobs")]
        public List<TargetMob> TargetMobsList { get; set; }

        [XmlAttribute("InteractNpcId")]
        public uint InteractNpcId { get; set; }

        [XmlAttribute("IgnoreNpcIds")]
        public int[] IgnoreNpcIds { get; set; }

        [XmlAttribute("GuardianNPCId")]
        public uint GuardianNPCId { get; set; }


        [DefaultValue(20.0f)]
        [XmlAttribute("LeashDistance")]
        public float LeashDistance { get; set; }


        [DefaultValue(30.0f)]
        [XmlAttribute("SearchDistance")]
        public float SearchDistance { get; set; }

        public static float StaticSearchDistance;
        public static uint StaticGuardianNPCId;
        public sealed override bool IsDone
        {
            get
            {
                if (IsQuestComplete)
                    return true;

                if (IsStepComplete)
                    return true;

                return false;
            }
        }

        private float LeashSquared;
        private bool NeedToMove;
        private GameObject NPC
        {
            get { return GameObjectManager.GetObjectByNPCId(InteractNpcId); }
        }

        private BattleCharacter GuardianNPC
        {
            get
            {
                var unit = GameObjectManager.GetObjectByNPCId<BattleCharacter>(GuardianNPCId);
                return unit;
            }
        }

        private uint[] Potions = new[] {4554u, 4553u, 4552u, 4551u};
        private async Task<bool> DoControlLogic()
        {
            if (!initialized)
            {
                init();
            }
                


            var npc = GuardianNPC;


            if (npc != null && NeedToMove == false)
            {

                if (npc.Location.DistanceSqr(Core.Player.Location) > LeashSquared)
                {
                    if (Core.Player.CurrentHealthPercent < 50)
                        NeedToMove = true;

                    if (!Core.Player.InCombat)
                        NeedToMove = true;
                }


            }


            if (Core.Player.CurrentHealthPercent <= 30)
            {
                foreach (var potion in Potions)
                {
                    if (ActionManager.ItemUseable(potion, null))
                    {
                        Logging.Write("[SimpleDuty] Player below 30% heath using potion id {0}",potion);
                        ActionManager.DoAction(ActionType.Item, potion, null);
                        break;
                    }
                }
            }

            while (NeedToMove)
            {

                if (QuestLogManager.InCutscene)
                    continue;

                if (NeedToMove && (npc == null || !npc.IsValid || npc.Location.Distance2D(Core.Player.Location) <= 6))
                {
                    NeedToMove = false;
                }
                else
                {
                    await movementComposite.ExecuteCoroutine(npc);
                }
            }

            return false;
        }

        private Composite movementComposite = CommonBehaviors.MoveAndStop(r => (r as BattleCharacter).Location, 5f, true, "Moving back into guardian npc range");
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Decorator(ret => Talk.DialogOpen, new Action(ret => Talk.Next())),
                new Decorator(ret => SelectYesno.IsOpen, new Action(ret => SelectYesno.ClickYes())),
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => DutyManager.InInstance && Core.Player.IsVisible,
                    new PrioritySelector(
                //r=> GuardianNPC,
                //new Decorator(ret => !initialized,new Action(ret => init())),
                //
                //new Decorator(r => NeedToMove && ((r as BattleCharacter) == null || !(r as BattleCharacter).IsValid || (r as BattleCharacter).Location.Distance2D(Core.Player.Location) <= 5), new Action(r => NeedToMove = false)),
                //new Decorator(r => NeedToMove, CommonBehaviors.MoveAndStop(r => (r as BattleCharacter).Location, 5f, true, "Moving back into guardian npc range")),
                //new Decorator(r => (r as BattleCharacter) != null && ((r as BattleCharacter).Location.DistanceSqr(Core.Player.Location) > LeashSquared), new ActionAlwaysFail()),

                        new ActionRunCoroutine(r=> DoControlLogic()),
                        new Decorator(r=>CombatTargeting.Instance.FirstUnit == null,new HookExecutor("HotspotPoi")),
                        new HookExecutor("SetCombatPoi"),
                        new ActionAlwaysSucceed()
                    )
                ),
                new Decorator(r=> ChocoboManager.Summoned,new ActionRunCoroutine(r => ChocoboManager.DismissChocobo())),
                CommonBehaviors.MoveAndStop(r => XYZ, InteractDistance, true),
                CreateUseObject(),
                new ActionAlwaysSucceed()
            );
        }

        private Composite CreateUseObject()
        {
            return
                new Sequence(
                    r => NPC,
                    new Action(ret => Navigator.PlayerMover.MoveStop()),
                    new WaitContinue(5, ret => !MovementManager.IsMoving, new Action(ret => RunStatus.Success)),
                    new Action(ret => (ret as GameObject).Interact()),
                    new Wait(5, ret => Core.Me.IsCasting || ShortCircut((ret as GameObject)), new Action(ret => RunStatus.Success)),
                    new DecoratorContinue(r => ShortCircut((r as GameObject)), new ActionAlwaysFail()),
                    new DecoratorContinue(r => !Core.Player.IsCasting, new FailLogger(r => "We are not interacting for some reason!")),
                    new WaitContinue(15, ret => !Core.Me.IsCasting, new Action(ret => RunStatus.Success)),
                    new Sleep(2000)
                );
        }
        protected bool ShortCircut(GameObject obj)
        {
            if (!obj.IsValid || !obj.IsTargetable || !obj.IsVisible)
                return true;

            if (Core.Player.InCombat && !InCombat)
                return true;

            if (Talk.DialogOpen)
                return true;

            if (SelectYesno.IsOpen)
                return true;

            return false;
        }

        private void init()
        {
            NeoProfileManager.CurrentGrindArea = new GrindArea()
            {
                Hotspots =  Hotspots,
                TargetMobs = TargetMobsList
            };
            initialized = true;
        }
        /// <summary>
        /// This gets called when a while loop starts over so reset anything that is used inside the IsDone check
        /// </summary>
        protected override void OnResetCachedDone()
        {
            initialized = false;
        }


        public override bool HighPriority
        {
            get { return true; }
        }
        protected override void OnTagStart()
        {
            ChocoboManager.BlockSummon = true;
            LeashSquared = LeashDistance * LeashDistance;
            StaticGuardianNPCId = GuardianNPCId;
            StaticSearchDistance = SearchDistance;
            CachedProvider = CombatTargeting.Instance.Provider;
            if (IgnoreNpcIds == null)
                IgnoreNpcIds = new int[0];

            CombatTargeting.Instance.Provider = new SoSmallDutyCombatTargetingProvider(IgnoreNpcIds);
            CombatTargeting.Instance.Locked = true;
            GameEvents.OnPlayerDied += GameEvents_OnPlayerDied;
        }

        void GameEvents_OnPlayerDied(object sender, EventArgs e)
        {
            initialized = false;
            NeoProfileManager.CurrentGrindArea = null;
        }

        protected override void OnTagDone()
        {
            ChocoboManager.BlockSummon = false;
            GameEvents.OnPlayerDied -= GameEvents_OnPlayerDied;
            CombatTargeting.Instance.Locked = false;
            CombatTargeting.Instance.Provider = CachedProvider;
            NeoProfileManager.CurrentGrindArea = null;
        }

        protected override Task<bool> Main()
        {
            throw new NotImplementedException();
        }
    }
}