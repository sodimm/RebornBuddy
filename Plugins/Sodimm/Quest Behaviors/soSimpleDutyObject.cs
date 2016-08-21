using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using System;
using System.Collections.Generic;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags {
    [XmlElement("SoSimpleDutyObject")]
    class SoSimpleDutyObjectTag : SoSimpleDutyTag {
        private Dictionary<string, bool> Checkpoints = new Dictionary<string, bool>();

        public void Reset(object sender, EventArgs e) {
            Checkpoints.Clear();
        }

        public bool HasCheckPointReached(object cp) {
            string checkpoint = cp.ToString();
            if (!Checkpoints.ContainsKey(checkpoint)) Checkpoints.Add(checkpoint, false);
            return Checkpoints[checkpoint];
        }

        public void CheckPointReached(object cp) {
            string checkpoint = cp.ToString();
            if (!Checkpoints.ContainsKey(checkpoint)) Checkpoints.Add(checkpoint, true);
            else Checkpoints[checkpoint] = true;
        }

        protected override void OnTagStart() {
            GameEvents.OnPlayerDied += Reset;
            base.OnTagStart();
        }

        protected override void OnResetCachedDone() {
            Reset(null, null);
            base.OnResetCachedDone();
        }

        protected Composite Q65995()
        {
            // (65995) A Realm Reborn - Pincer Maneuver
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 65995 && !Core.Player.InCombat && GameObjectManager.GetObjectByNPCId(2002521) != null && GameObjectManager.GetObjectByNPCId(2002521).IsVisible,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002521).Location) <= 3,
                            new Action( r => {
                                GameObjectManager.GetObjectByNPCId(2002521).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002521).Location, 3)
                    )
                ),
                base.CreateBehavior()
            );
        }

        protected Composite Q65886()
        {
            // (65886) A Realm Reborn - The Threat Of Superiority
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 65886 && GameObjectManager.GetObjectByNPCId(2001471) != null && GameObjectManager.GetObjectByNPCId(2001471).IsVisible && !Core.Player.InCombat,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2001471).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2001471).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2001471).Location, 3)
                    )
                ),
                base.CreateBehavior()
            );
        }

        //        ),
        //        new Decorator(ret => QuestId == 66633 && GameObjectManager.GetObjectByNPCId(2002522) != null && GameObjectManager.GetObjectByNPCId(2002522).IsVisible,
        //            new PrioritySelector(
        //                new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002522).Location) <= 3,
        //                    new Action(r =>
        //                    {
        //                        GameObjectManager.GetObjectByNPCId(2002522).Interact();
        //                    })
        //                ),
        //                CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002522).Location, 3)
        //            )
        //        ),
        //        new Decorator(ret => QuestId == 66448 && GameObjectManager.GetObjectByNPCId(2002279) != null && GameObjectManager.GetObjectByNPCId(2002279).IsVisible,
        //            new PrioritySelector(
        //                new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002279).Location) <= 3,
        //                    new Action(r =>
        //                    {
        //                        GameObjectManager.GetObjectByNPCId(2002279).Interact();
        //                    })
        //                ),
        //                CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002279).Location, 3)
        //            )
        //        ),
        //        new Decorator(ret => QuestId == 66057 && ((GameObjectManager.GetObjectByNPCId(2002428) != null && GameObjectManager.GetObjectByNPCId(2002428).IsVisible) || (GameObjectManager.GetObjectByNPCId(2002427) != null && GameObjectManager.GetObjectByNPCId(2002427).IsVisible)),
        //            new PrioritySelector(
        //new Decorator(ret => GameObjectManager.GetObjectByNPCId(2002428) != null && GameObjectManager.GetObjectByNPCId(2002428).IsVisible,
        //new PrioritySelector(
        //new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002428).Location) <= 3,
        //new Action(r =>
        //{
        //GameObjectManager.GetObjectByNPCId(2002428).Interact();
        //})
        //),
        //CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002428).Location, 3)
        //)
        //),
		//				new Decorator(ret => GameObjectManager.GetObjectByNPCId(2002427) != null && GameObjectManager.GetObjectByNPCId(2002427).IsVisible,
		//					new PrioritySelector(
		//						new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2002427).Location) <= 3,
		//							new Action(r =>
		//							{
		//								GameObjectManager.GetObjectByNPCId(2002427).Interact();
		//							})
		//						),
		//						CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2002427).Location, 3)
		//					)
		//				)
		//			)
         //       ),
		//		new Decorator(ret => QuestId == 66057 && ((GameObjectManager.GetObjectByNPCId(2094) != null && GameObjectManager.GetObjectByNPCId(2094).IsVisible) || (GameObjectManager.GetObjectByNPCId(1813) != null && GameObjectManager.GetObjectByNPCId(1813).IsVisible)),
        //            new PrioritySelector(
		//				new Decorator(ret => GameObjectManager.GetObjectByNPCId(2094) != null && GameObjectManager.GetObjectByNPCId(2094).IsVisible,
		//					CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2094).Location, 3)
		//				),
		//				new Decorator(ret => GameObjectManager.GetObjectByNPCId(1813) != null && GameObjectManager.GetObjectByNPCId(1813).IsVisible,
		//					CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(1813).Location, 3)
		//				)
		//			)
        //        ),
		//		new Decorator(ret => QuestId == 66540 && Vector3.Distance(Core.Player.Location, ObjectXYZ) < InteractDistance,
        //            new PrioritySelector(
		//				new Decorator(ret => MovementManager.IsMoving,
        //                    new Action(r =>
        //                    {
        //                        MovementManager.MoveForwardStop();
        //                    })
        //                ),
        //                new Decorator(ret => !didthething,
        //                    new Action(r =>
        //                    {
        //                        var targetnpc = ff14bot.Managers.GameObjectManager.GetObjectByNPCId((uint)InteractNpcId);
		//							foreach (ff14bot.Managers.BagSlot slot in ff14bot.Managers.InventoryManager.FilledSlots)
		//						{
		//							if (slot.RawItemId == 2000771)
		//							{
		//								slot.UseItem(targetnpc);
		//								didthething = true;
		//							}
		//						}
        //                   })
        //              )
        //            )
        //        ),
		//		new Decorator(ret => QuestId == 66638 && GameObjectManager.GetObjectByNPCId(1650) != null && !GameObjectManager.GetObjectByNPCId(1650).CanAttack,
        //            new PrioritySelector(
		//				new Decorator(ret => GameObjectManager.GetObjectByNPCId(1650) != null && GameObjectManager.GetObjectByNPCId(1650).IsVisible,
		//					new PrioritySelector(
		//						new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(1650).Location) <= 3,
		//							new Action(r =>
		//							{
		//								ff14bot.Managers.Actionmanager.DoAction(190, GameObjectManager.GetObjectByNPCId(1650));
		//							})
		//						),
		//						CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(1650).Location, 3)
		//					)
		//				)
		//			)
         //       ),

        protected Composite Q67124() {
            // (67124) Heavensward - At the end of Our Hope
            Vector3 c1 = new Vector3(175.0911f, 130.9083f, -430.1f);
            Vector3 c2 = new Vector3(362.1796f, 137.2033f, -383.6978f);
            Vector3 c3 = new Vector3(444.7922f, 160.8083f, -566.6742f);

            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 67124 && GameObjectManager.GetObjectByNPCId(2005850) != null && GameObjectManager.GetObjectByNPCId(2005850).IsVisible && !Core.Player.InCombat,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005850).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2005850).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005850).Location, 3)
                    )
                ),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(1) && Core.Me.Location.Distance(c1) < 5, new Action(a => { CheckPointReached(1); })),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(1), CommonBehaviors.MoveAndStop(ret => c1, 3)),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(2) && Core.Me.Location.Distance(c2) < 5, new Action(a => { CheckPointReached(2); })),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(2), CommonBehaviors.MoveAndStop(ret => c2, 3)),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(3) && Core.Me.Location.Distance(c3) < 3, new Action(a => { CheckPointReached(3); })),
                new Decorator(ret => DutyManager.InInstance && QuestId == 67124 && !HasCheckPointReached(3), CommonBehaviors.MoveAndStop(ret => c3, 3)),
                base.CreateBehavior()
            );
        }

        protected Composite Q67137() {
            // (67137) Heavensward -  Keeping the Flame Alive
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 67137 && GameObjectManager.GetObjectByNPCId(2005546) != null && GameObjectManager.GetObjectByNPCId(2005546).IsVisible && !Core.Player.InCombat,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005546).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2005546).Interact();
                                CheckPointReached(1);
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005546).Location, 3)
                    )
                ),
                new Decorator(ret => QuestId == 67137 && GameObjectManager.GetObjectByNPCId(2006332) != null && GameObjectManager.GetObjectByNPCId(2006332).IsVisible && !Core.Player.InCombat && HasCheckPointReached(1),
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2006332).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2006332).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2006332).Location, 3)
                    )
                ),
                base.CreateBehavior()
            );
        }

        protected Composite Q67131() {
            // (67131) Heavensward - A Series of Unfortunate Events
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                new Decorator(ret => QuestId == 67131 && GameObjectManager.GetObjectByNPCId(4129) != null && GameObjectManager.GetObjectByNPCId(4129).IsVisible && Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(4129).Location) > 20,
                    CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(4129).Location, 15)
                ),
                new Decorator(ret => QuestId == 67131 && GameObjectManager.GetObjectByNPCId(2005710) != null && GameObjectManager.GetObjectByNPCId(2005710).IsVisible && !Core.Player.InCombat,
                    new PrioritySelector(
                        new Decorator(ret => Core.Me.Location.Distance(GameObjectManager.GetObjectByNPCId(2005710).Location) <= 3,
                            new Action(r => {
                                GameObjectManager.GetObjectByNPCId(2005710).Interact();
                            })
                        ),
                        CommonBehaviors.MoveAndStop(ret => GameObjectManager.GetObjectByNPCId(2005710).Location, 3)
                    )
                ),
                base.CreateBehavior()
            );
        }

        protected override Composite CreateBehavior()
        {
            if (QuestId == 65995) return Q65995();
            if (QuestId == 65886) return Q65886();
            if (QuestId == 67124) return Q67124();
            if (QuestId == 67131) return Q67131();
            if (QuestId == 67137) return Q67137();

            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(ret => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),
                base.CreateBehavior()
            );
        }
    }
}