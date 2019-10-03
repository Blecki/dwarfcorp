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
    public class Mushroom : Plant
    {
        public Mushroom()
        {

        }

        public Mushroom(ComponentManager Manager,
                        Vector3 position, 
                        string asset, 
                        String resource, 
                        int numRelease, bool selfIlluminate) :
            base(Manager, "Mushroom", position, MathFunctions.Rand(-0.1f, 0.1f), new Vector3(1.0f, 1.0f, 1.0f), asset, 1.0f)
        {

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBox.Extents(), LocalBoundingBoxOffset)) as Inventory;

            for (int i = 0; i < numRelease; i++)
            {
                inventory.Resources.Add(new Inventory.InventoryItem()
                {
                    MarkedForRestock = false,
                    MarkedForUse = false,
                    Resource = new Resource(resource)
                });
            }

            var particles = AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
    Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_bush_harvest_1
            }) as ParticleTrigger;

            AddChild(new Health(Manager.World.ComponentManager, "HP", 30, 0.0f, 30));
            AddChild(new Flammable(Manager.World.ComponentManager, "Flames"));

            Tags.Add("Mushroom");
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
