//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("BuyItemPlus")]
    public class BuyItemPlus : ProfileBehavior
    {

        [XmlAttribute("ItemIds")]
        [XmlAttribute("ItemId")]
        public int[] ItemIds { get; set; }

        [XmlAttribute("ItemCounts")]
        [XmlAttribute("ItemCount")]
        public int[] ItemCounts { get; set; }


        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }

        [XmlAttribute("InteractDistance")]
        [DefaultValue(5f)]
        public float InteractDistance { get; set; }

        [DefaultValue(-1)]
        [XmlAttribute("DialogOption1")]
        public int DialogOption1 { get; set; }

        [DefaultValue(-1)]
        [XmlAttribute("DialogOption2")]
        public int DialogOption2 { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ
        {
            get { return Position; }
            set { Position = value; }
        }
        public Vector3 Position = Vector3.Zero;



        protected bool IsDoneOverride;
        private GameObject _cachedObject;
        private bool _missionBoardAccepted;

        private bool _dialogOption1Used;


        #region Overrides of ProfileBehavior

        public override bool IsDone
        {
            get
            {
                if (IsQuestComplete)
                    return true;
                if (IsStepComplete)
                    return true;


                if (DoneTalking)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion

        private bool DoneTalking;

        private string vendorName;



        private Queue<int> itemQueue;
        private Queue<int> itemcountQueue;

        private string ItemNames;


        protected override void OnStart()
        {
            _dialogOption1Used = false;
            vendorName = DataManager.GetLocalizedNPCName(NpcId);

            if (ItemCounts.Length != ItemIds.Length)
                throw new ArgumentException("ItemIds and ItemCounts must be the same length");


            itemQueue = new Queue<int>(ItemIds);
            itemcountQueue = new Queue<int>(ItemCounts);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ItemIds.Length; i++)
            {
                var item = DataManager.GetItem((uint)ItemIds[i]);

                if (i == ItemIds.Length - 1)
                {
                    sb.Append($"{item.CurrentLocaleName}");
                }
                else
                {
                    sb.Append($"{item.CurrentLocaleName},");
                }


            }
            ItemNames = sb.ToString();

        }

        protected override void OnResetCachedDone()
        {
            DoneTalking = false;
            dialogwasopen = false;
        }






        public override string StatusText { get { return "Talking to " + vendorName; } }


        public GameObject NPC
        {
            get
            {
                return GameObjectManager.GetObjectByNPCId((uint)NpcId);
            }
        }

        private async Task<bool> BuyItems()
        {

            var currentItems = Shop.Items;

            dialogwasopen = true;
            foreach (var itemid in ItemIds)
            {
                uint totalPurchased = 0;
                int loop = 1;
                var amountwanted = itemcountQueue.Dequeue();
                if (currentItems.Any(r => r.ItemId == itemid))
                {
                    var currentItem = currentItems.First(r => r.ItemId == itemid);

                Purhcase:
                    var purchased = Shop.Purchase((uint)itemid, (uint)amountwanted);
                    
                    await Coroutine.Wait(2000, () => SelectYesno.IsOpen);

                    if (!SelectYesno.IsOpen)
                        goto Purhcase;

                    await Coroutine.Sleep(500);

                    if (purchased != amountwanted)
                    {
                        Log("Purchasing {0} {1} from {2} for {3} gil ({4})", purchased, currentItem.product_name, vendorName, purchased * currentItem.Cost,loop);
                    }
                    else
                    {
                        Log(@"Purchasing {0} {1} from {2} for {3} gil", purchased, currentItem.product_name, vendorName, purchased * currentItem.Cost);
                    }
                    
                    SelectYesno.ClickYes();
                    await Coroutine.Sleep(500);

                    totalPurchased += purchased;

                    if (totalPurchased < amountwanted)
                    {
                        loop++;
                        goto Purhcase;
                    }
                        

                }
                else
                {
                    LogError(" {0} does not have itemid:{1}", vendorName,itemid);
                }

                


            }
            await Coroutine.Sleep(500);
            Shop.Close();
            await Coroutine.Sleep(500);
            return true;
        }

        private bool dialogwasopen;
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                ctx => NPC,

                new Decorator(r => SelectIconString.IsOpen, new Action(r =>
                {

                    if (DialogOption1 == -1)
                    {
                        TreeRoot.Stop("No DialogOption1 supplied, but found dialog window.");
                        throw new ArgumentException("No DialogOption1 supplied, but found dialog window.");
                        return RunStatus.Failure;
                    }

                    SelectIconString.ClickSlot((uint)DialogOption1);
                    return RunStatus.Success;
                })),

                new Decorator(r => SelectString.IsOpen, new Action(r =>
                {

                    if (DialogOption2 == -1)
                    {
                        TreeRoot.Stop("No DialogOption2 supplied, but found dialog window.");
                        throw new ArgumentException("No DialogOption2 supplied, but found dialog window.");
                        return RunStatus.Failure;
                    }

                    SelectString.ClickSlot((uint)DialogOption2);
                    return RunStatus.Success;
                })),

                new Decorator(r => dialogwasopen && !Core.Player.HasTarget, new Action(r => { DoneTalking = true; return RunStatus.Success; })),
                new Decorator(r => Talk.DialogOpen, new Action(r => { Talk.Next(); return RunStatus.Success; })),
                //new Action(r => { dialogwasopen = true; return RunStatus.Success; })
                new Decorator(r => Shop.Open, new ActionRunCoroutine(ctx=>BuyItems())),


                CommonBehaviors.MoveAndStop(ret => XYZ, ret => InteractDistance, true, ret => $"[{GetType().Name}] Moving to {XYZ} so we can buy {ItemNames} from {vendorName}"),
                new Decorator(ret => NPC == null, new Sequence(new SucceedLogger(r => $"Waiting at {Core.Player.Location} for {vendorName} to spawn so we can purchase {ItemNames}"), new WaitContinue(5, ret => NPC != null, new Action(ret => RunStatus.Failure)))),
                new Action(ret => NPC.Interact())
                );
        }
    }
}
