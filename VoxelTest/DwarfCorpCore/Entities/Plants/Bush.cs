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
    [JsonObject(IsReference = true)]
    public class Bush : Body
    {
        public Bush() { }

        public Bush(Vector3 position, string asset, float bushSize) :
            base("Bush", PlayState.ComponentManager.RootComponent, Matrix.Identity, new Vector3(bushSize, bushSize, bushSize),Vector3.Zero)
        {
            ComponentManager componentManager = PlayState.ComponentManager;
            Matrix matrix = Matrix.Identity;
            matrix.Translation = position + new Vector3(0.5f, -0.2f, 0.5f);
            LocalTransform = matrix;

            new Mesh(componentManager, "Model", this, Matrix.CreateScale(bushSize, bushSize, bushSize), asset, false);

            Health health = new Health(componentManager, "HP", this, 30 * bushSize, 0.0f, 30 * bushSize);
            new Flammable(componentManager, "Flames", this, health);

            Voxel voxelUnder = new Voxel();

            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(componentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            Tags.Add("Vegetation");
            Tags.Add("Bush");
            Tags.Add("EmitsFood");
            Inventory inventory = new Inventory("Inventory", this)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = (int)(bushSize * 5)
                }
            };

            inventory.Resources.AddResource(new ResourceAmount()
            {
                NumResources = (int)(bushSize * 5),
                ResourceType = ResourceLibrary.Resources[ResourceLibrary.ResourceType.Berry]
            });

            AddToOctree = true;
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
