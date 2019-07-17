using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Grave : Fixture
    {
        [EntityFactory("Grave")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Grave(Manager, Position);
        }

        public Grave()
        {

        }

        public Grave(ComponentManager manager, Vector3 position) :
            base(manager,
            position + MathFunctions.RandVector3Box(-0.05f, 0.05f, -0.001f, 0.001f, -0.05f, 0.05f), 
            new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(MathFunctions.RandInt(4, 8), 1))
        {
            Name = "Grave";
            Tags.Add("Grave");
            Matrix transform = Matrix.CreateRotationY(1.57f + MathFunctions.Rand(-0.1f, 0.1f));
            transform.Translation = LocalTransform.Translation;
            LocalTransform = transform;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (GetComponent<SimpleSprite>().HasValue(out var sprite))
                sprite.OrientationType = SimpleSprite.OrientMode.Fixed;
        }
    }

}
