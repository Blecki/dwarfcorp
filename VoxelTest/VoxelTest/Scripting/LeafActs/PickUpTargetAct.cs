using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class PickUpTargetAct : CreatureAct
    {
        public enum PickUpType
        {
            None,
            Stockpile,
            Room
        }

        public Zone Zone { get; set;}

        public PickUpType PickType { get; set; }

        public PickUpTargetAct(CreatureAIComponent agent, PickUpType type, Zone zone) :
            base(agent)
        {
            Name = "Pick up Target";
            PickType = type;
            Zone = zone;
        }


        public bool EntityIsInHands()
        {
            return Agent.TargetComponent == Agent.Hands.GetFirstGrab();
        }


        public override IEnumerable<Status> Run()
        {

            if (EntityIsInHands())
            {
                yield return Status.Success;
            }
            else if (Creature.Hands.IsFull() || Agent.TargetComponent == null)
            {
                yield return Status.Fail;
            }
            else
            {
                switch (PickType)
                {
                    case (PickUpType.Room):
                    case (PickUpType.Stockpile):
                    {

                        if (Zone == null)
                        {
                            yield return Status.Fail;
                            break;
                        }
                        bool removed = Zone.RemoveItem(Agent.TargetComponent);
                        
                        if (removed)
                        {
                            Creature.Hands.Grab(Agent.TargetComponent);
                            Agent.Blackboard.SetData("HeldObject", Agent.TargetComponent);
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
                        Creature.Hands.Grab(Agent.TargetComponent);
                        Agent.Blackboard.SetData("HeldObject", Agent.TargetComponent);

                        if (Creature.Master.GatherDesignations.Contains(Agent.TargetComponent))
                        {
                            Creature.Master.GatherDesignations.Remove(Agent.TargetComponent);
                        }

                        yield return Status.Success;
                        break;
                    }
               
                }
            }
        }
        
    }
}
