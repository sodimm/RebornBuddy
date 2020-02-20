using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using System;
using System.ComponentModel;
using System.Linq;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("SoUseSpell")]
    public class SoUseSpellTag : ProfileBehavior
    {
        protected SoUseSpellTag()
        {
            Hotspots = new IndexedList<HotSpot>();
        }

        public override bool HighPriority
        { 
            get
            {
                return true;
            } 
        }

        private SpellData Spell
        { 
            get
            {
                return DataManager.GetSpellData(SpellId);
            }
        }

        public override string StatusText
        {
            get
            {
                return $"Using ability {Spell.LocalizedName} for {QuestName}.";
            }
        }

        public sealed override bool IsDone
        {
            get
            {
                if (IsQuestComplete)
                { 
                    return true; 
                }

                if (IsStepComplete)
                {
                    return true; 
                }

                if (Conditional != null)
                {
                    var cond = !Conditional();
                    return cond;
                }

                return false;
            }
        }

        [XmlAttribute("SpellId")]
        public uint SpellId { get; set; }

        [XmlAttribute("NpcIds")]
        [XmlAttribute("NpcId")]
        public int[] NpcIds { get; set; }

        [XmlAttribute("UseDistance")]
        [DefaultValue(3.24f)]
        public float UseDistance { get; set; }

        [XmlElement("HotSpots")]
        public IndexedList<HotSpot> Hotspots { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }

        public HotSpot Position
        {
            get
            { 
                return Hotspots.CurrentOrDefault;
            } 
        }

        [XmlAttribute("Radius")]
        [DefaultValue(50f)]
        public float Radius { get; set; }

        [XmlAttribute("WaitTime")]
        public int WaitTime { get; set; }

        [XmlAttribute("Condition")]
        public string Condition { get; set; }

        public Func<bool> Conditional { get; set; }

        public void SetupConditional()
        {
            try
            {
                if (Conditional == null && !string.IsNullOrEmpty(Condition)) 
                {
                    Conditional = ScriptManager.GetCondition(Condition);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic(ScriptManager.FormatSyntaxErrorException(ex));
                TreeRoot.Stop();
                throw;
            }
        }

        protected override Composite CreateBehavior()
        {
            return
                new PrioritySelector(ctx => Target,
                    CustomLogic,
                    new Decorator(ret => Hotspots.Count != 0 && Navigator.InPosition(Position, Core.Me.Location, 5f),
                        new Action(ret => Hotspots.Next())
                    ),
                    CommonBehaviors.MoveAndStop(ret => Position, 3f, true)
                );
        }

        protected bool ShortCircuit(GameObject obj)
        {
            if (!obj.IsValid || !obj.IsTargetable || !obj.IsVisible)
            {
                return true;
            }

            if (Talk.DialogOpen)
            {
                return true; 
            }

            return false;
        }

        private Composite CustomLogic
        {
            get
            {
                return
                    new Decorator(r => (r as GameObject) != null,
                        new PrioritySelector(
                            CommonBehaviors.MoveAndStop(ret => ((GameObject)ret).Location, UseDistance, true),
                            CreateUseSpell()
                         )
                     );
            }
        }

        private Composite CreateUseSpell()
        {
            return
                new Sequence(
                    new Action(ret => Navigator.PlayerMover.MoveStop()),
                    new WaitContinue(5, ret => !MovementManager.IsMoving, new Action(ret => RunStatus.Success)),
                    new Sleep(1000),
                    new DecoratorContinue(ret => ShortCircuit((ret as GameObject)), new Action(ret => RunStatus.Failure)),
                    new DecoratorContinue(r => !Spell.GroundTarget, new Action(ret => ActionManager.DoAction(Spell, ((GameObject)ret)))),
                    new DecoratorContinue(r => Spell.GroundTarget, new Action(ret => ActionManager.DoActionLocation(Spell.Id, ((GameObject)ret).Location))),
                    new Wait(5, ret => Core.Me.IsCasting || ShortCircuit((ret as GameObject)), new Action(ret => RunStatus.Success)),
                    new Sleep(WaitTime)
                );
        }

        private GameObject _target;

        public GameObject Target
        {
            get
            {
                if (_target != null)
                {
                    if (!_target.IsValid || !_target.IsTargetable || !_target.IsVisible)
                    { 
                        _target = null; 
                    }
                    else 
                    { 
                        return _target;
                    }
                }

                _target = GetObject();

                if (_target != null)
                {
                    Log($"Target set to {_target.EnglishName}.");
                }

                return _target;
            }
        }

        protected virtual GameObject GetObject()
        {
            var possible = GameObjectManager.GetObjectsOfType<GameObject>(true, false).Where(obj => obj.IsVisible && obj.IsTargetable && NpcIds.Contains((int)obj.NpcId)).OrderBy(obj => obj.DistanceSqr(Core.Me.Location));

            float closest = float.MaxValue;
            foreach (var obj in possible)
            {
                if (obj.DistanceSqr() < 1)
                {
                    return obj; 
                }

                HotSpot target = null;
                foreach (var hotspot in Hotspots)
                {
                    if (hotspot.WithinHotSpot2D(obj.Location))
                    {
                        var dist = hotspot.Position.DistanceSqr(obj.Location);
                        if (dist < closest)
                        {
                            closest = dist;
                            target = hotspot;
                        }
                    }
                }

                if (target != null)
                {
                    while (Hotspots.Current != target)
                    {
                        Hotspots.Next();
                    }

                    return obj;
                }
            }

            return null;
        }

        protected override void OnStart()
        {
            SetupConditional();

            if (Hotspots != null)
            {
                if (Hotspots.Count == 0)
                {
                    if (XYZ == Vector3.Zero)
                    {
                        LogError("No hotspots and no XYZ provided, this is an invalid combination for this behavior.");
                        return;
                    }

                    Hotspots.Add(new HotSpot(XYZ, Radius));
                }

                Hotspots.IsCyclic = true;
                Hotspots.Index = 0;
            }
        }

        protected override void OnDone()
        {

        }
    }
}