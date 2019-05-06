using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Cactus : Plant
    {
        public Cactus() { }

        public Cactus(ComponentManager Manager, Vector3 position, float bushSize) :
            base(Manager, "Cactus", position, MathFunctions.Rand(-0.1f, 0.1f), new Vector3(bushSize, bushSize, bushSize), "Entities\\Plants\\cactus", bushSize)
        {
            AddChild(new Health(Manager, "HP", 30 * bushSize, 0.0f, 30 * bushSize));
            AddChild(new Flammable(Manager, "Flames"));

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBox.Extents(), LocalBoundingBoxOffset)
            {
                Resources = new List<Inventory.InventoryItem>(),
            }) as Inventory;

            inventory.AddResource(new ResourceAmount()
            {
                Count = 2,
                Type = ResourceType.Cactus
            });

            var particles = AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
                Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_bush_harvest_1
            }) as ParticleTrigger;

            Tags.Add("Vegetation");
            Tags.Add("Cactus");

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
