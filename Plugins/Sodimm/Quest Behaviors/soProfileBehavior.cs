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
    using ff14bot.RemoteWindows;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using TreeSharp;
    using ff14bot.RemoteAgents;
    using ff14bot.Settings;
    using System.Threading;

    public abstract class SoProfileBehavior : ProfileBehavior
    {
        static SoProfileBehavior() { }

        protected SoProfileBehavior() { }

        #region Attributes
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("RewardSlot")]
        [DefaultValue(-1)]
        public int RewardSlot { get; set; }

        [XmlAttribute("ItemIds")]
        public int[] ItemIds { get; set; }

        [XmlAttribute("RequiresHq")]
        public bool[] RequiresHq { get; set; }

        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }

        [XmlAttribute("DialogOption")]
        [DefaultValue(-1)]
        public int DialogOption { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ
        {
            get
            {
                return Destination;
            }
            set
            {
                Destination = value;
            }
        }
        public Vector3 Destination;

        [DefaultValue(3.24f)]
        [XmlAttribute("Distance")]
        public float Distance { get; set; }

        [XmlAttribute("Objective")]
        [DefaultValue(-1)]
        public int Objective { get; set; }

        [XmlAttribute("Count")]
        [DefaultValue(-1)]
        public int Count { get; set; }

        [DefaultValue(true)]
        [XmlAttribute("UseMesh")]
        public bool UseMesh { get; set; }

        [XmlAttribute("Condition")]
        public string Condition { get; set; }
        public Func<bool> Conditional { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("MapId")]
        public int MapId { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("NoFlight")]
        public bool NoFlight { get; set; }

        [DefaultValue(50f)]
        [XmlAttribute("MountDistance")]
        public float MountDistance { get; set; }
        #endregion

        #region IsDone Overriders
        internal bool _done;

        internal bool HasQuest { get { return QuestId > 0 ? ConditionParser.HasQuest(QuestId) : true; } }

        internal bool HasItems
        {
            get
            {
                if (ItemIds != null)
                {
                    var items = InventoryManager.FilledInventoryAndArmory.ToArray();

                    for (int i = 0; i < ItemIds.Length; i++)
                    {
                        BagSlot item;
                        item = items.FirstOrDefault(z => z.RawItemId == ItemIds[i]);

                        if (item != null) return true;
                    }

                    return false;
                }

                return true;
            }
        }

        internal bool IsOnTurnInStep { get { return ConditionParser.GetQuestStep(QuestId) == 255; } }

        internal bool IsObjectiveCountComplete { get { return Objective > -1 && Count > -1 ? ConditionParser.GetQuestById(QuestId).GetTodoArgs(Objective).Item1 >= Count : false; } }

        internal bool IsObjectiveComplete { get { return QuestId > 0 && StepId > 0 && Objective > -1 ? ConditionParser.IsTodoChecked(QuestId, (int)StepId, Objective) : false; } }

        internal bool IsQuestAcceptQualified { get { return ConditionParser.IsQuestAcceptQualified(QuestId); } }
        #endregion

        protected void FlightCheck()
        {
            if (NoFlight)
            {
                foreach (PluginContainer p in PluginManager.Plugins.Where(r => r.Plugin.Name == "Enable Flight" && r.Enabled))
                {
                    Log("Disabling {0}", p.Plugin.Name);
                    p.Enabled = false;
                }
            }
            else if (!NoFlight)
            {
                foreach (PluginContainer p in PluginManager.Plugins.Where(r => r.Plugin.Name == "Enable Flight" && !r.Enabled))
                {
                    Log("Enabling {0}", p.Plugin.Name);
                    p.Enabled = true;
                }
            }
        }

        #region Quest Data
        private QuestResult ThisQuest;

        internal string NpcName { get { return NpcId != 0 ? DataManager.GetLocalizedNPCName(NpcId) : null; } }

        internal string QuestGiver { get { return QuestId != 0 ? DataManager.GetLocalizedNPCName((int)ThisQuest.PickupNPCId) : null; } }

        internal bool hasRewards = false;

        private HashSet<BagSlot> usedSlots;

        struct Score
        {
            public Score(Reward reward)
            {
                Reward = reward;
                Value = ItemWeightsManager.GetItemWeight(reward);
            }

            public Reward Reward;
            public float Value;
        }

        protected void GetQuestData()
        {
            if (QuestId != 0)
            {
                if (QuestId > 65535)
                    DataManager.QuestCache.TryGetValue((uint)QuestId, out ThisQuest);
                else
                    DataManager.QuestCache.TryGetValue((ushort)QuestId, out ThisQuest);

                usedSlots = new HashSet<BagSlot>();
                if (RewardSlot == -1)
                {
                    if (ThisQuest != null && ThisQuest.Rewards.Any())
                    {
                        var values = ThisQuest.Rewards.Select(r => new Score(r)).OrderByDescending(r => r.Value).ToArray();

                        if (values.Select(r => r.Value).Distinct().Count() == 1)
                            values = values.OrderByDescending(r => r.Reward.Worth).ToArray();

                        RewardSlot = ThisQuest.Rewards.IndexOf(values[0].Reward) + 5;
                        hasRewards = true;
                    }
                }
                else
                {
                    RewardSlot = RewardSlot + 5;
                    hasRewards = true;
                }

                if (RequiresHq == null)
                {
                    if (ItemIds != null)
                        RequiresHq = new bool[ItemIds.Length];
                }
                else
                {
                    if (RequiresHq.Length != ItemIds.Length)
                        LogError("'RequiresHq' must have the same number of items as 'ItemIds'");
                }
            }
        }
        #endregion

        protected void SetupConditional()
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

        protected virtual void OnTagStart() { }

        protected virtual void OnTagDone() { }

        protected sealed override void OnStart()
        {
            FlightCheck();
            SetupConditional();
            GetQuestData();
            OnTagStart();
        }

        protected sealed override void OnDone()
        {
            OnTagDone();
        }

        #region -----Common Tasks-----

        /// <summary>
        /// Mounts the player.
        /// </summary>
        /// <returns>true if player is not mounted.</returns>
        public static async Task<bool> MountUp()
        {
            // errorcode 0 = we can mount.
            if (ActionManager.CanMount != 0)
                return false;

            // if we have the transparent buff, don't mount.
            if (Core.Player.HasAura(416))
                return false;

            // if we have the transporting buff, don't mount.
            if (Core.Player.HasAura(404))
                return false;

            if (!Core.Player.IsMounted)
            {
                // stop the player if we're moving.
                if (MovementManager.IsMoving)
                    Navigator.PlayerMover.MoveStop();

                ActionManager.Mount();
                // wait whilst we're casting.
                await Coroutine.Wait(5000, () => !Core.Player.IsCasting);
                // wait until we're mounted.
                await Coroutine.Wait(5000, () => Core.Player.IsMounted);
            }

            // return true if the player ISN'T mounted, forcing the routine to go back.
            // return false if the player IS mounted, allowing the routine to continue.
            return !Core.Player.IsMounted;
        }

        /// <summary>
        /// Dismounts the player.
        /// </summary>
        /// <returns>true if the player is mounted.</returns>
        public static async Task<bool> Dismount()
        {
            if (Core.Player.IsMounted)
            {
                // wait to land
                if (await CommonTasks.Land())
                    // should only take a second or so, but sometimes the landing is off and it misses the ground, 3s before returning.
                    await Coroutine.Wait(3000, () => !MovementManager.IsFlying);

                // stop the player if we're moving.
                if (MovementManager.IsMoving)
                    Navigator.PlayerMover.MoveStop();

                ActionManager.Dismount();
                // wait until dismounted.
                await Coroutine.Wait(5000, () => !Core.Player.IsMounted);
            }

            // return true if the player IS mounted, forcing the routine to go back.
            // return false if the player ISN'T mounted, allowing the routine to continue.
            return Core.Player.IsMounted;
        }

        /// <summary>
        /// Teleports the player to the first available location on their teleport list.
        /// Location will be derived via either AetheryteId OR ZoneId.
        /// </summary>
        /// <param name="aetheryteid">If stipulated, will teleport to the zone associated to the aetheryte</param>
        /// <param name="zoneid">If stipulated, will teleport to the first available aetheryte associated with the zone.</param>
        /// <returns>true if not on requested zone.</returns>
        public static async Task<bool> CreateTeleportBehavior(uint aetheryteid = 0, uint zoneid = 0)
        {
            // cache the available locations.
            IEnumerable<uint> id = WorldManager.AvailableLocations.Where(x => x.ZoneId == zoneid).Select(r => r.AetheryteId);

            if (WorldManager.CanTeleport())
            {
                // stop the player.
                Navigator.Stop();
                await Coroutine.Sleep(240);

                // if the aetheryteId is stipulated, get zoneid.
                if (aetheryteid > 0 && zoneid == 0)
                    zoneid = WorldManager.GetZoneForAetheryteId(aetheryteid);

                // if zoneid is stipulated, get aetheryteid.
                if (aetheryteid == 0 && zoneid > 0)
                    aetheryteid = id.FirstOrDefault();

                // teleport.
                WorldManager.TeleportById(aetheryteid);

                // wait whilst we're casting.
                await Coroutine.Wait(5000, () => Core.Player.IsCasting);
                // wait until zoneId returns true, and we're not loading.
                await Coroutine.Wait(15000, () => (WorldManager.ZoneId == zoneid && !CommonBehaviors.IsLoading));
            }

            // return true if the player ISN'T on the required ZoneId, forcing the routine to go back.
            // return false if the player IS on the required ZoneId, allowing the routine to continue.
            return WorldManager.ZoneId != zoneid;
        }

        /// <summary>
        /// Checks ExBuddys EnableFlight plugin status.
        /// </summary>
        private static bool IsFlightDisabled
        {
            get
            {
                foreach (PluginContainer p in PluginManager.Plugins.Where(r => r.Plugin.Name == "Enable Flight"))
                {
                    if (p.Enabled) return false;
                }

                return true;
            }
        }
        /// <summary>
        /// Moves the player to the selected location.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="range"></param>
        /// <param name="destinationName"></param>
        /// <param name="stopInRange"></param>
        /// <param name="zoneId"></param>
        /// <param name="overrideMountRange"></param>
        /// <returns>true if not InPosition.</returns>
        public static async Task<bool> MoveAndStop(Vector3 destination, float range, string destinationName = null, bool stopInRange = false, ushort zoneId = 0, float overrideMountRange = 0f)
        {
            // override mount range if needed.
            if (overrideMountRange > 0)
                CharacterSettings.Instance.MountDistance = overrideMountRange;

            // if we're on the wrong zone, go there.
            if (zoneId > 0 && WorldManager.ZoneId != zoneId && await CreateTeleportBehavior(0, zoneId)) return true;

            if (!Navigator.InPosition(Core.Player.Location, destination, range))
            {
                // wait for mount
                if (Core.Player.Distance(destination) > CharacterSettings.Instance.MountDistance && await MountUp()) return true;

                // if exbuddy has failed it's takeoff, takeoff.
                if (!IsFlightDisabled && Core.Player.IsMounted && WorldManager.CanFly && !MovementManager.IsFlying && await CommonTasks.TakeOff()) return true;

                // get moving.
                Navigator.MoveTo(destination, destinationName);
            }

            if (Navigator.InPosition(Core.Player.Location, destination, range) && stopInRange)
            {
                Navigator.PlayerMover.MoveStop();
                // We're where we want to be, continue.
                return false;
            }

            // Navigator.InPosition is returning false, go back.
            return true;
        }

        internal static bool WindowsOpen()
        {
            return
                CraftingLog.IsOpen ||
                HousingGardening.IsOpen ||
                JournalAccept.IsOpen ||
                JournalResult.IsOpen ||
                JournalResult.ButtonClickable ||
                MaterializeDialog.IsOpen ||
                Repair.IsOpen ||
                Request.IsOpen ||
                SelectIconString.IsOpen ||
                SelectString.IsOpen ||
                SelectYesno.IsOpen ||
                Synthesis.IsOpen ||
                Talk.DialogOpen ||
                NowLoading.IsVisible ||
                Talk.ConvoLock ||
                QuestLogManager.InCutscene;
        }

        public async Task<bool> Interact()
        {
            if (!MovementManager.IsFlying && await InteractWith((uint)NpcId)) return true;

            if (await HandleWindows()) return true;

            return false;
        }

        private static bool DoneInteract()
        {
            if (Core.Player.HasTarget && WindowsOpen())
                return true;
            return false;
        }

        private static async Task<bool> InteractWith(uint npcId)
        {
            // small delay before interact
            await Coroutine.Sleep(100);
            // interact
            GameObjectManager.GetObjectByNPCId(npcId).Interact();
            // wait for windows open
            await Coroutine.Wait(3000, () => Core.Player.HasTarget && WindowsOpen());
            // ensure windows open
            return !DoneInteract();
        }

        private async Task<bool> HandleWindows()
        {
            while (WindowsOpen())
            {
                if (SelectString.IsOpen)
                {
                    if (DialogOption > -1)
                        SelectString.ClickSlot((uint)DialogOption);

                    SelectString.ClickLineEquals(QuestName);
                    await Coroutine.Sleep(100);
                }

                if (SelectIconString.IsOpen)
                {
                    if (DialogOption > -1)
                        SelectIconString.ClickSlot((uint)DialogOption);

                    SelectIconString.ClickLineEquals(QuestName);
                    await Coroutine.Sleep(100);
                }

                if (SelectYesno.IsOpen)
                {
                    SelectYesno.ClickYes();
                    await Coroutine.Sleep(100);
                }

                if (Request.IsOpen)
                {
                    var items = InventoryManager.FilledInventoryAndArmory.ToArray();
                    for (int i = 0; i < ItemIds.Length; i++)
                    {
                        BagSlot item;
                        if (RequiresHq[i])
                            item = items.FirstOrDefault(z => z.RawItemId == ItemIds[i] && z.IsHighQuality && !usedSlots.Contains(z));
                        else
                            item = items.FirstOrDefault(z => z.RawItemId == ItemIds[i] && !usedSlots.Contains(z));

                        item.Handover();
                        usedSlots.Add(item);
                    }
                    usedSlots.Clear();
                    Request.HandOver();
                    await Coroutine.Sleep(100);
                }

                if (JournalResult.IsOpen)
                {
                    if (JournalResult.ButtonClickable)
                        JournalResult.Complete();

                    if (hasRewards)
                        JournalResult.SelectSlot(RewardSlot);

                    await Coroutine.Sleep(100);
                }

                if (JournalAccept.IsOpen)
                {
                    JournalAccept.Accept();
                    await Coroutine.Sleep(100);
                }

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

            return QuestLogManager.InCutscene || Core.Player.HasTarget;
        }

        #endregion

        protected abstract Task<bool> Main();

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ctx => Main());
        }
    }
}
