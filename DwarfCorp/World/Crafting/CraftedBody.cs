using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class CraftedBody : GameComponent
    {
        public CraftedBody()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftedBody(
            ComponentManager Manager,
            string name,
            Matrix localTransform,
            Vector3 bboxExtents,
            Vector3 bboxPos,
            CraftDetails details) :
            base(Manager, name, localTransform, bboxExtents, bboxPos)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(details);
        }

    }
}
