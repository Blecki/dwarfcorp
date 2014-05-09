using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature grabs a given item and puts it in their inventory
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class StashAct : CreatureAct
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

        public string StashedItemOut { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Body Target { get { return GetTarget(); } set { SetTarget(value); } }

        public StashAct()
        {

        }

        public StashAct(CreatureAIComponent agent, PickUpType type, Zone zone, string targetName, string stashedItemOut) :
            base(agent)
        {
            Name = "Stash " + targetName;
            PickType = type;
            Zone = zone;
            TargetName = targetName;
            StashedItemOut = stashedItemOut;
        }

        public Body GetTarget()
        {
            return Agent.Blackboard.GetData<Body>(TargetName);
        }

        public void SetTarget(Body targt)
        {
            Agent.Blackboard.SetData(TargetName, targt);
        }


        public override IEnumerable<Status> Run()
        {
            if(Target == null)
            {
                yield return Status.Fail;
            }

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
                        bool removed = Zone.Resources.RemoveResource(new ResourceAmount(Target.Tags[0]));

                        if (removed)
                        {
                            if(Creature.Inventory.Pickup(Target))
                            {
                                Agent.Blackboard.SetData(StashedItemOut, new ResourceAmount(Target));
                                SoundManager.PlaySound(ContentPaths.Audio.dig, Agent.Position);
                                yield return Status.Success;
                            }
                            else
                            {
                                yield return Status.Fail;
                            }
                        }
                        else
                        {
                            yield return Status.Fail;
                        }
                        break;
                    }
                case (PickUpType.None):
                    {
                        if (!Creature.Inventory.Pickup(Target))
                        {
                            yield return Status.Fail;
                        }

                        if (Creature.Faction.GatherDesignations.Contains(Target))
                        {
                            Creature.Faction.GatherDesignations.Remove(Target);
                        }

                        ResourceAmount resource = new ResourceAmount(Target);
                        Agent.Blackboard.SetData(StashedItemOut, resource);
                        Creature.DrawIndicator(resource.ResourceType.Image);
                        SoundManager.PlaySound(ContentPaths.Audio.dig, Agent.Position);
                        yield return Status.Success;
                        break;
                    }
            }
        }
        
    }
    
}

