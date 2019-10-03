using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Bush : Plant
    {
        public Bush() { }

        public Bush(ComponentManager componentManager, Vector3 position, string asset, float bushSize) :
            base(componentManager, "Berry Bush", position, MathFunctions.Rand(-0.1f, 0.1f), new Vector3(bushSize, bushSize, bushSize), asset, bushSize)
        {
            AddChild(new Health(componentManager, "HP", 30 * bushSize, 0.0f, 30 * bushSize));
            AddChild(new Flammable(componentManager, "Flames"));

            var particles = AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter", Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_bush_harvest_1
            }) as ParticleTrigger;

            Tags.Add("Vegetation");
            Tags.Add("Bush");
            Tags.Add("EmitsFood");

            Inventory inventory = AddChild(new Inventory(componentManager, "Inventory", BoundingBox.Extents(), LocalBoundingBoxOffset)) as Inventory;

            for (var i = 0; i < 3; ++i)
                inventory.AddResource(new Resource("Berry"));

            CollisionType = CollisionType.Static;
            PropogateTransforms();
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            CreateCrossPrimitive(MeshAsset);
            base.CreateCosmeticChildren(Manager);
        }
    }
}
