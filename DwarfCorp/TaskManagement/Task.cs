using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DwarfCorp
{
    public abstract class Task
    {
        public TaskCategory Category { get; set; }
        public TaskPriority Priority { get; set; }
        public int MaxAssignable = 1;
        public bool ReassignOnDeath = true;
        public List<CreatureAI> AssignedCreatures = new List<CreatureAI>();
        public bool Hidden = false;

        [JsonIgnore] public object GuiTag = null;

        public bool AutoRetry = false;
        public string Name;
        // Todo: Need to add energy cost per task. Apply it to their energy status the same way as boredom.
        public float BoredomIncrease = 0.0f;

        protected bool Equals(Task other)
        {
            if (global::System.Object.ReferenceEquals(other, null))
            {
                return false;
            }
            return string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public virtual void Render(DwarfTime time)
        {
            
        }

        public override bool Equals(object obj)
        {
            return !global::System.Object.ReferenceEquals(obj, null) && obj is Task && string.Equals(Name, ((Task) (obj)).Name);
        }

        public virtual Act CreateScript(Creature agent)
        {
            return null;
        }

        public virtual float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }

        public virtual Feasibility IsFeasible(Creature agent)
        {
            return Feasibility.Feasible;
        }

        public virtual bool ShouldRetry(Creature agent)
        {
            return AutoRetry;
        }

        public virtual bool ShouldDelete(Creature agent)
        {
            return false;
        }

        public virtual void OnAssign(CreatureAI agent)
        {
            AssignedCreatures.Add(agent);
        }

        public virtual void OnUnAssign(CreatureAI agent)
        {
            AssignedCreatures.Remove(agent);   
        }

        public virtual bool IsComplete(WorldManager World)
        {
            return false;
        }

        public virtual void OnEnqueued(WorldManager World)
        {

        }

        public virtual void OnDequeued(WorldManager World)
        {

        }

        public virtual void OnVoxelChange(VoxelChangeEvent changeEvent)
        {

        }

        public virtual void OnUpdate()
        {

        }

        public virtual void OnCancelled(TaskManager Manager, WorldManager World)
        {

        }

    }
}