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
    using ff14bot.RemoteWindows;
    using ff14bot.Settings;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using TreeSharp;

    public abstract class SoProfileBehavior : ProfileBehavior
    {
        #region Status Text
        private string Status;
        public override sealed string StatusText { get { return string.Concat(GetType().Name, ": ", Status); } set { Status = value; } }
        #endregion

        #region Defaults

        static SoProfileBehavior() { }

        protected SoProfileBehavior() { }

        protected virtual void OnTagStart() { }

        protected virtual void OnTagDone() { }

        protected sealed override void OnStart()
        {
            FlightCheck();
            GetQuestData();
            SetupConditional();
            TreeHooks.Instance.AddHook("Combat", new ActionRunCoroutine(cr => UsePotions()));
            OnTagStart();
        }

        private uint[] CurativePotions = new[] { 4554u, 4553u, 4552u, 4551u };
        private BagSlot Potion { get { return InventoryManager.FilledInventoryAndArmory.FirstOrDefault(r => CurativePotions.Contains(r.RawItemId)); } }
        protected async Task UsePotions()
        {
            if (Potion != null)
            {
                while (Me.CurrentHealthPercent < 45 && Potion.CanUse())
                {
                    if (Core.Me.IsCasting)
                        Actionmanager.StopCasting();

                    Potion.UseItem();
                    await Coroutine.Sleep(50);
                }
            }
        }

        protected sealed override void OnDone()
        {
            TreeHooks.Instance.RemoveHook("Combat", new ActionRunCoroutine(cr => UsePotions()));
            OnTagDone();
        }

        #endregion

       #region Get Quest Data

        private QuestResult ThisQuest;
        internal string NpcName { get { if (NpcId != 0) return DataManager.GetLocalizedNPCName(NpcId); return null; } }
        internal string QuestGiver { get { if (QuestId != 0) return DataManager.GetLocalizedNPCName((int)ThisQuest.PickupNPCId); return null; } }
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

        #region Movement

        #region Flight Check
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
        #endregion

        #region Path Navigation
        private readonly HashSet<uint> SupportedNpcs = new HashSet<uint>()
        {
            1000106,1000541,1000868,1001263,1001834,1002039,1002695,
            1003540,1003583,1003584,1003585,1003586,1003587,1003588,
            1003589,1003597,1003611,1004005,1004037,1004339,1004433,
            1005238,1011211,1011212,1011224,1011946,1011949,1012149,
            1012153,1012331,2001011,2001695,2005370,2005371,2005372
        };

        public string ZoneId
        {
            get
            {
                if (WorldManager.ZoneId.ToString() == "128")
                {
                    if (WorldManager.SubZoneId == 725)
                        return "725";
                    else
                        return "128";
                }

                if (WorldManager.ZoneId.ToString() == "130")
                {
                    if (WorldManager.SubZoneId == 654)
                        return "654";
                    else
                        return "130";
                }

                return WorldManager.ZoneId.ToString();
            }
        }

        static Path<TNode> FindPath<TNode>(TNode start, TNode destination) where TNode : IHasNeighbours<TNode>
        {
            var closed = new HashSet<TNode>();

            var queue = new PriorityQueue<double, Path<TNode>>();

            queue.Enqueue(0, new Path<TNode>(start));

            while (!queue.IsEmpty)
            {
                var path = queue.Dequeue();

                if (closed.Contains(path.LastStep))
                    continue;

                if (path.LastStep.Equals(destination))
                    return path;

                closed.Add(path.LastStep);

                foreach (TNode n in path.LastStep.Neighbours)
                {
                    var newPath = path.AddStep(n, 10);
                    queue.Enqueue(newPath.TotalCost, newPath);
                }

            }

            return null;
        }

        static Graph graph = new Graph();
        static void PopulateGraph()
        {
            CreateGraph(graph);
        }

        private Path<Node> GetPath()
        {
            string from = ZoneId;
            string to = MapId;
            Node start = graph.Nodes[from];
            Node end = graph.Nodes[to];
            Path<Node> Path = FindPath(start, end);

            foreach (Path<Node> p in Path.Reverse())
            {
                if (p.PreviousSteps != null)
                    return p;
            }

            Logging.WriteDiagnostic("Could not find a valid Path");
            return null;
        }

        public int SSO = -1;
        public string thisZone = null;
        private uint thisNpcId { get; set; }

        public async Task GoThere()
        {
            // if aetheryte available, we don't need the graph
            if (aeId > 0 && WorldManager.CanTeleport())
                return;

            // do it once
            if (graph.Nodes.Count <= 0)
                PopulateGraph();

            // calculate path on return
            while (ZoneId != MapId)
            {
                Path<Node> shortestPath = GetPath();
                if (shortestPath.LastStep.SSO < 0)
                {
                    await MoveAndStop(shortestPath.LastStep.XYZ, 3.24f, shortestPath.LastStep.Key);
                    if (InPosition(shortestPath.LastStep.XYZ, 3.24f))
                    {
                        thisZone = ZoneId;
                        MovementManager.MoveForwardStart();
                        await Coroutine.Sleep(4000);
                        MovementManager.MoveForwardStop();
                        await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => thisZone != ZoneId);
                    }
                }
                else
                {
                    await MoveAndStop(shortestPath.LastStep.XYZ, 3.24f, shortestPath.LastStep.Key);
                    if (InPosition(shortestPath.LastStep.XYZ, 3.24f))
                    {
                        SSO = shortestPath.LastStep.SSO;
                        var units = GameObjectManager.GameObjects;
                        foreach (var unit in units.OrderBy(r => r.Distance2D(Me.Location)))
                        {
                            foreach (var id in SupportedNpcs)
                            {
                                if (id == unit.NpcId && unit.Distance2D(Me.Location) < 5)
                                    thisNpcId = id;
                            }
                        }
                        thisZone = ZoneId;
                        await Coroutine.Sleep(500);
                        GameObjectManager.GetObjectByNPCId(thisNpcId).Interact();
                        await Coroutine.Wait(3000, () => Me.HasTarget && WindowsOpen());
                        await Coroutine.Sleep(500);
                        if (WindowsOpen())
                            await HandleWindows();
                        await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => thisZone != ZoneId);
                    }
                }

                await Coroutine.Yield();
            }
        }
        #endregion

        #region Teleport
        private static bool CanTeleport { get { return !Me.IsDead && !Me.InCombat; } }
        public async Task<bool> Teleport()
        {
            Poi.Clear("Teleporting to the correct Map.");
            Navigator.Stop();

            while (CanTeleport && ZoneId != MapId)
            {
                if (!Me.IsCasting && !CommonBehaviors.IsLoading)
                    WorldManager.TeleportById(aeId);

                await Snooze(5000);
            }
            return true;
        }

        private static async Task<bool> Snooze(uint ms)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds < ms && CanTeleport)
                await Coroutine.Yield();

            stopwatch.Stop();
            return CanTeleport;
        }

        #region Aetheryte Id
        private uint aeId
        {
            get
            {
                if ((ZoneId == "178" && MapId == " 131") || (ZoneId == "178" && MapId == "130"))
                    return 0; 
                if ((ZoneId == "210" && MapId == "131") || (ZoneId == "210" && MapId == "130"))
                    return 0;
                if (ZoneId == "131" && MapId == "130")
                    return 0;
                if (ZoneId == "130" && MapId == "131")
                    return 0;

                IEnumerable<uint> id = WorldManager.AvailableLocations.Where(x => x.ZoneId.ToString() == MapId).Select(r => r.AetheryteId);
                if (id != null)
                    return id.FirstOrDefault();
                else return 0;
            }
        }
        #endregion
        #endregion

        #region MoveAndStop
        public bool InPosition(Vector3 where, float distance) { if (where != null && distance != 0) return Me.Location.Distance(where) <= distance; else return Me.Location.Distance(Destination) <= Distance; }
        public async Task MoveAndStop(Vector3 location, float distance, string status, bool ignoreLanding = false)
        {
            if (aeId > 0 && ZoneId != MapId && WorldManager.CanTeleport())
                await Teleport();

            if (Me.Location.Distance(location) > distance)
            {
                if (!Me.IsMounted && Me.Location.Distance(location) > Settings.MountDistance && Settings.UseMount)
                    await CommonTasks.MountUp();

                if (Me.IsMounted && WorldManager.CanFly && !MovementManager.IsFlying && !NoFlight)
                    await CommonTasks.TakeOff();

                if (status != null)
                    StatusText = status;

                Navigator.MoveTo(location);
            }

            if (Me.Location.Distance(location) <= distance)
            {
                if (Me.IsMounted && MovementManager.IsFlying && !ignoreLanding)
                    await CommonTasks.Land();

                if (InPosition(location, distance))
                {
                    Navigator.PlayerMover.MoveStop();
                    Navigator.Clear();
                }
            }
        }
        #endregion

        #endregion

        #region Interact
        private async Task InteractWith(uint npcId)
        {
            await Coroutine.Sleep(500);
            GameObjectManager.GetObjectByNPCId(npcId).Interact();
            await Coroutine.Wait(3000, () => Me.HasTarget && WindowsOpen());
        }

        public async Task Interact()
        {
            await InteractWith((uint)NpcId);
            await HandleWindows();
        }

        public async Task HandleWindows()
        {
            while (WindowsOpen())
            {
                if (SelectString.IsOpen)
                {
                    if (SSO > -1)
                        SelectString.ClickSlot((uint)SSO);
                    if (DialogOption > -1)
                        SelectString.ClickSlot((uint)DialogOption);

                    SelectString.ClickLineEquals(QuestName);
                    await Coroutine.Sleep(100);
                }

                if (SelectIconString.IsOpen)
                {
                    if (SSO > -1)
                        SelectIconString.ClickSlot((uint)SSO);
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

                await Coroutine.Yield();
            }

            //if (!WindowsOpen())
            //    return;
        }
        #endregion

        #region Conditional
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
        #endregion

        #region IsDone Overriders
        internal bool _done;
        internal bool HasQuest { get { if (QuestId > 0) return ConditionParser.HasQuest(QuestId); else return true; } }
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

                        if (item != null)
                            return true;
                    }
                    return false;
                }
                return true;
            }
        }
        internal bool IsOnTurnInStep { get { return ConditionParser.GetQuestStep(QuestId) == 255; } }
        internal bool IsObjectiveCountComplete { get { if (Objective > -1 && Count > -1) return ConditionParser.GetQuestById(QuestId).GetTodoArgs(Objective).Item1 >= Count; else return false; } }
        internal bool IsObjectiveComplete { get { if (QuestId > 0 && StepId > 0 && Objective > -1) return ConditionParser.IsTodoChecked(QuestId, (int)StepId, Objective); else return false; } }
        internal bool IsQuestAcceptQualified { get { return ConditionParser.IsQuestAcceptQualified(QuestId); } }
        #endregion

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
        public Vector3 XYZ { get { return Destination; } set { Destination = value; } }
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

        [XmlAttribute("MapId")]
        public string MapId { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("NoFlight")]
        public bool NoFlight { get; set; }
        #endregion

        #region Misc
        protected static LocalPlayer Me { get { return GameObjectManager.LocalPlayer; } }
        protected CharacterSettings Settings { get { return CharacterSettings.Instance; } }

        protected static async Task Dismount()
        {
            while (Me.IsMounted)
            {
                Actionmanager.Dismount();
                await Coroutine.Yield();
            }

            await Coroutine.Sleep(1000);
        }

        #region Zone Switch
        private bool ZoneSet;
        private uint CurrentSubZone;
        protected void LogSubZone()
        {
            CurrentSubZone = WorldManager.SubZoneId;
            ZoneSet = true;
        }

        public bool ZoneChanged()
        {
            if (ZoneSet && WorldManager.SubZoneId != CurrentSubZone)
            {
                ZoneSet = false;
                return true;
            }
            else
                return false;
        }
        #endregion
        #endregion

        #region Window Check
        internal bool WindowsOpen()
        {
            while ((Me.HasTarget && (CraftingLog.IsOpen || HousingGardening.IsOpen || JournalAccept.IsOpen || JournalResult.IsOpen
                || JournalResult.ButtonClickable || MaterializeDialog.IsOpen || Repair.IsOpen || Request.IsOpen
                || SelectIconString.IsOpen || SelectString.IsOpen || SelectYesno.IsOpen || Synthesis.IsOpen
                || Talk.DialogOpen || NowLoading.IsVisible || Talk.ConvoLock)) || QuestLogManager.InCutscene)

                return true;

            return false;
        }
        #endregion

        #region Behavior
        protected abstract Task Main();
        protected override Composite CreateBehavior() { return new ActionRunCoroutine(ctx => Main()); }
        #endregion

        #region Pathfinding

        class PriorityQueue<P, V> : IEnumerable
        {
            private SortedDictionary<P, Queue<V>> list = new SortedDictionary<P, Queue<V>>();

            public void Enqueue(P priority, V value)
            {
                Queue<V> q;

                if (!list.TryGetValue(priority, out q))
                {
                    q = new Queue<V>();

                    list.Add(priority, q);
                }

                q.Enqueue(value);
            }

            public V Dequeue()
            {
                // will throw if there isn’t any first element!
                var pair = list.First();

                var v = pair.Value.Dequeue();

                if (pair.Value.Count == 0) // nothing left of the top priority.
                    list.Remove(pair.Key);

                return v;
            }

            public bool IsEmpty
            {
                get { return !list.Any(); }
            }

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return list.GetEnumerator();
            }

            #endregion
        }

        interface IHasNeighbours<N>
        {
            IEnumerable<N> Neighbours { get; }
        }

        public class Graph
        {
            #region Private Member Variables
            private NodeList nodes;
            #endregion

            #region Constructor
            public Graph()
            {
                this.nodes = new NodeList();
            }

            public Graph(NodeList nodes)
            {
                this.nodes = nodes;
            }
            #endregion

            #region Public Methods
            public virtual void Clear()
            {
                nodes.Clear();
            }

            #region Adding TNode Methods
            public virtual Node AddNode(string key, Vector3 xyz, int sso)
            {
                if (!nodes.ContainsKey(key))
                {
                    Node n = new Node(key, xyz, sso);
                    nodes.Add(n);
                    return n;
                }
                else
                    throw new ArgumentException("There already exists a node in the graph with key " + key);
            }

            public virtual void AddNode(Node n)
            {
                if (!nodes.ContainsKey(n.Key))
                    nodes.Add(n);
                else
                    throw new ArgumentException("There already exists a node in the graph with key " + n.Key);
            }
            #endregion

            #region Adding Edge Methods
            public virtual void AddEdge(Node u, Node v)
            {
                AddEdge(u, v, 10);
            }

            public virtual void AddEdge(Node u, Node v, int cost)
            {
                if (nodes.ContainsKey(u.Key) && nodes.ContainsKey(v.Key))
                {
                    u.AddDirected(v, cost);
                    v.AddDirected(u, cost);
                }
                else
                    throw new ArgumentException("One or both of the nodes supplied were not members of the graph.");
            }

            public virtual void AddEdge(Node u, Node v, double cost)
            {
                if (nodes.ContainsKey(u.Key) && nodes.ContainsKey(v.Key))
                {
                    u.AddDirected(v, cost);
                    v.AddDirected(u, cost);
                }
                else
                    throw new ArgumentException("One or both of the nodes supplied were not members of the graph.");
            }

            #endregion

            #region Contains Methods
            public virtual bool Contains(Node n)
            {
                return Contains(n.Key);
            }

            public virtual bool Contains(string key)
            {
                return nodes.ContainsKey(key);
            }
            #endregion
            #endregion

            #region Public Properties
            public virtual int Count
            {
                get
                {
                    return nodes.Count;
                }
            }

            public virtual NodeList Nodes
            {
                get
                {
                    return this.nodes;
                }
            }
            #endregion
        }

        class Path<Node> : IEnumerable<Path<Node>>
        {
            public Node LastStep { get; private set; }

            public Path<Node> PreviousSteps { get; private set; }

            public double TotalCost { get; private set; }

            private Path(Node lastStep, Path<Node> previousSteps, double totalCost)
            {
                LastStep = lastStep;

                PreviousSteps = previousSteps;

                TotalCost = totalCost;
            }

            public Path(Node start) : this(start, null, 0) { }

            public Path<Node> AddStep(Node step, double stepCost)
            {
                return new Path<Node>(step, this, TotalCost + stepCost);
            }

            public IEnumerator<Path<Node>> GetEnumerator()
            {
                for (Path<Node> p = this; p != null; p = p.PreviousSteps)
                    yield return p;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class NodeList : IEnumerable
        {
            private Hashtable data = new Hashtable();

            #region Public Methods
            public virtual void Add(Node n)
            {
                data.Add(n.Key, n);
            }

            public virtual void Remove(Node n)
            {
                data.Remove(n.Key);
            }

            public virtual bool ContainsKey(string key)
            {
                return data.ContainsKey(key);
            }

            public virtual void Clear()
            {
                data.Clear();
            }

            public IEnumerator GetEnumerator()
            {
                return new NodeListEnumerator(data.GetEnumerator());
            }
            #endregion

            #region Public Properties
            public virtual Node this[string key]
            {
                get
                {
                    return (Node)data[key];
                }
            }

            public virtual int Count
            {
                get
                {
                    return data.Count;
                }
            }
            #endregion

            #region NodeList Enumerator
            class NodeListEnumerator : IEnumerator, IDisposable
            {
                IDictionaryEnumerator list;
                public NodeListEnumerator(IDictionaryEnumerator coll)
                {
                    list = coll;
                }

                public void Reset()
                {
                    list.Reset();
                }

                public bool MoveNext()
                {
                    return list.MoveNext();
                }

                public Node Current
                {
                    get
                    {
                        return (Node)((DictionaryEntry)list.Current).Value;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return (Current);
                    }
                }

                public void Dispose()
                {
                    list = null;
                }
            }
            #endregion
        }

        public partial class Node
        {
            #region Public Properties

            public string Key { get; private set; }

            public Vector3 XYZ { get; set; }

            public int SSO { get; set; }

            public AdjacencyList Neighbors { get; private set; }

            public Node PathParent { get; set; }

            #endregion

            #region Constructors

            public Node(string key, Vector3 xyz, int sso) : this(key, xyz, sso, null)
            {
            }

            public Node(string key, Vector3 data, int sso, AdjacencyList neighbors)
            {
                Key = key;
                XYZ = data;
                SSO = sso;

                if (neighbors == null)
                {
                    Neighbors = new AdjacencyList();
                }
                else
                {
                    Neighbors = neighbors;
                }
            }

            public Node(string key)
            {
                Key = key;
                XYZ = Vector3.Zero;
                SSO = -1;
                Neighbors = new AdjacencyList();
            }

            #endregion

            #region Public Methods

            #region Add Methods

            internal void AddDirected(Node n)
            {
                AddDirected(new EdgeToNeighbor(n));
            }

            internal void AddDirected(Node n, int cost)
            {
                AddDirected(new EdgeToNeighbor(n, cost));
            }

            internal void AddDirected(Node n, double cost)
            {
                AddDirected(new EdgeToNeighbor(n, cost));
            }

            internal void AddDirected(EdgeToNeighbor e)
            {
                Neighbors.Add(e);
            }

            #endregion

            public override string ToString()
            {
                return string.Format("Key = {0} | XYZ = {1} | SSO = {2}", Key, XYZ, SSO);
            }

            #endregion
        }

        sealed partial class Node : IHasNeighbours<Node>
        {
            public IEnumerable<Node> Neighbours
            {
                get
                {
                    List<Node> nodes = new List<Node>();

                    foreach (EdgeToNeighbor etn in Neighbors)
                    {
                        nodes.Add(etn.Neighbor);
                    }

                    return nodes;
                }
            }
        }

        public class AdjacencyList : CollectionBase
        {
            protected internal virtual void Add(EdgeToNeighbor e)
            {
                base.InnerList.Add(e);
            }

            public virtual EdgeToNeighbor this[int index]
            {
                get { return (EdgeToNeighbor)base.InnerList[index]; }
                set { base.InnerList[index] = value; }
            }
        }

        public class EdgeToNeighbor
        {
            #region Public Properties

            public virtual double Cost { get; private set; }

            public virtual Node Neighbor { get; private set; }

            #endregion

            #region Constructors

            public EdgeToNeighbor(Node neighbor) : this(neighbor, 0)
            {
            }

            public EdgeToNeighbor(Node neighbor, double cost)
            {
                Cost = cost;
                Neighbor = neighbor;
            }

            #endregion

            #region Public Methods

            public override string ToString()
            {
                return string.Format("Neighbor = {0} | Cost = {1}", Neighbor.Key, Cost);
            }

            #endregion
        }

        public static void CreateGraph(Graph graph)
        {
            // Target Locations

            Node Idyllshire = new Node("478"); graph.AddNode(Idyllshire);
            Node DravanianHinterlandsEast = new Node("399_East"); graph.AddNode(DravanianHinterlandsEast);
            Node DravanianHinterlandsWest = new Node("399_West"); graph.AddNode(DravanianHinterlandsWest);
            Node DravanianForelands = new Node("398"); graph.AddNode(DravanianForelands);
            Node AzysLla = new Node("402"); graph.AddNode(AzysLla);
            Node TheSeaOfClouds = new Node("401"); graph.AddNode(TheSeaOfClouds);
            Node TheChurningMists = new Node("400"); graph.AddNode(TheChurningMists);
            Node CoerthasWesternHighlands = new Node("397"); graph.AddNode(CoerthasWesternHighlands);
            Node ThePillars = new Node("419"); graph.AddNode(ThePillars);
            Node Foundation = new Node("418"); graph.AddNode(Foundation);
            Node CoerthasCentralHighlands = new Node("155"); graph.AddNode(CoerthasCentralHighlands);
            Node MorDhona = new Node("156"); graph.AddNode(MorDhona);
            Node OldGridania = new Node("133"); graph.AddNode(OldGridania);
            Node LotusStand = new Node("205"); graph.AddNode(LotusStand);
            Node NewGridania = new Node("132"); graph.AddNode(NewGridania);
            Node NorthShroud = new Node("154"); graph.AddNode(NorthShroud);
            Node CentralShroud = new Node("148"); graph.AddNode(CentralShroud);
            Node EastShroud = new Node("152"); graph.AddNode(EastShroud);
            Node SouthShroud = new Node("153"); graph.AddNode(SouthShroud);
            Node NorthernThanalan = new Node("147"); graph.AddNode(NorthernThanalan);
            Node WesternThanalan = new Node("140"); graph.AddNode(WesternThanalan);
            Node EasternThanalan = new Node("145"); graph.AddNode(EasternThanalan);
            Node TheWakingSands = new Node("212"); graph.AddNode(TheWakingSands);
            Node CentralThanalan = new Node("141"); graph.AddNode(CentralThanalan);
            Node SouthernThanalan = new Node("146"); graph.AddNode(SouthernThanalan);
            Node UldahStepsOfNald = new Node("130"); graph.AddNode(UldahStepsOfNald);
            Node UldahInnRoom = new Node("178"); graph.AddNode(UldahInnRoom);
            Node HeartOfTheSworn = new Node("210"); graph.AddNode(HeartOfTheSworn);
            Node UldahAirShip = new Node("654"); graph.AddNode(UldahAirShip);
            Node UldahStepsOfThal = new Node("131"); graph.AddNode(UldahStepsOfThal);
            Node OuterLaNoscea = new Node("180"); graph.AddNode(OuterLaNoscea);
            Node UpperLaNosceaWest = new Node("139_West"); graph.AddNode(UpperLaNosceaWest);
            Node UpperLaNosceaEast = new Node("139_East"); graph.AddNode(UpperLaNosceaEast);
            Node WesternLaNoscea = new Node("138"); graph.AddNode(WesternLaNoscea);
            Node EasternLaNosceaWest = new Node("137_West"); graph.AddNode(EasternLaNosceaWest);
            Node EasternLaNosceaEast = new Node("137_East"); graph.AddNode(EasternLaNosceaEast);
            Node MiddleLaNoscea = new Node("134"); graph.AddNode(MiddleLaNoscea);
            Node LowerLaNoscea = new Node("135"); graph.AddNode(LowerLaNoscea);
            Node LimsaLominsaLowerDecks = new Node("129"); graph.AddNode(LimsaLominsaLowerDecks);
            Node LimsaLominsaUpperDecks = new Node("128"); graph.AddNode(LimsaLominsaUpperDecks);
            Node LimsaAirShip = new Node("725"); graph.AddNode(LimsaAirShip);

            // One Directional Boundaries

            // Idyllshire

            Node IdyllToDravHintW = new Node("Idyllshire > The Dravanian Hinterlands West", new Vector3(74.39938f, 205f, 140.4551f), -1); graph.AddNode(IdyllToDravHintW); Idyllshire.AddDirected(IdyllToDravHintW); IdyllToDravHintW.AddDirected(DravanianHinterlandsWest);
            Node IdyllToDravHintE = new Node("Idyllshire > The Dravanian Hinterlands East", new Vector3(144.5908f, 207f, 114.8838f), -1); graph.AddNode(IdyllToDravHintE); Idyllshire.AddDirected(IdyllToDravHintE); IdyllToDravHintE.AddDirected(DravanianHinterlandsEast);

            // The Dravanian Hinterlands

            Node DravHintWToIdyll = new Node("The Dravanian Hinterlands West > Idyllshire", new Vector3(-540.4974f, 155.7123f, -515.0025f), -1); graph.AddNode(DravHintWToIdyll); DravanianHinterlandsWest.AddDirected(DravHintWToIdyll); DravHintWToIdyll.AddDirected(Idyllshire);
            Node DravHintEToIdyll = new Node("The Dravanian Hinterlands East > Idyllshire", new Vector3(-227.6785f, 106.5826f, -628.679f), -1); graph.AddNode(DravHintEToIdyll); DravanianHinterlandsEast.AddDirected(DravHintEToIdyll); DravHintEToIdyll.AddDirected(Idyllshire);
            Node DravHintEToDravFore = new Node("The Dravanian Hinterlands East > The Dravanian Forelands", new Vector3(904.6548f, 161.711f, 189.163f), -1); graph.AddNode(DravHintEToDravFore); DravanianHinterlandsEast.AddDirected(DravHintEToDravFore); DravHintEToDravFore.AddDirected(DravanianForelands);

            // The Churning Mists 

            Node ChurningMistsToDravFore = new Node("The Churning Mists > The Dravanian Forelands", new Vector3(201.5868f, -68.68091f, 709.3461f), 1); graph.AddNode(ChurningMistsToDravFore); TheChurningMists.AddDirected(ChurningMistsToDravFore); ChurningMistsToDravFore.AddDirected(DravanianForelands);

            // The Dravanian Forelands

            Node DravForeToDravHintE = new Node("The Dravanian Forelands > The Dravanian Hinterlands East", new Vector3(-795.4093f, -122.2338f, 577.756f), -1); graph.AddNode(DravForeToDravHintE); DravanianForelands.AddDirected(DravForeToDravHintE); DravForeToDravHintE.AddDirected(DravanianHinterlandsEast);
            Node DravForeToChurningMists = new Node("The Dravanian Forelands > The Churning Mists", new Vector3(-692.5875f, 5.001416f, -838.3893f), 1); graph.AddNode(DravForeToChurningMists); DravanianForelands.AddDirected(DravForeToChurningMists); DravForeToChurningMists.AddDirected(TheChurningMists);
            Node DravForeToCoerthasW = new Node("The Dravanian Forelands > Coerthas Western Highlands", new Vector3(870.7913f, -3.649778f, 350.4391f), -1); graph.AddNode(DravForeToCoerthasW); DravanianForelands.AddDirected(DravForeToCoerthasW); DravForeToCoerthasW.AddDirected(CoerthasWesternHighlands);

            // Camp Cloudtop

            Node SeaOfCloudsToPillars = new Node("Camp Cloudtop > The Pillars", new Vector3(-734.9813f, -105.0583f, 459.3728f), 1); TheSeaOfClouds.AddDirected(SeaOfCloudsToPillars); SeaOfCloudsToPillars.AddDirected(ThePillars); graph.AddNode(SeaOfCloudsToPillars);

            // Coerthas Western Highlands

            Node CoerthasWToDravFore = new Node("Coerthas Western Highlands > The Dravanian Forelands", new Vector3(-848.7283f, 117.683f, -655.5744f), -1); graph.AddNode(CoerthasWToDravFore); CoerthasWesternHighlands.AddDirected(CoerthasWToDravFore); CoerthasWToDravFore.AddDirected(DravanianForelands);
            Node CoerthasWToFoundation = new Node("Coerthas Western Highlands > Foundation", new Vector3(469.2851f, 224.4823f, 879.3615f), -1); graph.AddNode(CoerthasWToFoundation); CoerthasWesternHighlands.AddDirected(CoerthasWToFoundation); CoerthasWToFoundation.AddDirected(Foundation);

            // Azys Lla

            Node AzysToPillars = new Node("Azys Lla > The Pillars", new Vector3(-877.0629f, -184.3138f, -670.1103f), 1); graph.AddNode(AzysToPillars); AzysLla.AddDirected(AzysToPillars); AzysToPillars.AddDirected(ThePillars);

            // The Pillars

            Node PillarsToAzys = new Node("The Pillars > Azys Lla", new Vector3(168.4442f, -14.34896f, 49.57654f), 1); graph.AddNode(PillarsToAzys); ThePillars.AddDirected(PillarsToAzys); PillarsToAzys.AddDirected(AzysLla);
            Node PillarsToFoundation = new Node("The Pillars > Foundation", new Vector3(-16.78843f, -13.06285f, -67.11987f), -1); graph.AddNode(PillarsToFoundation); ThePillars.AddDirected(PillarsToFoundation); PillarsToFoundation.AddDirected(Foundation);
            Node PillarsToCloudtop = new Node("The Pillars > Camp Cloudtop", new Vector3(151.9916f, -12.55534f, -7.858459f), 1); graph.AddNode(PillarsToCloudtop); ThePillars.AddDirected(PillarsToCloudtop); PillarsToCloudtop.AddDirected(TheSeaOfClouds);

            // Foundation

            Node FoundationToCoerthasC = new Node("Foundation > Coerthas Central Highlands", new Vector3(4.592957f, -2.52555f, 149.4926f), 0); Foundation.AddDirected(FoundationToCoerthasC); FoundationToCoerthasC.AddDirected(CoerthasCentralHighlands);
            Node FoundationToCoerthasW = new Node("Foundation > Coerthas Western Highlands", new Vector3(-187.9524f, 14.72722f, -57.81636f), -1); Foundation.AddDirected(FoundationToCoerthasW); FoundationToCoerthasW.AddDirected(CoerthasWesternHighlands);
            Node FoundationToPillars = new Node("Foundation > The Pillars", new Vector3(-57.32227f, 20.69349f, -96.31832f), -1); Foundation.AddDirected(FoundationToPillars); FoundationToPillars.AddDirected(ThePillars);

            // Coerthas Central Highlands

            Node CoerthasCToFoundation = new Node("Coerthas Central Highlands > Foundation", new Vector3(-163.8972f, 304.1538f, -333.0587f), 0); graph.AddNode(CoerthasCToFoundation); CoerthasCentralHighlands.AddDirected(CoerthasCToFoundation); CoerthasCToFoundation.AddDirected(Foundation);
            Node CoerthasCToNorthShroud = new Node("Coerthas Central Highlands > North Shroud", new Vector3(9.579012f, 183.084641f, 580.486389f), -1); graph.AddNode(CoerthasCToNorthShroud); CoerthasCentralHighlands.AddDirected(CoerthasCToNorthShroud); CoerthasCToNorthShroud.AddDirected(NorthShroud);
            Node CoerthasCToMorDhona = new Node("Coerthas Central Highlands > Mor Dhona", new Vector3(-221.167343f, 217.634018f, 702.521790f), -1); graph.AddNode(CoerthasCToMorDhona); CoerthasCentralHighlands.AddDirected(CoerthasCToMorDhona); CoerthasCToMorDhona.AddDirected(MorDhona);

            // Mor Dhona

            Node MorDhonaToNorthThan = new Node("Mor Dhona > Northern Thanalan", new Vector3(-419.502350f, -3.216816f, -116.779129f), -1); graph.AddNode(MorDhonaToNorthThan); MorDhona.AddDirected(MorDhonaToNorthThan); MorDhonaToNorthThan.AddDirected(NorthernThanalan);
            Node MorDhonaToCoerthasC = new Node("Mor Dhona > Coerthas Central Highlands", new Vector3(124.911911f, 31.45f, -770.00f), -1); graph.AddNode(MorDhonaToCoerthasC); MorDhona.AddDirected(MorDhonaToCoerthasC); MorDhonaToCoerthasC.AddDirected(CoerthasCentralHighlands);

            // The Black Shroud

            // North Shroud
            Node NorthShroudToOldGridania = new Node("North Shroud > Old Gridania", new Vector3(454.377014f, -1.377332f, 194.206085f), -1); graph.AddNode(NorthShroudToOldGridania); NorthShroud.AddDirected(NorthShroudToOldGridania); NorthShroudToOldGridania.AddDirected(OldGridania);
            Node NorthShroudToCentralShroud = new Node("North Shroud > Central Shroud", new Vector3(18.537865f, -54.870895f, 531.396240f), -1); graph.AddNode(NorthShroudToCentralShroud); NorthShroud.AddDirected(NorthShroudToCentralShroud); NorthShroudToCentralShroud.AddDirected(CentralShroud);
            Node NorthShroudToCoerthasC = new Node("North Shroud > Coerthas Central Highlands", new Vector3(-369.235107f, -5.984657f, 189.336166f), -1); graph.AddNode(NorthShroudToCoerthasC); NorthShroud.AddDirected(NorthShroudToCoerthasC); NorthShroudToCoerthasC.AddDirected(CoerthasCentralHighlands);
            // Old Gridania
            Node OldGridaniaToNewGridania = new Node("Old Gridania > New Gridania", new Vector3(140.014786f, 11.062968f, -21.999306f), -1); graph.AddNode(OldGridaniaToNewGridania); OldGridania.AddDirected(OldGridaniaToNewGridania); OldGridaniaToNewGridania.AddDirected(NewGridania);
            Node OldGridaniaToEastShroud = new Node("Old Gridania > East Shroud", new Vector3(179.866f, -2.239f, -241.585f), 1); graph.AddNode(OldGridaniaToEastShroud); OldGridania.AddDirected(OldGridaniaToEastShroud); OldGridaniaToEastShroud.AddDirected(EastShroud);
            Node OldGridaniaToNorthShroud = new Node("Old Gridania > North Shroud", new Vector3(-207.365845f, 10.368533f, -95.740646f), -1); graph.AddNode(OldGridaniaToNorthShroud); OldGridania.AddDirected(OldGridaniaToNorthShroud); OldGridaniaToNorthShroud.AddDirected(NorthShroud);
            Node OldGridaniaToLotusStand = new Node("Old Gridania > Lotus Stand", new Vector3(-159.5f, 4f, -4.199f), -1); graph.AddNode(OldGridaniaToLotusStand); OldGridania.AddDirected(OldGridaniaToLotusStand); OldGridaniaToLotusStand.AddDirected(LotusStand);
            Node LotusStandToOldGridania = new Node("Lotus Stand > Old Gridania", new Vector3(45.799f, 7.699f, 47.400f), -1); graph.AddNode(LotusStandToOldGridania); LotusStand.AddDirected(LotusStandToOldGridania); LotusStandToOldGridania.AddDirected(OldGridania);
            // New Gridania
            Node NewGridaniaToOldGridania = new Node("New Gridania > Old Gridania", new Vector3(100.587738f, 4.958726f, 15.518513f), -1); graph.AddNode(NewGridaniaToOldGridania); NewGridania.AddDirected(NewGridaniaToOldGridania); NewGridaniaToOldGridania.AddDirected(OldGridania);
            Node NewGridaniaToCentShroud = new Node("New Gridania > Central Shroud", new Vector3(154.172577f, -12.851933f, 157.515976f), -1); graph.AddNode(NewGridaniaToCentShroud); NewGridania.AddDirected(NewGridaniaToCentShroud); NewGridaniaToCentShroud.AddDirected(CentralShroud);
            Node NewGridaniaToLimsaAirShip = new Node("New Gridania > Limsa Lominsa Airship Landing", new Vector3(29.00732f, -19f, 105.4856f), 1); graph.AddNode(NewGridaniaToLimsaAirShip); NewGridania.AddDirected(NewGridaniaToLimsaAirShip); NewGridaniaToLimsaAirShip.AddDirected(LimsaAirShip);
            // Central Shroud
            Node CentralShroudToNewGridania = new Node("Central Shroud > New Gridania", new Vector3(127.973625f, 25.274239f, -315.603302f), -1); graph.AddNode(CentralShroudToNewGridania); CentralShroud.AddDirected(CentralShroudToNewGridania); CentralShroudToNewGridania.AddDirected(NewGridania);
            Node CentralShroudToEastShroud = new Node("Central Shroud > East Shroud", new Vector3(385.957275f, -3.278250f, -184.674515f), -1); graph.AddNode(CentralShroudToEastShroud); CentralShroud.AddDirected(CentralShroudToEastShroud); CentralShroudToEastShroud.AddDirected(EastShroud);
            Node CentralShroudToSouthShroud = new Node("Central Shroud > South Shroud", new Vector3(159.596100f, -23.807894f, 550.260925f), -1); graph.AddNode(CentralShroudToSouthShroud); CentralShroud.AddDirected(CentralShroudToSouthShroud); CentralShroudToSouthShroud.AddDirected(SouthShroud);
            Node CentralShroudToNorthShroud = new Node("Central Shroud > North Shroud", new Vector3(-501.604462f, 74.197563f, -355.052673f), -1); graph.AddNode(CentralShroudToNorthShroud); CentralShroud.AddDirected(CentralShroudToNorthShroud); CentralShroudToNorthShroud.AddDirected(NorthShroud);
            // East Shroud
            Node EastShroudToOldGridania = new Node("East Shroud > Old Gridania", new Vector3(-575.629f, 8.274f, 74.9197f), 1); graph.AddNode(EastShroudToOldGridania); EastShroud.AddDirected(EastShroudToOldGridania); EastShroudToOldGridania.AddDirected(OldGridania);
            Node EastShroudToCentralShroud = new Node("East Shroud > Central Shroud", new Vector3(-515.380066f, 18.856667f, 276.312958f), -1); graph.AddNode(EastShroudToCentralShroud); EastShroud.AddDirected(EastShroudToCentralShroud); EastShroudToCentralShroud.AddDirected(CentralShroud);
            Node EastShroudToSouthShroud = new Node("East Shroud > South Shroud", new Vector3(-161.377731f, 4.327631f, 450.613647f), -1); graph.AddNode(EastShroudToSouthShroud); EastShroud.AddDirected(EastShroudToSouthShroud); EastShroudToSouthShroud.AddDirected(SouthShroud);
            // South Shroud
            Node SouthShroudToCentralShroud = new Node("South Shroud > Central Shroud", new Vector3(-368.008545f, 29.841984f, -243.654633f), -1); graph.AddNode(SouthShroudToCentralShroud); SouthShroud.AddDirected(SouthShroudToCentralShroud); SouthShroudToCentralShroud.AddDirected(CentralShroud);
            Node SouthShroudToEastShroud = new Node("South Shroud > East Shroud", new Vector3(276.864288f, 11.068289f, -259.381195f), -1); graph.AddNode(SouthShroudToEastShroud); SouthShroud.AddDirected(SouthShroudToEastShroud); SouthShroudToEastShroud.AddDirected(EastShroud);
            Node SouthShroudToEastThan = new Node("South Shroud > Eastern Thanalan", new Vector3(-285.880859f, -0.202368f, 696.760864f), -1); graph.AddNode(SouthShroudToEastThan); SouthShroud.AddDirected(SouthShroudToEastThan); SouthShroudToEastThan.AddDirected(EasternThanalan);

            // Thanalan

            // Northern Thanalan
            Node NorthThanToCentralThan = new Node("North Thanalan > Central Thanalan", new Vector3(36.747425f, 6.114986f, 512.867920f), -1); graph.AddNode(NorthThanToCentralThan); NorthernThanalan.AddDirected(NorthThanToCentralThan); NorthThanToCentralThan.AddDirected(CentralThanalan);
            Node NorthThanToMorDhona = new Node("Northern Thanalan > Mor Dhona", new Vector3(-96.634842f, 84.427101f, -415.727081f), -1); graph.AddNode(NorthThanToMorDhona); NorthernThanalan.AddDirected(NorthThanToMorDhona); NorthThanToMorDhona.AddDirected(MorDhona);
            // Western Thanalan
            Node WestThanToCentralThan = new Node("Western Thanalan > Central Thanalan", new Vector3(261.533295f, 53.309750f, -9.452567f), -1); graph.AddNode(WestThanToCentralThan); WesternThanalan.AddDirected(WestThanToCentralThan); WestThanToCentralThan.AddDirected(CentralThanalan);
            Node WestThanToUldahNald = new Node("Western Thanalan > Ul'dah Steps of Nald", new Vector3(471.744293f, 96.620567f, 159.596161f), -1); graph.AddNode(WestThanToUldahNald); WesternThanalan.AddDirected(WestThanToUldahNald); WestThanToUldahNald.AddDirected(UldahStepsOfNald);
            Node WestThanToWakingSands = new Node("Western Thanalan > The Waking Sands", new Vector3(-482.076f, 17.075f, -387.232f), -1); graph.AddNode(WestThanToWakingSands); WesternThanalan.AddDirected(WestThanToWakingSands); WestThanToWakingSands.AddDirected(TheWakingSands);
            Node WakingSandsToWestThan = new Node("The Waking Sands > Western Thanalan", new Vector3(-15.132613182068f, 0f, -0.0081899836659431f), -1); graph.AddNode(WakingSandsToWestThan); TheWakingSands.AddDirected(WakingSandsToWestThan); WakingSandsToWestThan.AddDirected(WesternThanalan);
            Node WestThanToLimsaLower = new Node("Western Thanalan > Limsa Lominsa Lower Decks", new Vector3(-486.74f, 23.99f, -331.675f), 1); graph.AddNode(WestThanToLimsaLower); WesternThanalan.AddDirected(WestThanToLimsaLower); WestThanToLimsaLower.AddDirected(LimsaLominsaLowerDecks);
            // Eastern Thanalan
            Node EastThanToCentralShroud = new Node("Eastern Thanalan > Central Shroud", new Vector3(369.957184f, 32.385502f, -296.594879f), -1); graph.AddNode(EastThanToCentralShroud); EasternThanalan.AddDirected(EastShroudToCentralShroud); EastShroudToCentralShroud.AddDirected(SouthShroud);
            Node EastThanToSouthThan = new Node("Eastern Thanalan > Southern Thanalan", new Vector3(-169.531570f, -46.311321f, 489.810608f), -1); graph.AddNode(EastThanToSouthThan); EasternThanalan.AddDirected(EastThanToSouthThan); EastThanToSouthThan.AddDirected(SouthernThanalan);
            Node EastThanToCentralThan = new Node("Eastern Thanalan > Central Thanalan", new Vector3(-564.688477f, -19.019501f, 340.296539f), -1); graph.AddNode(EastThanToCentralThan); EasternThanalan.AddDirected(EastThanToCentralThan); EastThanToCentralThan.AddDirected(CentralThanalan);
            // Central Thanalan
            Node CentralThanToUldahNald = new Node("Central Thanalan > Ul'dah Steps of Nald", new Vector3(-116.704605f, 18.374495f, 339.036774f), -1); graph.AddNode(CentralThanToUldahNald); CentralThanalan.AddDirected(CentralThanToUldahNald); CentralThanToUldahNald.AddDirected(UldahStepsOfNald);
            Node CentralThanToUldahThal = new Node("Central Thanalan > Ul'dah Steps of Thal", new Vector3(13.381968f, 18.375681f, 563.856323f), -1); graph.AddNode(CentralThanToUldahThal); CentralThanalan.AddDirected(CentralThanToUldahThal); CentralThanToUldahThal.AddDirected(UldahStepsOfThal);
            Node CentralThanToEastThan = new Node("Central Thanalan > Eastern Thanalan", new Vector3(450.149078f, -17.999840f, -179.656448f), -1); graph.AddNode(CentralThanToEastThan); CentralThanalan.AddDirected(CentralThanToEastThan); CentralThanToEastThan.AddDirected(EasternThanalan);
            Node CentralThanToSouthThan = new Node("Central Thanalan > Southern Thanalan", new Vector3(232.728973f, 2.753296f, 674.237854f), -1); graph.AddNode(CentralThanToSouthThan); CentralThanalan.AddDirected(CentralThanToSouthThan); CentralThanToSouthThan.AddDirected(SouthernThanalan);
            Node CentralThanToNorthThan = new Node("Central Thanalan > Northern Thanalan", new Vector3(-27.951309f, 33.000000f, -494.383026f), -1); graph.AddNode(CentralThanToNorthThan); CentralThanalan.AddDirected(CentralThanToNorthThan); CentralThanToNorthThan.AddDirected(NorthernThanalan);
            Node CentralThanToWestThan = new Node("Central Thanalan > Western Thanalan", new Vector3(-406.028320f, -1.327996f, 98.055061f), -1); graph.AddNode(CentralThanToWestThan); CentralThanalan.AddDirected(CentralThanToWestThan); CentralThanToWestThan.AddDirected(WesternThanalan);
            // Southern Thanalan
            Node SouthThanToCentralThan = new Node("Southern Thanalan > Central Thanalan", new Vector3(-427.950775f, 12.862315f, -426.944122f), -1); graph.AddNode(SouthThanToCentralThan); SouthernThanalan.AddDirected(SouthThanToCentralThan); SouthThanToCentralThan.AddDirected(CentralThanalan);
            Node SouthThanToEastThan = new Node("Southern Thanalan > Eastern Thanalan", new Vector3(-28.638260f, 17.150595f, -767.939148f), -1); graph.AddNode(SouthThanToEastThan); SouthernThanalan.AddDirected(SouthThanToEastThan); SouthThanToEastThan.AddDirected(EasternThanalan);
            // Ul'dah Steps of Thal
            Node UldahThalToUldahNald = new Node("Ul'dah Steps of Thal > Ul'dah Steps of Nald", new Vector3(64.644173f, 8.000000f, -82.809769f), -1); graph.AddNode(UldahThalToUldahNald); UldahStepsOfThal.AddDirected(UldahThalToUldahNald); UldahThalToUldahNald.AddDirected(UldahStepsOfNald);
            Node UldahThalToHeartOfTheSworn = new Node("Ul'dah Steps of Thal > Heart of the Sworn", new Vector3(-123.8881f, 40f, 95.38416f), 1); graph.AddNode(UldahThalToHeartOfTheSworn); UldahStepsOfThal.AddDirected(UldahThalToHeartOfTheSworn); UldahThalToHeartOfTheSworn.AddDirected(HeartOfTheSworn);
            Node HeartOfTheSwornToUldahThal = new Node("Heart of the Sworn > Ul'dah Steps of Thal", new Vector3(0.01519775f, 0.9002075f, 10.14716f), 1); graph.AddNode(HeartOfTheSwornToUldahThal); HeartOfTheSworn.AddDirected(HeartOfTheSwornToUldahThal); HeartOfTheSwornToUldahThal.AddDirected(UldahStepsOfThal);
            Node UldahThalToCentralThan = new Node("Ul'dah Steps of Thal > Central Thanalan", new Vector3(163.689270f, 3.999968f, 43.879639f), -1); graph.AddNode(UldahThalToCentralThan); UldahStepsOfThal.AddDirected(UldahThalToCentralThan); UldahThalToCentralThan.AddDirected(CentralThanalan);
            // Ul'dah Steps of Nald
            Node UldahNaldToCentralThan = new Node("Ul'dah Steps of Nald > Central Thanalan", new Vector3(43.772938f, 3.999976f, -163.789337f), -1); graph.AddNode(UldahNaldToCentralThan); UldahStepsOfNald.AddDirected(UldahNaldToCentralThan); UldahNaldToCentralThan.AddDirected(CentralThanalan);
            Node UldahInnRoomToUldahNald = new Node("Ul'dah Inn Room > Ul'dah Steps of Nald", new Vector3(0.01519775f, 1.968323f, 8.132996f), 1); graph.AddNode(UldahInnRoomToUldahNald); UldahInnRoom.AddDirected(UldahInnRoomToUldahNald); UldahInnRoomToUldahNald.AddDirected(UldahStepsOfNald);
            Node UldahNaldToUldahAirShip = new Node("Ul'dah Steps of Nald > Ul'dah AirShip", new Vector3(-23.33112f, 10f, -43.44244f), 0); graph.AddNode(UldahNaldToUldahAirShip); UldahStepsOfNald.AddDirected(UldahNaldToUldahAirShip); UldahNaldToUldahAirShip.AddDirected(UldahAirShip);
            Node UldahNaldToWestThan = new Node("Ul'dah Steps of Nald > Western Thanalan", new Vector3(-180.163864f, 14.000000f, -14.053099f), -1); graph.AddNode(UldahNaldToWestThan); UldahStepsOfNald.AddDirected(UldahNaldToWestThan); UldahNaldToWestThan.AddDirected(WesternThanalan);
            Node UldahNaldToUldahThal = new Node("Ul'dah Steps of Nald > Ul'dah Steps of Thal", new Vector3(-120.054825f, 10.031486f, -8.766253f), -1); graph.AddNode(UldahNaldToUldahThal); UldahStepsOfNald.AddDirected(UldahNaldToUldahThal); UldahNaldToUldahThal.AddDirected(UldahStepsOfThal);

            // La Noscea

            // Outer La Noscea
            //areas.Add("180-139_West", new AreaInfo { XYZ = new Vector3(-320.6279f, 51.65852f, -75.99368f), Name = ">Outer La Noscea-->Upper La Noscea-", Communicationlocalindex = -1 });
            //areas.Add("180-139_East", new AreaInfo { XYZ = new Vector3(240.5355f, 54.22388f, -252.5956f), Name = ">Outer La Noscea-->Upper La Noscea-", Communicationlocalindex = -1 });
            // Upper La Noscea West
            //areas.Add("139_East-139_West", new AreaInfo { XYZ = new Vector3(221.7828f, -0.9591975f, 258.2541f), Name = "Upper La Noscea East-->Upper La Noscea West", Communicationlocalindex = 1 });
            //areas.Add("139_West-139_East", new AreaInfo { XYZ = new Vector3(-340.5905f, -1.024988f, 111.8383f), Name = "Upper La Noscea West-->Upper La Noscea East", Communicationlocalindex = 1 });
            // Upper La Noscea East
            //areas.Add("139_East-137_West", new AreaInfo { XYZ = new Vector3(719.070007f, 0.217405f, 214.217957f), Name = ">Upper La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
            //areas.Add("139_West-138", new AreaInfo { XYZ = new Vector3(-476.706177f, 1.921210f, 287.913330f), Name = "Upper La Noscea-->Western La Noscea", Communicationlocalindex = -1 });
            //areas.Add("139_West-180", new AreaInfo { XYZ = new Vector3(-344.8658f, 48.09458f, -17.46293f), Name = ">Upper La Noscea-->Outer La Noscea", Communicationlocalindex = -1 });
            //areas.Add("139_East-180", new AreaInfo { XYZ = new Vector3(286.4225f, 41.63181f, -201.1194f), Name = ">Upper La Noscea-->Outer La Noscea", Communicationlocalindex = -1 });
            // Western La Noscea
            //areas.Add("138-129", new AreaInfo { XYZ = new Vector3(318.314f, -36f, 351.376f), Name = ">Western La Noscea-->Limsa (Lower)", Communicationlocalindex = 1 });
            //areas.Add("138-135", new AreaInfo { XYZ = new Vector3(318.314f, -36f, 351.376f), Name = ">Western La Noscea-->Lower La Noscea", Communicationlocalindex = 2 });
            //areas.Add("138-134", new AreaInfo { XYZ = new Vector3(811.963623f, 49.586365f, 390.644775f), Name = ">Western La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
            //areas.Add("138-139_West", new AreaInfo { XYZ = new Vector3(410.657715f, 30.619648f, -10.786478f), Name = ">Western La Noscea-->Upper La Noscea", Communicationlocalindex = -1 });
            // Eastern La Noscea West
            //areas.Add("137_West-134", new AreaInfo { XYZ = new Vector3(-113.323311f, 70.324112f, 47.165649f), Name = "Eastern La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
            //areas.Add("137_West-137_East", new AreaInfo { XYZ = new Vector3(21.74548f, 34.07887f, 223.4946f), Name = "Eastern La Noscea Costa Del Sol-->Eastern La Noscea Wineport", Communicationlocalindex = 1 });
            //areas.Add("137_West-139_East", new AreaInfo { XYZ = new Vector3(78.965446f, 80.393074f, -119.879181f), Name = "Eastern La Noscea-->Upper La Noscea", Communicationlocalindex = -1 });
            // Eastern La Noscea East
            //areas.Add("137_East-129", new AreaInfo { XYZ = new Vector3(606.901f, 11.6f, 391.991f), Name = "Eastern La Noscea-->Limsa (Lower)", Communicationlocalindex = 1 });
            //areas.Add("137_East-135", new AreaInfo { XYZ = new Vector3(246.811844f, 56.341099f, 837.507141f), Name = "Eastern La Noscea-->Lower La Noscea", Communicationlocalindex = -1 });
            //areas.Add("137_East-137_West", new AreaInfo { XYZ = new Vector3(345.3907f, 32.77044f, 91.39402f), Name = "Eastern La Noscea Costa Del Sol-->Eastern La Noscea Wineport", Communicationlocalindex = 1 });
            // Middle La Noscea
            //areas.Add("134-129", new AreaInfo { XYZ = new Vector3(-43.422066f, 35.445602f, 153.802917f), Name = "Middle La Noscea-->Limsa (Lower)", Communicationlocalindex = -1 });
            //areas.Add("134-135", new AreaInfo { XYZ = new Vector3(203.290405f, 65.182816f, 285.331512f), Name = "Middle La Noscea-->Lower La Noscea", Communicationlocalindex = -1 });
            //areas.Add("134-137_West", new AreaInfo { XYZ = new Vector3(-163.673187f, 35.884563f, -734.864807f), Name = "Middle La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
            //areas.Add("134-138", new AreaInfo { XYZ = new Vector3(-375.221436f, 33.130100f, -603.032593f), Name = "Middle La Noscea-->Western La Noscea", Communicationlocalindex = -1 });
            // Lower La Noscea
            //areas.Add("135-128", new AreaInfo { XYZ = new Vector3(-52.436810f, 75.830246f, 116.130196f), Name = "Lower La Noscea-->Limsa (Upper)", Communicationlocalindex = -1 });
            //areas.Add("135-134", new AreaInfo { XYZ = new Vector3(230.518661f, 74.490341f, -342.391663f), Name = "Lower La Noscea-->Middle La Noscea", Communicationlocalindex = -1 });
            //areas.Add("135-137_East", new AreaInfo { XYZ = new Vector3(694.988586f, 79.927017f, -387.720428f), Name = "Lower La Noscea-->Eastern La Noscea", Communicationlocalindex = -1 });
            //areas.Add("135-339", new AreaInfo { XYZ = new Vector3(598.555847f, 61.519623f, -108.400681f), Name = "Lower La Noscea-->???", Communicationlocalindex = -1 });
            // Limsa Lominsa Lower Decks
            Node LimsaLowerToLimsaUpper = new Node("Limsa Lominsa Lower Decks > Limsa Lominsa Upper Decks", new Vector3(-83.549187f, 17.999935f, -25.898380f), -1); graph.AddNode(LimsaLowerToLimsaUpper); LimsaLominsaLowerDecks.AddDirected(LimsaLowerToLimsaUpper); LimsaLowerToLimsaUpper.AddDirected(LimsaLominsaUpperDecks);
            Node LimsaLowerToMiddleLaNoscea = new Node("Limsa Lominsa Lower Decks > Middle La Noscea", new Vector3(63.212173f, 19.999994f, 0.221235f), -1); graph.AddNode(LimsaLowerToMiddleLaNoscea); LimsaLominsaLowerDecks.AddDirected(LimsaLowerToMiddleLaNoscea); LimsaLowerToMiddleLaNoscea.AddDirected(MiddleLaNoscea);
            Node LimsaLowerToWestThan = new Node("Limsa Lominsa Lower Decks > Western Thanalan", new Vector3(-360.9217f, 8.000013f, 38.92566f), 0); graph.AddNode(LimsaLowerToWestThan); LimsaLominsaLowerDecks.AddDirected(LimsaLowerToWestThan); LimsaLowerToWestThan.AddDirected(WesternThanalan);
            Node LimsaLowerToWestLaNoscea = new Node("Limsa Lominsa Lower Decks > Western La Noscea", new Vector3(-191.834f, 1f, 210.829f), 1); graph.AddNode(LimsaLowerToWestLaNoscea); LimsaLominsaLowerDecks.AddDirected(LimsaLowerToWestLaNoscea); LimsaLowerToWestLaNoscea.AddDirected(WesternLaNoscea);
            Node LimsaLowerToEastLaNosceaE = new Node("Limsa Lominsa Lower Decks > Eastern La Noscea East", new Vector3(-190.834f, 1f, 210.829f), 2); graph.AddNode(LimsaLowerToEastLaNosceaE); LimsaLominsaLowerDecks.AddDirected(LimsaLowerToEastLaNosceaE); LimsaLowerToEastLaNosceaE.AddDirected(EasternLaNosceaEast);
            Node LimsaLowerToLimsaAirShip = new Node("Limsa Lominsa Lower Decks > Limsa Lominsa AirShip", new Vector3(9.781006f, 20.99925f, 15.09113f), 0); graph.AddNode(LimsaLowerToLimsaAirShip); LimsaLominsaLowerDecks.AddDirected(LimsaLowerToLimsaAirShip); LimsaLowerToLimsaAirShip.AddDirected(LimsaAirShip);
            // Limsa Lominsa Upper Decks
            Node LimsaAirShipToUldahAirShip = new Node("Limsa Lominsa AirShip > Ul'dah Steps of Thal", new Vector3(-25.92511f, 91.99995f, -3.677429f), 0); graph.AddNode(LimsaAirShipToUldahAirShip); LimsaAirShip.AddDirected(LimsaAirShipToUldahAirShip); LimsaAirShipToUldahAirShip.AddDirected(UldahAirShip);
            Node LimsaAirShipToNewGridania = new Node("Limsa Lominsa AirShip > New Gridania", new Vector3(-25.92511f, 91.99995f, -3.677429f), 1); graph.AddNode(LimsaAirShipToNewGridania); LimsaAirShip.AddDirected(LimsaAirShipToNewGridania); LimsaAirShipToNewGridania.AddDirected(NewGridania);
            Node LimsaUpperToLimsaLower = new Node("Limsa Lominsa Upper Decks > Limsa Lominsa Lower Decks", new Vector3(-5.868670f, 43.095970f, -27.703053f), -1); graph.AddNode(LimsaUpperToLimsaLower); LimsaLominsaUpperDecks.AddDirected(LimsaUpperToLimsaLower); LimsaUpperToLimsaLower.AddDirected(LimsaLominsaLowerDecks);
            Node LimsaUpperToLowerLaNoscea = new Node("Limsa Lominsa Upper Decks > Lower La Noscea", new Vector3(24.692057f, 44.499928f, 180.197906f), -1); graph.AddNode(LimsaUpperToLowerLaNoscea); LimsaLominsaUpperDecks.AddDirected(LimsaUpperToLowerLaNoscea); LimsaUpperToLowerLaNoscea.AddDirected(LowerLaNoscea);
            Node LimsaAirShipToLimsaUpper = new Node("Limsa Lominsa Airship Landing > Limsa Lominsa Upper Decks", new Vector3(-7.248047f, 91.49999f, -16.12885f), 0); graph.AddNode(LimsaAirShipToLimsaUpper); LimsaAirShip.AddDirected(LimsaAirShipToLimsaUpper); LimsaAirShipToLimsaUpper.AddDirected(LimsaLominsaUpperDecks);
            Node LimsaAirShipToLimsaLower = new Node("Limsa Lominsa Airship Landing > Limsa Lominsa Lower Decks", new Vector3(-7.248047f, 91.49999f, -16.12885f), 1); graph.AddNode(LimsaAirShipToLimsaLower); LimsaAirShip.AddDirected(LimsaAirShipToLimsaLower); LimsaAirShipToLimsaLower.AddDirected(LimsaLominsaLowerDecks);

            // unknown

            //areas.Add("130-178", new AreaInfo { XYZ = new Vector3(29.635f, 7.000f, -80.346f), Name = "ul'dah-Ul dah -Ul dah - Inn", Communicationlocalindex = 1 });
            //areas.Add("140-341", new AreaInfo { XYZ = new Vector3(316.839722f, 67.180557f, 236.260666f), Name = "Western Thanalan-Cutterscry??", Communicationlocalindex = -1 });
            //areas.Add("156-351", new AreaInfo { XYZ = new Vector3(21.124185562134f, 21.252725601196f, -631.37310791016f), Name = "Mor Dhona-Ini ??? ", Communicationlocalindex = -1 });
            //areas.Add("341-140", new AreaInfo { XYZ = new Vector3(-10.554447f, -11.076664f, -197.729034f), Name = "Cutterscry??-Western Thanalan", Communicationlocalindex = -1 });
            //areas.Add("178-130", new AreaInfo { XYZ = new Vector3(-0.213f, -2.82f, 6.4577f), Name = "Ul dah - Inn- ul'dah-Ul dah", Communicationlocalindex = -1 });
            //areas.Add("128-177", new AreaInfo { XYZ = new Vector3(13.245f, 39.999f, 11.8289f), Name = "Limsa (Upper)-->Gridania - Inn", Communicationlocalindex = 1 });

            //Girandia Airplane

            //areas.Add("132-130-2", new AreaInfo { XYZ = new Vector3(29.85914f, -19f, 103.573f), Name = "Girandia-->ul'dah lift", Communicationlocalindex = 1 });
            //areas.Add("132-128-2", new AreaInfo { XYZ = new Vector3(29.1025f, -19.000f, 102.408f), Name = "Girandia-Limsa (Upper)", Communicationlocalindex = 2 });
            //areas.Add("132-179", new AreaInfo { XYZ = new Vector3(25.977f, -8f, 100.151f), Name = "Girandia-Limsa Lominsa - Inn", Communicationlocalindex = 1 });
            //areas.Add("132-204", new AreaInfo { XYZ = new Vector3(232f, 1.90f, 45.5f), Name = "Girandia-Limsa Lominsa - Command", Communicationlocalindex = -1 });
            //areas.Add("204-132", new AreaInfo { XYZ = new Vector3(0f, 1f, 9.8f), Name = "Limsa Lominsa Command- New Gridania", Communicationlocalindex = -1 });
            //areas.Add("179-132", new AreaInfo { XYZ = new Vector3(-0.081f, 9.685f, 6.3329f), Name = "Limsa Lominsa Command- Limsa Lominsa - Inn", Communicationlocalindex = -1 });
            //areas.Add("351-156", new AreaInfo { XYZ = new Vector3(0.044669650495052f, 2.053418636322f, 27.388998031616f), Name = "???- Limsa Lominsa - Inn--Mor Dhona", Communicationlocalindex = -1 });

            //ul dah lift unten nach komen

            //areas.Add("130-130-1", new AreaInfo { XYZ = new Vector3(-20.59343f, 10f, -44.79702f), Name = "ul'dah- ul dah list lower", Communicationlocalindex = 1 });

            // lift oben nach Girandia

            //areas.Add("130-1-132", new AreaInfo { XYZ = new Vector3(-22.364f, 83.199f, -4.82f), Name = "ul'dah-Ul dah - Girandia", Communicationlocalindex = 1 });
            //areas.Add("130-1-128-3", new AreaInfo { XYZ = new Vector3(-22.364f, 83.199f, -4.82f), Name = "ul'dah lift -Limsa (Upper)", Communicationlocalindex = 2 });

            //Lift oben Koordinaten und fahre nachunten

            //areas.Add("130-2-130", new AreaInfo { XYZ = new Vector3(-24.62714f, 81.8f, -29.91334f), Name = "Girandia-->ul'dah lift", Communicationlocalindex = 1 });
            //areas.Add("130-3-130-2", new AreaInfo { XYZ = new Vector3(-24.62714f, 81.8f, -29.91334f), Name = "Girandia-->ul'dah lift", Communicationlocalindex = -1 });
            //areas.Add("128-128-1", new AreaInfo { XYZ = new Vector3(8.763986f, 40.0003f, 15.04217f), Name = "Limsa (Upper)-->Limsa", Communicationlocalindex = 1 });
            //areas.Add("128-1-132", new AreaInfo { XYZ = new Vector3(-23.511f, 91.99f, -3.719f), Name = "Limsa (Upper)-->New Gridania", Communicationlocalindex = 2 });
            //areas.Add("128-1-130-3", new AreaInfo { XYZ = new Vector3(-23.511f, 91.99f, -3.719f), Name = "Limsa (Upper)-->ul'dah lift", Communicationlocalindex = 1 });
            //areas.Add("128-2-128", new AreaInfo { XYZ = new Vector3(-8.450267f, 91.5f, -15.72492f), Name = "Limsa (Upper)-->ul'dah lift", Communicationlocalindex = 1 });
            //areas.Add("128-3-128-2", new AreaInfo { XYZ = new Vector3(-9.610059f, 91.49965f, -16.58086f), Name = "Limsa (Upper)-->Limsa", Communicationlocalindex = -1 });

            //areas.Add("177-128", new AreaInfo { XYZ = new Vector3(-0.113f, 0.007f, 7.086f), Name = "Gridania - Inn-->Limsa (Upper)", Communicationlocalindex = -1 });
            //areas.Add("339-135", new AreaInfo { XYZ = new Vector3(-9.653202f, 48.346123f, -169.488068f), Name = ">???-->Lower La Noscea-", Communicationlocalindex = -1 });
        }

        #endregion
    }
}