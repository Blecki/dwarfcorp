namespace DwarfCorp
{
    public class CraftDetails : GameComponent // Todo: I would like to get rid of this wasteful class.
    {
        public Resource Resource;

        public CraftDetails()
        {
        }

        public CraftDetails(ComponentManager manager, Resource Resource) :
            base(manager)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            this.Resource = Resource;
        }

        public override void Die()
        {
            if (Resource != null)
            {
                var bounds = this.GetRoot().GetBoundingBox();
                var pos = MathFunctions.RandVector3Box(bounds);
                new ResourceEntity(Manager, Resource, pos);
            }

            base.Die();
        }
    }
}
