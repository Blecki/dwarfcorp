namespace DwarfCorp
{

    /// <summary>
    /// A task is an abstract object which describes a goal for a creature.
    /// Tasks construct acts (or behaviors) to solve them. Tasks have costs,
    /// and can either be feasible or infeasible for a crature.
    /// </summary>
    public class Task
    {

        protected bool Equals(Task other)
        {
            return string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public string Name { get; set; }


        public Task()
        {
            
        }

        public override bool Equals(object obj)
        {
            return obj is Task && Name.Equals(((Task) (obj)).Name);
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
    }

}