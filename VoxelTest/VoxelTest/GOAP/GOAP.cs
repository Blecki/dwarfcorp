using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference =  true)]
    public class GOAP
    {
        #region helper_classes

        public enum MotionStatus
        {
            Moving,
            Stationary
        }

        public enum TargetType
        {
            Voxel,
            Entity,
            Zone,
            None
        }

        public enum HandState
        {
            Empty,
            Full
        }

        #endregion

        public Dictionary<string, Goal> Goals { get; set; }
        public Dictionary<string, Action> Actions { get; set; }
        public WorldState Belief { get; set; }
        public GOAPPlanner Planner { get; set; }
        public List<Zone> Zones { get; set; }
        public List<Item> Items { get; set; }
        public List<VoxelRef> Voxels { get; set; }
        public CreatureAIComponent Creature { get; set; }


        public GOAP(CreatureAIComponent creature)
        {
            Goals = new Dictionary<string, Goal>();
            Actions = new Dictionary<string, Action>();
            Belief = new WorldState();
            Zones = new List<Zone>();
            Items = new List<Item>();
            Voxels = new List<VoxelRef>();

            Planner = new GOAPPlanner(this);
            Creature = creature;
        }

        public void CreateGeneralActions()
        {
            AddAction(new ForgetTargets());
            AddAction(new LeaveTarget());
            AddAction(new ForgetTargetEntity());
            AddAction(new ForgetTargetZone());
            AddAction(new AttackTargetEntity());
            AddAction(new AttackTargetVoxel());
            AddAction(new GoToTargetVoxel());
            AddAction(new GoToTargetEntity());
            AddAction(new GoToTargetZone());
            AddAction(new PickupTargetEntity(this));
            AddAction(new DropHeldItem());
            AddAction(new Stop());
            AddAction(new Wander());
            AddAction(new EatHeldItem());
        }

        public void CreateZoneActions()
        {
            foreach(Zone z in Zones)
            {
                AddAction(new PutHeldObjectInZone(Creature, z));
                AddAction(new SetTargetZone(Creature, z));
            }

            foreach(VoxelRef v in Voxels)
            {
                AddAction(new SetTargetVoxel(v));
            }
        }

        public void CreateItemActions()
        {
            foreach(Item i in Items)
            {
                AddAction(new SetTargetEntity(i));

                if(i is InteractiveItem)
                {
                    AddAction(new Interact((InteractiveItem) i));
                }
            }
        }

        public void AddGoal(Goal goal)
        {
            if(!Goals.ContainsKey(goal.Name))
            {
                Goals[goal.Name] = goal;
            }
        }

        public void AddAction(Action a)
        {
            Actions[a.Name] = a;
        }

        public List<Action> GetPossibleActions(WorldState belief)
        {
            List<Action> toReturn = new List<Action>();
            foreach(Action a in Actions.Values)
            {
                if(a.CanPerform(belief))
                {
                    toReturn.Add(a);
                }
            }
            return toReturn;
        }

        public List<Action> GetActionsSatisfying(WorldState belief)
        {
            List<Action> toReturn = new List<Action>();
            List<Action> actionsCopy = new List<Action>();
            actionsCopy.AddRange(Actions.Values);
            foreach(Action a in actionsCopy)
            {
                if(a.Satisfies(belief))
                {
                    toReturn.Add(a);
                }
            }
            return toReturn;
        }


        public Goal GetHighestPriorityGoal()
        {
            Goal max = null;
            float maxPriority = 0;
            foreach(Goal goal in Goals.Values)
            {
                if(goal.Priority > maxPriority)
                {
                    maxPriority = goal.Priority;
                    max = goal;
                }
            }

            return max;
        }

        public List<Action> PlanToGoal(Goal goal)
        {
            List<Action> preset = goal.GetPresetPlan(Creature, this);
            if(preset != null)
            {
                Creature.Say("Preset plan.");
                return preset;
            }

            GOAPPlanner.GOAPNode start = new GOAPPlanner.GOAPNode();
            start.m_state = new WorldState(Belief);

            GOAPPlanner.GOAPNode end = new GOAPPlanner.GOAPNode();
            end.m_state = new WorldState(goal.State);


            if(goal is CompoundGoal)
            {
                CompoundGoal comp = (CompoundGoal) goal;
                Creature.Say("Goal Index: " + comp.CurrentGoalIndex);
                end.m_state = comp.Goals[comp.CurrentGoalIndex].State;
            }


            List<GOAPPlanner.GOAPNode> path = Planner.FindPath(start, end, null, 1000);

            List<Action> toReturn = new List<Action>();

            if(path == null)
            {
                return null;
            }
            else
            {
                foreach(GOAPPlanner.GOAPNode node in path)
                {
                    if(node.m_actionTaken != null)
                    {
                        toReturn.Add(node.m_actionTaken);
                    }
                }

                return toReturn;
            }
        }
    }

}