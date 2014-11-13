using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    /// <summary>
    /// A task is an abstract object which describes a goal for a creature.
    /// Tasks construct acts (or behaviors) to solve them. Tasks have costs,
    /// and can either be feasible or infeasible for a crature.
    /// </summary>
    public abstract class Task
    {
        public enum PriorityType
        {
            Eventually = 0,
            Low = 1,
            Medium = 2,
            High = 3,
            Urgent = 4
        }

        public PriorityType Priority { get; set; }
        public Act Script { get; set; }
        public bool AutoRetry = false;

        protected bool Equals(Task other)
        {
            return string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public string Name { get; set; }

        public abstract Task Clone();

        public virtual void Render(GameTime time)
        {
            
        }

        public override bool Equals(object obj)
        {
            return obj is Task && Name.Equals(((Task) (obj)).Name);
        }


        public virtual void SetupScript(Creature agent)
        {
            if(Script != null)
                Script.OnCanceled();

            Script = CreateScript(agent);
        }


        public virtual Act CreateScript(Creature agent)
        {
            return null;
        }

        public virtual float ComputeCost(Creature agent)
        {
            return 1.0f;
        }

        public virtual bool IsFeasible(Creature agent)
        {
            return true;
        }

        public virtual bool ShouldRetry(Creature agent)
        {
            return AutoRetry;
        }
    }

    public class ActWrapperTask : Task
    {
        public ActWrapperTask()
        {
            
        }


        public ActWrapperTask(Act act)
        {
            Script = act;
            Name = Script.Name;
        }

        public override Task Clone()
        {
            return new ActWrapperTask(Script);
        }

        public override bool IsFeasible(Creature agent)
        {
            return true;
        }

        public override Act CreateScript(Creature agent)
        {
            Script.Initialize();
            return Script;
        }
    }

}