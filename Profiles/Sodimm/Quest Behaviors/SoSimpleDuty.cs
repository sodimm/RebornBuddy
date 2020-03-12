using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing;
using ff14bot.RemoteWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoSimpleDuty")]
    public class SoSimpleDuty : SimpleDutyTag
    {
        protected SoSimpleDuty()
        {
            Interactobjects = new List<InteractObject>();
            Checkpoints = new List<CheckPoint>();
        }

        [XmlAttribute("ItemIds")]
        [XmlAttribute("ItemId")]
        public int[] ItemIds { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("UseItem")]
        public bool UseItem { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("InteractInCombat")]
        public bool InteractInCombat { get; set; }

        [XmlElement("InteractObjects")]
        public List<InteractObject> Interactobjects { get; set; }

        [XmlElement("CheckPoints")]
        public List<CheckPoint> Checkpoints { get; set; }

        private string ItemNames;
        private Composite _combatInteractLogic;
        private HashSet<uint> interactNpcIds;
        private HashSet<BagSlot> usedSlots;

        protected override void OnStart()
        {
            interactNpcIds = new HashSet<uint>();
            usedSlots = new HashSet<BagSlot>();

            if (Interactobjects != null)
            {
                Log($"{Interactobjects.Count} InteractObjects.");

                if (Interactobjects.Count != 0)
                {
                    foreach (var obj in Interactobjects)
                    {
                        interactNpcIds.Add((uint)obj.NpcId);
                    }
                }
            }

            if (Checkpoints != null)
            {
                Log($"{Checkpoints.Count} CheckPoints.");
            }

            if (UseItem)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < ItemIds.Length; i++)
                {
                    var item = DataManager.GetItem((uint)ItemIds[i]);

                    if (i == ItemIds.Length - 1)
                    {
                        sb.Append($"{item.CurrentLocaleName}");
                    }
                }
                ItemNames = sb.ToString();
            }

            if (InteractInCombat)
            {
                _combatInteractLogic = new ActionRunCoroutine(cr => CombatInteract());
                Log("Injecting InCombat Interact Logic.");
                TreeHooks.Instance.InsertHook("TreeStart", 0, _combatInteractLogic);
            }

            GameEvents.OnPlayerDied += OnPlayerDeathEvent;

            base.OnStart();
        }

        private bool doneUseItem;
        private bool HasInteractObjects => Interactobjects.Count != 0;
        private bool HasCheckpoints => Checkpoints.Count != 0;
        private Vector3 CurrentCheckpoint => HasCheckpoints ? Checkpoints.First().XYZ : Vector3.Zero;
        private GameObject InteractableTarget => GameObjectManager.GetObjectsOfType<GameObject>(true, false).Where(obj => obj.IsVisible && obj.IsTargetable && interactNpcIds.Contains(obj.NpcId)).FirstOrDefault();
        //private GameObject UseItemTarget => GameObjectManager.GetObjectByNPCId(InteractNpcId);
        //private BagSlot Item => UseItem ? InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemIds.FirstOrDefault()) : null;

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene,
                    new ActionAlwaysSucceed()
                ),
                new Decorator(r => Talk.DialogOpen && SelectYesno.IsOpen,
                    new Action(r =>
                    {
                        SelectYesno.ClickYes();
                    })
                ),
                new Decorator(r => Request.IsOpen,
                    new Action(r =>
                    {
                        var items = InventoryManager.FilledInventoryAndArmory.ToArray();
                        for (int i = 0; i < ItemIds.Length; i++)
                        {
                            BagSlot item;
                            item = items.FirstOrDefault(z => z.RawItemId == ItemIds[i] && !usedSlots.Contains(z));

                            if (item == null)
                            {
                                LogError($"We don't have any items with an id of {ItemIds[i]}.");
                            }
                            else
                            {
                                item.Handover();
                                usedSlots.Add(item);
                            }
                        }

                        usedSlots.Clear();
                        Request.HandOver();
                    })
                ),
                new Decorator(r => Core.Player.HasTarget && UseItem && !doneUseItem,
                    new Action(r =>
                    {
                        var targetNpc = GameObjectManager.GetObjectByNPCId(InteractNpcId);
                        foreach (BagSlot slot in InventoryManager.FilledSlots)
                        {
                            if (slot.RawItemId == ItemIds.FirstOrDefault())
                            {
                                Log($"Using {slot.EnglishName} on {targetNpc.EnglishName}.");
                                slot.UseItem(targetNpc);
                            }
                        }

                        if (Core.Player.IsCasting)
                        {
                            doneUseItem = true;
                        }

                        if (SelectYesno.IsOpen)
                        {
                            doneUseItem = true;
                        }
                    })
                ),
                new Decorator(ret => HasInteractObjects && DutyManager.InInstance && !Core.Player.InCombat && InteractableTarget != null,
                    new PrioritySelector(
                        new Decorator(ret => Core.Player.Location.Distance(InteractableTarget.Location) <= 5,
                            new Action(r =>
                            {
                                InteractableTarget.Interact();
                            })
                        ),
                        new Decorator(ret => Core.Player.Location.Distance(InteractableTarget.Location) > 5,
                            CommonBehaviors.MoveAndStop(ret => InteractableTarget.Location, 3)
                        ),
                        new ActionAlwaysSucceed()
                    )
                ),
                new Decorator(ret => HasCheckpoints && DutyManager.InInstance,
                    new PrioritySelector(
                        new Decorator(ret => Core.Player.Location.Distance(CurrentCheckpoint) < 5,
                            new Action(r =>
                            {
                                Checkpoints.Remove(Checkpoints.First());
                            })
                        ),
                        new Decorator(ret => Core.Player.Location.Distance(CurrentCheckpoint) > 5,
                            CommonBehaviors.MoveAndStop(ret => CurrentCheckpoint, 3)
                        )
                    )
                ),

                base.CreateBehavior()
            );
        }

        private async Task<bool> CombatInteract()
        {
            if (HasInteractObjects && InteractableTarget != null)
            {
                if (Core.Player.Distance(InteractableTarget.Location) > 5)
                {
                    return await CommonTasks.MoveAndStop(new MoveToParameters(InteractableTarget.Location), 3);
                }

                if (Core.Player.Distance(InteractableTarget.Location) < 5)
                {
                    InteractableTarget.Interact();
                    await Coroutine.Wait(10000, () => !Core.Player.IsCasting);
                }

                await Coroutine.Yield();
            }

            return false;
        }

        protected override void OnDone()
        {
            if (_combatInteractLogic != null)
            {
                Log("Removing Combat Interact Logic.");
                TreeHooks.Instance.RemoveHook("TreeStart", _combatInteractLogic);
            }

            doneUseItem = false;
            GameEvents.OnPlayerDied -= OnPlayerDeathEvent;
            base.OnDone();
        }

        public void OnPlayerDeathEvent(object sender, EventArgs e)
        {
            doneUseItem = false;
        }

        protected override void OnResetCachedDone()
        {
            doneUseItem = false;
            base.OnResetCachedDone();
        }
    }
}

namespace ff14bot.NeoProfiles
{
    [XmlElement("InteractObject")]
    public class InteractObject
    {
        public InteractObject()
        {
        }

        public string Name { get; set; }

        [XmlAttribute("Name", true)]
        public string ObjectName { get; set; }

        [XmlAttribute("NpcId", true)]
        public int NpcId { get; set; }
    }

    [XmlElement("CheckPoint")]
    public class CheckPoint
    {
        public CheckPoint()
        {
        }

        public string Name { get; set; }

        [XmlAttribute("XYZ", true)]
        public Vector3 XYZ { get; set; }
    }
}