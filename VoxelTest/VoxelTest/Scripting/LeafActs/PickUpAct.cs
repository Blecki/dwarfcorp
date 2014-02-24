using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature grabs a given item.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class PickUpAct : CreatureAct
    {
        public enum PickUpType
        {
            None,
            Stockpile,
            Room
        }

        public Zone Zone { get; set; }

        public PickUpType PickType { get; set; }

        public string TargetName { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public LocatableComponent Target { get { return GetTarget(); } set { SetTarget(value);} }

        public PickUpAct()
        {

        }

        public PickUpAct(CreatureAIComponent agent, PickUpType type, Zone zone, string targetName) :
            base(agent)
        {
            Name = "Pick up " + targetName;
            PickType = type;
            Zone = zone;
            TargetName = targetName;
        }

        public LocatableComponent GetTarget()
        {
            return Agent.Blackboard.GetData<LocatableComponent>(TargetName);
        }

        public void SetTarget(LocatableComponent targt)
        {
            Agent.Blackboard.SetData(TargetName, targt);
        }


        public bool EntityIsInHands()
        {
            return Target == Agent.Hands.GetFirstGrab();
        }


        public override IEnumerable<Status> Run()
        {
            if(EntityIsInHands())
            {
                yield return Status.Success;
            }
            else if(Creature.Hands.IsFull() || Target == null)
            {
                yield return Status.Fail;
            }
            else
            {
                switch(PickType)
                {
                    case (PickUpType.Room):
                    case (PickUpType.Stockpile):
                    {
                        if(Zone == null)
                        {
                            yield return Status.Fail;
                            break;
                        }
                        bool removed = Zone.RemoveItem(Target);

                        if(removed)
                        {
                            Creature.Hands.Grab(Target);
                            Agent.Blackboard.SetData("HeldObject", Target);
                            yield return Status.Success;
                        }
                        else
                        {
                            yield return Status.Fail;
                        }
                        break;
                    }
                    case (PickUpType.None):
                    {
                        Creature.Hands.Grab(Target);
                        Agent.Blackboard.SetData("HeldObject", Target);

                        if(Creature.Faction.GatherDesignations.Contains(Target))
                        {
                            Creature.Faction.GatherDesignations.Remove(Target);
                        }

                        yield return Status.Success;
                        break;
                    }
                }
            }
        }
    }

}