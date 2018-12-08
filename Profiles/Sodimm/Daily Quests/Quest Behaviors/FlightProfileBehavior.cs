namespace OrderBotTags.Behaviors
{
    using Buddy.Coroutines;
    using Clio.Utilities;
    using Clio.XmlEngine;
    using ff14bot;
    using ff14bot.Behavior;
    using ff14bot.Enums;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.NeoProfiles;
    using ff14bot.Objects;
    using ff14bot.Pathing;
    using ff14bot.RemoteAgents;
    using ff14bot.RemoteWindows;
    using ff14bot.Settings;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using TreeSharp;

    public abstract class FlightEnabledProfileBehavior : ProfileBehavior
    {
        static FlightEnabledProfileBehavior() { }
        protected FlightEnabledProfileBehavior() { }

        [XmlAttribute("Condition")]
        public string Condition { get; set; }
        internal Func<bool> Conditional { get; set; }

        [DefaultValue(-1)]
        [XmlAttribute("Todo")]
        public int Todo { get; set; }

        /// <summary>
        /// Checks IsTodoChecked if Todo attribute is defined.
        /// </summary>
        public bool IsObjectiveComplete => Todo > -1 ? ConditionParser.IsTodoChecked(QuestId, (int)StepId, Todo) : false;

        [DefaultValue(-1)]
        [XmlAttribute("TodoCount")]
        public int TodoCount { get; set; }

        [DefaultValue(-1)]
        [XmlAttribute("TodoIndex")]
        public int TodoIndex { get; set; }

        /// <summary>
        /// Checks GetTodoArgs.Item1 (Count) if TodoCount and TodoIndex attributes are defined.
        /// </summary>
        public bool IsTodoCountComplete => TodoCount > -1 && TodoIndex > -1 ? ConditionParser.GetQuestById(QuestId).GetTodoArgs((int)StepId, TodoIndex).Item1 != TodoCount : false;

        [DefaultValue(false)]
        [XmlAttribute("Land")]
        public bool Land { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("Dismount")]
        public bool Dismount { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("BlacklistAfter")]
        public bool BlacklistAfter { get; set; }

        [DefaultValue(180)]
        [XmlAttribute("BlacklistDuration")]
        public int BlacklistDuration { get; set; }

        [XmlAttribute("Wait")]
        [DefaultValue(1000)]
        public int Wait { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("IgnoreIndoors")]
        public bool IgnoreIndoors { get; set; }

        [DefaultValue(0f)]
        [XmlAttribute("MinHeight")]
        public float MinHeight { get; set; }

        internal void SetupConditional()
        {
            try
            {
                if (Conditional == null && !string.IsNullOrEmpty(Condition))
                    Conditional = ScriptManager.GetCondition(Condition);
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic(ScriptManager.FormatSyntaxErrorException(ex));
                TreeRoot.Stop();
                throw;
            }
        }

        protected override Composite CreateBehavior() => base.CreateBehavior();

        public static class Helpers
        {
            /// <summary>
            /// Checks to see if the Object has any AuraId matching those in an Array of Ids.
            /// </summary>
            /// <param name="obj">The GameObject.</param>
            /// <param name="ids">The AuraIds to Check for.</param>
            /// <returns>True if the AuraId exists.</returns>
            public static bool HasAnyAura(BattleCharacter obj, params int[] ids)
            {
                int idCount = 0;

                foreach (uint id in ids)
                {
                    if (obj.HasAura(id)) { idCount++; }
                }

                return idCount > 0 ? true : false;
            }
        }

        #region Movement
        public class Movement
        {
            /// <summary>
            /// Checks to see if we're at the required destination.
            /// </summary>
            /// <param name="destination">The Destination</param>
            public static bool AtLocation(Vector3 destination)
            {
                float yTolerance;
                if (Core.Me.Location.Distance2DSqr(destination) > 3.0f * 5.0f) return false;

                if (MovementManager.IsDiving) yTolerance = Math.Max(0.9f, 1.9f); // Has to be close
                else yTolerance = Math.Max(2.9f, 4.9f);

                return Math.Abs(destination.Y - Core.Me.Location.Y) < yTolerance;
            }

            /// <summary>
            /// Movement Coroutine using Flightor.
            /// </summary>
            /// <param name="destination">The Destination</param>
            /// <param name="land">Do you want to land?</param>
            /// <param name="dismount">Do you want to dismount?</param>
            /// <param name="ignoreIndoors">Ignore Indoor Annotations?</param>
            /// <param name="minHeight">Sets Minimum height.</param>
            public static async Task<bool> MoveTo(Vector3 destination, bool land = false, bool dismount = false, bool ignoreIndoors = false, float minHeight = 0f)
            {
                if (CommonBehaviors.IsLoading) { await CommonTasks.HandleLoading(); }

                if (!await Common.AwaitCombat(500)) { return false; }

                while (!AtLocation(destination))
                {
                    if (Core.Me.IsDead) { return false; }

                    if (!Core.Me.IsMounted && Core.Me.Location.Distance(destination) > CharacterSettings.Instance.MountDistance)
                    {
                        await CommonTasks.SummonFlyingMount();
                        continue;
                    }

                    var parameters = new FlyToParameters(destination) { CheckIndoors = !ignoreIndoors };

                    if (MovementManager.IsDiving) parameters.CheckIndoors = false;

                    if (minHeight > 0) parameters.MinHeight = minHeight;

                    Flightor.MoveTo(parameters);
                    await Coroutine.Yield();
                }

                while (AtLocation(destination))
                {
                    MovementManager.MoveStop();

                    if (!MovementManager.IsDiving && MovementManager.IsFlying && land)
                    {
                        await Common.Land();
                        continue;
                    }

                    if (Core.Me.IsMounted && dismount)
                    {
                        await Common.Dismount();
                        continue;
                    }

                    Flightor.Clear();
                    break;
                }

                return true;
            }
        }
        #endregion Movement

        #region Common Tasks
        public class Common
        {
            /// <summary>
            /// Causes the Coroutine to Yield.
            /// </summary>
            /// <param name="milliseconds"></param>
            public static async Task<bool> Sleep(int milliseconds)
            {
                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < milliseconds) { await Coroutine.Yield(); }

                return true;
            }

            /// <summary>
            /// Dismounts the Player.
            /// </summary>
            public static async Task<bool> Dismount()
            {
                if (!Core.Me.IsMounted) { return false; }

                while (Core.Me.IsMounted)
                {
                    await CommonTasks.StopAndDismount();
                    await Sleep(100);

                    if (!Core.Me.IsMounted) { break; }

                }

                return true;
            }

            /// <summary>
            /// Lands the Player if Flying.
            /// </summary>
            public static async Task<bool> Land()
            {
                if (!MovementManager.IsFlying) { return false; }

                while (MovementManager.IsFlying)
                {
                    await CommonTasks.Land();
                    await Sleep(100);

                    if (!MovementManager.IsFlying) { break; }

                }

                return true;
            }

            /// <summary>
            /// Causes the Coroutine to wait unless in combat.
            /// </summary>
            /// <param name="milliseconds"></param>
            public static async Task<bool> AwaitCombat(int milliseconds = 1000)
            {
                await Coroutine.Wait(milliseconds, () => Core.Me.InCombat);

                if (Core.Me.InCombat) { return false; }

                return true;
            }

            /// <summary>
            /// Returns the closest object to the player matching the given NpcId.
            /// </summary>
            /// <param name="npcid">The NpcId.</param>
            public static GameObject GetClosest(int npcid) => GameObjectManager.GameObjects.Where(
                    o => o.NpcId == npcid
                    && o.IsVisible
                    && o.IsTargetable
                    && !Blacklist.Contains(o.ObjectId))
                    .OrderBy(o => o.Location.Distance3D(Core.Me.Location))
                    .FirstOrDefault();

            /// <summary>
            /// Returns the closest object to the player matching any NpcId in the array.
            /// </summary>
            /// <param name="npcids">The NpcId array.</param>
            public static GameObject GetClosest(int[] npcids) => GameObjectManager.GameObjects.Where(
                    o => npcids.Contains((int)o.NpcId)
                    && o.IsVisible
                    && o.IsTargetable
                    && !Blacklist.Contains(o.ObjectId))
                    .OrderBy(o => o.Location.Distance3D(Core.Me.Location))
                    .FirstOrDefault();

            /// <summary>
            /// Checks if an object with the given NpcId exists at the given location.
            /// </summary>
            /// <param name="npcId">The NpcId.</param>
            public static bool Exists(Vector3 location, int npcId, float range = 20f) => GameObjectManager.GameObjects.Where(
                    o => o.NpcId == npcId
                    && o.IsVisible
                    && o.IsTargetable
                    && !Blacklist.Contains(o.ObjectId))
                    .Any(o => o.Location.Distance3D(location) <= range);

            /// <summary>
            /// Checks if an object within an array of NpcIds exists at the given location.
            /// </summary>
            /// <param name="npcIds">The NpcId array.</param>
            public static bool Exists(Vector3 location, int[] npcIds, float range = 20f) => GameObjectManager.GameObjects.Where(
                    o => npcIds.Contains((int)o.NpcId)
                    && o.IsVisible
                    && o.IsTargetable
                    && !Blacklist.Contains(o.ObjectId))
                    .Any(o => o.Location.Distance3D(location) <= range);

            /// <summary>
            /// Handles Using an Emote on a GameObject.
            /// </summary>
            /// <param name="emote">The Emote to use.</param>
            /// <param name="obj">The GameObject.</param>
            /// <param name="blackList">Should you Blacklist the object after use?</param>
            /// <param name="duration">How long do you want the Blacklist to last.</param>
            /// <returns></returns>
            public static async Task<bool> UseEmote(string emote, GameObject obj, bool blackList = false, int duration = 180)
            {
                while (true)
                {
                    if (Dialog.IsTalking)
                    {
                        await Dialog.Skip();
                        await Sleep(500);
                        continue;
                    }

                    if (Core.Me.InCombat) { return false; }

                    if (!Exists(obj.Location, (int)obj.NpcId) || !obj.IsTargetable || !obj.IsValid) { break; }

                    if (Core.Me.CurrentTarget != obj)
                    {
                        obj.Target();
                        await Sleep(300);
                        continue;
                    }

                    ChatManager.SendChat("/" + emote);
                    await Sleep(800);
                    await Coroutine.Wait(10000, () => !Core.Me.IsCasting);
                    if (!Core.Me.InCombat) { await Sleep(800); }

                    if (blackList)
                    {
                        Blacklist.Add(obj.ObjectId, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(duration), "BlacklistAfter");
                    }

                }

                return true;
            }

            /// <summary>
            /// Handles Using a Spell on a GameObject.
            /// </summary>
            /// <param name="spellId">The Id of the Spell to use.</param>
            /// <param name="obj">The GameObject.</param>
            /// <param name="blackList">Should you Blacklist the object after use?</param>
            /// <param name="duration">How long do you want the Blacklist to last.</param>
            /// <returns></returns>
            public static async Task<bool> UseSpell(int spellId, GameObject obj, bool blackList = false, int duration = 180)
            {
                while (true)
                {
                    if (Dialog.IsTalking)
                    {
                        await Dialog.Skip();
                        await Sleep(500);
                        continue;
                    }

                    if (!Exists(obj.Location, (int)obj.NpcId) || !obj.IsTargetable || !obj.IsValid) { break; }

                    if (Core.Me.CurrentTarget != obj)
                    {
                        obj.Target();
                        await Sleep(300);
                        continue;
                    }

                    if (!ActionManager.DoAction((uint)spellId, obj) && ActionManager.CanCast((uint)spellId, obj))
                    {
                        ActionManager.DoAction((uint)spellId, obj);
                    }

                    else if (!ActionManager.DoActionLocation((uint)spellId, obj.Location) && ActionManager.CanCastLocation((uint)spellId, obj.Location))
                    {
                        ActionManager.DoActionLocation((uint)spellId, obj.Location);
                    }

                    await Sleep(800);
                    await Coroutine.Wait(10000, () => !Core.Me.IsCasting);
                    if (!Core.Me.InCombat) { await Sleep(800); }

                    if (blackList)
                    {
                        Blacklist.Add(obj.ObjectId, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(duration), "BlacklistAfter");
                    }

                }

                return true;
            }

            /// <summary>
            /// Handles Using a GameObject.
            /// </summary>
            /// <param name="obj">The GameObject.</param>
            /// <param name="blackList">Should you Blacklist the object after use?</param>
            /// <param name="duration">How long do you want the Blacklist to last.</param>
            /// <returns></returns>
            public static async Task<bool> UseObject(GameObject obj, bool blackList = false, int duration = 180)
            {
                while (true)
                {
                    if (Dialog.IsTalking)
                    {
                        await Dialog.Skip();
                        await Sleep(500);
                        continue;
                    }

                    if (Core.Me.InCombat) { return false; }

                    if (!Exists(obj.Location, (int)obj.NpcId) || !obj.IsTargetable || !obj.IsValid) { break; }

                    if (Core.Me.CurrentTarget != obj)
                    {
                        obj.Target();
                        await Sleep(300);
                        continue;
                    }

                    obj.Interact();
                    await Sleep(800);
                    await Coroutine.Wait(10000, () => !Core.Me.IsCasting);
                    if (!Core.Me.InCombat) { await Sleep(800); }

                    if (blackList)
                    {
                        Blacklist.Add(obj.ObjectId, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(duration), "BlacklistAfter");
                    }
                }

                return true;
            }

            /// <summary>
            /// Handles Using an Item on a GameObject.
            /// </summary>
            /// <param name="item">The Id of the Item you want to use.</param>
            /// <param name="obj">The GameObject.</param>
            /// <param name="blackList">Should you Blacklist the object after use?</param>
            /// <param name="duration">How long do you want the Blacklist to last.</param>
            /// <returns></returns>
            public static async Task<bool> UseItem(int item, GameObject obj, bool blackList = false, int duration = 180, bool inCombat = false, int healthPercent = 40, int[] hasAnyAura = null)
            {
                var slot = InventoryManager.FilledSlots.FirstOrDefault(s => s.RawItemId == item);
                if (slot == null) { return false; }

                var count = slot.Count;

                while (true)
                {
                    if (Dialog.IsTalking)
                    {
                        await Dialog.Skip();
                        await Sleep(500);
                        continue;
                    }

                    if (inCombat && hasAnyAura != null && Helpers.HasAnyAura((obj as BattleCharacter), hasAnyAura)) { return false; }

                    if (inCombat && Core.Me.CurrentTarget.CurrentHealthPercent > healthPercent) { return false; }

                    if (!inCombat && Core.Me.InCombat) { return false; }

                    if (!Exists(obj.Location, (int)obj.NpcId) || !obj.IsTargetable || !obj.IsValid || !slot.CanUse(obj)) { break; }
                    if (slot.Count < count) { break; }

                    if (Core.Me.CurrentTarget != obj)
                    {
                        obj.Target();
                        await Sleep(300);
                        continue;
                    }

                    if (slot.Item.IsGroundTargeting) { slot.UseItem(Core.Me.CurrentTarget.Location); }
                    else { slot.UseItem(Core.Me.CurrentTarget); }
                    await Sleep(800);
                    await Coroutine.Wait(10000, () => !Core.Me.IsCasting);
                    if (!Core.Me.InCombat) { await Sleep(800); }

                    if (blackList)
                    {
                        Blacklist.Add(obj.ObjectId, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(duration), "BlacklistAfter");
                    }

                }

                return true;
            }

        }
        #endregion Common Tasks

        #region Dialog
        public class Dialog
        {
            /// <summary>
            /// Determines if the player is talking.
            /// </summary>
            public static bool IsTalking =>
                QuestLogManager.InCutscene
                || Talk.DialogOpen
                || Talk.ConvoLock
                || SelectString.IsOpen
                || SelectYesno.IsOpen
                || SelectYesNoItem.IsOpen
                || SelectIconString.IsOpen
                || MovementManager.IsOccupied // Quest emotes etc lock movement.
                || CommonBehaviors.IsLoading;

            /// <summary>
            /// Targets and Interacts with the given object with npcId specified. Handles all windows.
            /// </summary>
            /// <param name="npcId">The Id of the NPC you wish to talk to.</param>
            /// <param name="questId">Used for acquisition of QuestRewards and SelectIconString data.</param>
            /// <param name="selectString">Used to override the slot selected once SelectString windows are available.</param>
            public static async Task<bool> Interact(int npcId, int questId = 0, int selectString = 0)
            {
                // Target the Npc.
                while (Core.Player.CurrentTarget?.NpcId != npcId)
                {
                    if (!Common.Exists(Core.Me.Location, npcId)) { return false; }

                    GameObjectManager.GetObjectByNPCId((uint)npcId)?.Target();
                    await Coroutine.Yield();
                }

                // Interact with the Npc.
                while (Core.Me.HasTarget && !IsTalking)
                {
                    if (!Core.Me.HasTarget) { return false; }

                    Core.Player.CurrentTarget.Interact();

                    await Common.Sleep(50);

                    if (Core.Player.IsCasting) { await Coroutine.Wait(10000, () => !Core.Player.IsCasting); }
                }

                // Handle all dialog.
                while (Core.Me.HasTarget)
                {
                    await Common.Sleep(50);

                    if (await SkipCutscene()) { continue; }
                    if (await HandleTalkDialogOpen()) { continue; }
                    if (await HandleJournalAccept()) { continue; }
                    if (await HandleSelectString(selectString)) { continue; }
                    if (await HandleIconString(questId)) { continue; }
                    if (await HandleSelectYesNo()) { continue; }
                    if (await HandleRequestHandOver()) { continue; }
                    if (await HandleRewards(questId)) { continue; }
                    if (await HandleCompleteQuest()) { continue; }
                }

                return true;
            }

            /// <summary>
            /// Skips Cutscenes.
            /// </summary>
            public static async Task<bool> SkipCutscene()
            {
                if (!QuestLogManager.InCutscene) { return false; }

                if (JournalResult.IsOpen) { return false; }

                if (JournalAccept.IsOpen) { return false; }

                if (Talk.DialogOpen) { return false; }

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

                return true;
            }

            /// <summary>
            /// Skips generic interactions.
            /// </summary>
            public static async Task<bool> Skip()
            {
                if (!IsTalking) { return false; }

                while (IsTalking)
                {
                    await Common.Sleep(50);

                    if (await SkipCutscene()) { continue; }
                    if (await HandleTalkDialogOpen()) { continue; }
                    if (await HandleSelectYesNo()) { continue; }
                    if (await HandleRequestHandOver()) { continue; }

                    if (!IsTalking) { break; }
                }

                //await Coroutine.Wait(10000, () => !Talk.ConvoLock);

                return true;
            }

            /// <summary>
            /// Skips Talk dialogs if Open.
            /// </summary>
            private static async Task<bool> HandleTalkDialogOpen()
            {
                if (!Talk.DialogOpen) { return false; }

                while (Talk.DialogOpen)
                {
                    Talk.Next();
                    await Common.Sleep(50);

                    if (!Talk.DialogOpen) { break; }
                }

                return true;
            }

            /// <summary>
            /// Clicks Complete Quest once available.
            /// </summary>
            private static async Task<bool> HandleCompleteQuest()
            {
                if (!JournalResult.IsOpen || !JournalResult.ButtonClickable) { return false; }

                while (JournalResult.ButtonClickable)
                {
                    JournalResult.Complete();
                    await Common.Sleep(100);

                    if (!JournalResult.IsOpen || !JournalResult.ButtonClickable) { break; }
                }

                return true;
            }

            /// <summary>
            /// Clicks Accept Quest once available.
            /// </summary>
            private static async Task<bool> HandleJournalAccept()
            {
                if (!JournalAccept.IsOpen) { return false; }

                while (JournalAccept.IsOpen)
                {
                    JournalAccept.Accept();
                    await Common.Sleep(50);

                    if (!JournalAccept.IsOpen) { break; }
                }

                return true;
            }

            /// <summary>
            /// Hands Over any requested items. Doesn't need Ids as the only items available are those for the NPC.
            /// </summary>
            private static async Task<bool> HandleRequestHandOver()
            {
                if (!Request.IsOpen) { return false; }

                foreach (var slot in InventoryManager.GetBagByInventoryBagId(InventoryBagId.KeyItems).FilledSlots)
                {
                    slot.Handover();
                    await Common.Sleep(10);

                    if (Request.HandOverButtonClickable) { break; }
                }

                while (Request.HandOverButtonClickable)
                {
                    Request.HandOver();
                    await Common.Sleep(50);

                    if (!Request.IsOpen) { break; }
                }

                return true;
            }

            /// <summary>
            /// Clicks the desired slot for SelectString inputs.
            /// </summary>
            /// <param name="slot">The Line Number, like any index, this starts at 0.</param>
            private static async Task<bool> HandleSelectString(int slot)
            {
                if (!SelectString.IsOpen) { return false; }

                while (SelectString.IsOpen)
                {
                    SelectString.ClickSlot((uint)slot);
                    await Common.Sleep(50);

                    if (!SelectString.IsOpen) { break; }
                }

                return true;
            }

            /// <summary>
            /// Clicks the SelectIconString of the corresponding Quest.
            /// </summary>
            /// <param name="questId"></param>
            private static async Task<bool> HandleIconString(int questId)
            {
                var name = DataManager.GetLocalizedQuestName(questId);

                if (!SelectIconString.IsOpen) { return false; }

                while (SelectIconString.IsOpen)
                {
                    if (questId > 0)
                    {
                        SelectIconString.ClickLineEquals(name);
                        await Common.Sleep(50);
                    }
                    else
                    {
                        SelectIconString.ClickSlot(0);
                        await Common.Sleep(50);
                    }

                    if (!SelectIconString.IsOpen) { break; }
                }

                return true;
            }

            /// <summary>
            /// Clicks Yes :: Most Quests only use this as a gimmick. All of them use Yes thus far.
            /// </summary>
            private static async Task<bool> HandleSelectYesNo()
            {
                if (!SelectYesno.IsOpen) { return false; }

                while (SelectYesno.IsOpen)
                {
                    SelectYesno.ClickYes();
                    await Common.Sleep(50);

                    if (!SelectYesno.IsOpen) { break; }
                }

                return true;
            }

            private struct Score
            {
                public Score(Reward reward)
                {
                    Reward = reward;
                    Value = ItemWeightsManager.GetItemWeight(reward);
                }

                public Reward Reward;
                public float Value;
            }
            /// <summary>
            /// Chooses equipment if better than currently equipped (using Item Weights) or items based on higest value.
            /// </summary>
            /// <param name="questId"></param>
            private static async Task<bool> HandleRewards(int questId)
            {
                if (!JournalResult.IsOpen) { return false; }

                if (!DataManager.QuestCache.TryGetValue((uint)questId, out QuestResult questData)) { return false; }

                if (questData.Rewards.Length == 0) { return false; }

                var values = questData.Rewards.Select(r => new Score(r)).OrderByDescending(r => r.Value).ToArray();

                int chosenSlot;
                if (values.Select(r => r.Value).Distinct().Count() == 1)
                {
                    chosenSlot = questData.Rewards.IndexOf(values[0].Reward);
                }
                else
                {
                    var candidateByCost = questData.Rewards
                        .OrderByDescending(r => r.Worth)
                        .FirstOrDefault();

                    chosenSlot = Array.IndexOf(questData.Rewards, candidateByCost);
                }

                JournalResult.SelectSlot(5 + chosenSlot);
                await Common.Sleep(50);

                return true;
            }
        }
        #endregion Dialog
    }
}
