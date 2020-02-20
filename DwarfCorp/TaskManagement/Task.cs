using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public bool AutoRetry = false;
        public string Name;
        public float BoredomIncrease = 0.0f;
        public float EnergyDecrease = 0.0f;
        public bool WasCancelled = false;

        public FailureRecord FailureRecord = new FailureRecord();

        [JsonIgnore] public object GuiTag = null;

        public override int GetHashCode() { return Name != null ? Name.GetHashCode() : 0; }
        protected bool Equals(Task other) { return other == null ? false : Name == other.Name; }
        public override bool Equals(object obj) { return !Object.ReferenceEquals(obj, null) && obj is Task && string.Equals(Name, ((Task)(obj)).Name); }

        public void CleanupInactiveWorkers()
        {
            AssignedCreatures.RemoveAll(c => !c.Active || c.IsDead);
        }

        public void OnAssign(CreatureAI agent) { AssignedCreatures.Add(agent); }
        public void OnUnAssign(CreatureAI agent) { AssignedCreatures.Remove(agent); }

        public virtual void Render(DwarfTime time) {}
        public virtual MaybeNull<Act> CreateScript(Creature agent) { return null; }
        public virtual float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false) { return 1.0f; }
        public virtual Feasibility IsFeasible(Creature agent) { return Feasibility.Feasible; }
        public virtual bool ShouldRetry(Creature agent) { return AutoRetry; }
        public virtual bool ShouldDelete(Creature agent) { return false; }
        public virtual bool IsComplete(WorldManager World) { return false; }
        public virtual void OnEnqueued(WorldManager World) {}
        public virtual void OnDequeued(WorldManager World) {}
        public virtual void OnVoxelChange(VoxelChangeEvent changeEvent) {}
        public virtual void OnUpdate(WorldManager World) {}
        public virtual void OnCancelled(TaskManager Manager, WorldManager World) {}

        public virtual Microsoft.Xna.Framework.Vector3? GetCameraZoomLocation()
        {
            if (AssignedCreatures.Count > 0)
                return AssignedCreatures[0].Position;
            return null;
        }
    }
}