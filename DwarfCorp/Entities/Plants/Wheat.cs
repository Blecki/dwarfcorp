using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Wheat : Plant
    {
        public Wheat()
        {
            
        }

        public Wheat(ComponentManager Manager, Vector3 position) :
            base(Manager, "Wheat", position, MathFunctions.Rand(-0.1f, 0.1f), new Vector3(1.0f, 1.0f, 1.0f), "Entities\\Plants\\wheat", 1.0f)
        {
            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBox.Extents(), LocalBoundingBoxOffset)) as Inventory;

            for (int i = 0; i < MathFunctions.RandInt(1, 5); i++)
            {
                inventory.Resources.Add(new Inventory.InventoryItem()
                {
                    MarkedForRestock = false,
                    MarkedForUse = false,
                    Resource = new Resource("Grain")
                });
            }

            AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
                Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_bush_harvest_1
            });

            AddChild(new Health(Manager, "HP", 30, 0.0f, 30));
            AddChild(new Flammable(Manager, "Flames"));

            Tags.Add("Wheat");
            Tags.Add("Vegetation");
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
