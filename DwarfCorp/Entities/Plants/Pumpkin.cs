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
    public class Pumpkin : Plant
    {
        public Pumpkin() { }

        public Pumpkin(ComponentManager Manager, Vector3 position, string asset, float bushSize) :
            base(Manager, "Pumpkin", position, 0.0f, new Vector3(bushSize, bushSize, bushSize), asset, bushSize)
        {
            LocalBoundingBoxOffset = Vector3.Zero;
            AddChild(new Health(Manager, "HP", 30 * bushSize, 0.0f, 30 * bushSize));
            AddChild(new Flammable(Manager, "Flames"));

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBox.Extents(), LocalBoundingBoxOffset)
            {
                Resources = new List<Inventory.InventoryItem>(),
            }) as Inventory;

            inventory.AddResource(new ResourceAmount()
            {
                Count = 2,
                Type = ResourceType.Pumkin
            });

            var particles = AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
                Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_bush_harvest_1
            }) as ParticleTrigger;

            Tags.Add("Vegetation");
            Tags.Add("Pumpkin");

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
