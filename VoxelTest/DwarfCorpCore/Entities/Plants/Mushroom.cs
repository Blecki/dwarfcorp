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
    [JsonObject(IsReference = true)]
    public class Mushroom : Body
    {
        public Mushroom()
        {

        }

        public Mushroom(Vector3 position) :
            base("Mushroom", PlayState.ComponentManager.RootComponent, Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            Matrix matrix = Matrix.CreateRotationY(MathFunctions.Rand(-0.1f, 0.1f));
            matrix.Translation = position + new Vector3(0.5f, -0.25f, 0.5f);
            LocalTransform = matrix;

            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Plants.wheat);

            List<Point> frames = new List<Point>
            {
                new Point(0, 0)
            };
            Animation tableAnimation = new Animation(GameState.Game.GraphicsDevice, spriteSheet, "Mushroom", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            Sprite sprite = new Sprite(PlayState.ComponentManager, "sprite", this, Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite.AddAnimation(tableAnimation);

            Sprite sprite2 = new Sprite(PlayState.ComponentManager, "sprite2", this, Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            };
            sprite2.AddAnimation(tableAnimation);

            Voxel voxelUnder = new Voxel();
            bool success = PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder);

            if (success)
            {
                VoxelListener listener = new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            Inventory inventory = new Inventory("Inventory", this)
            {
                Resources = new ResourceContainer()
                {
                    MaxResources = 1,
                    Resources = new Dictionary<ResourceLibrary.ResourceType, ResourceAmount>()
                    {
                        {
                            ResourceLibrary.ResourceType.Mushroom,
                            new ResourceAmount(ResourceLibrary.ResourceType.Mushroom)
                        }
                    }
                }
            };

            Health health = new Health(PlayState.ComponentManager, "HP", this, 30, 0.0f, 30);
            tableAnimation.Play();
            Tags.Add("Mushroom");
            Tags.Add("Vegetation");
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
