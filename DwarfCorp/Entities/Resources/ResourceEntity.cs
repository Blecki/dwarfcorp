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
    public class ResourceEntity : Physics
    {
        public ResourceAmount Resource { get; set; }
        public Timer LifeTimer = new Timer(3600, true);
       
        public ResourceEntity()
        {
            
        }

        public ResourceEntity(ComponentManager manager, ResourceAmount resourceType, Vector3 position) :
            base(manager, resourceType.Type, 
                Matrix.CreateTranslation(position), new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0))
        {
            Resource = resourceType;
            if (Resource.Count > 1)
            {
                Name = String.Format("Pile of {0} {1}s", Resource.Count, Resource.Type);
            }
            Restitution = 0.1f;
            Friction = 0.1f;
            Resource type = ResourceLibrary.GetResourceByName(resourceType.Type);
            
            Tags.Add(type.Name);
            Tags.Add("Resource");
            
            // Todo: Clean this whole thing up
            if (type.Tags.Contains(DwarfCorp.Resource.ResourceTags.Flammable))
            {
                AddChild(new Health(Manager, "health", 10.0f, 0.0f, 10.0f));
                AddChild(new Flammable(Manager, "Flames"));
            }

            PropogateTransforms();
            CreateCosmeticChildren(Manager);
            Orientation = OrientMode.Fixed;
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {           
            base.Update(gameTime, chunks, camera);

            LifeTimer.Update(gameTime);
            if (LifeTimer.HasTriggered)
            {
                Die();
            }
            var tint = ResourceLibrary.GetResourceByName(this.Resource.Type).Tint;
            if (tint != Color.White)
                this.SetVertexColorRecursive(ResourceLibrary.GetResourceByName(this.Resource.Type).Tint);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            var type = ResourceLibrary.GetResourceByName(Resource.Type);

            if (type == null)
            {
                var invalidResource = ResourceLibrary.GetResourceByName("Invalid");
                if (!ResourceLibrary.IsInitialized)
                {
                    throw new InvalidOperationException("Tried to create a resource entity before the resource library was initialized.");
                }
                else if (invalidResource == null)
                {
                    throw new InvalidOperationException("Tried to create a resource entity that is invalid.");
                }
                type = invalidResource;
            }

            Tinter sprite = null;

            int numSprites = Math.Min(Resource.Count, 3);
            for (int i = 0; i < numSprites; i++)
            {
                // Minor optimization for single layer resources.
                if (type.CompositeLayers.Count == 1)
                {
                    var layer = type.CompositeLayers[0];
                    sprite = AddChild(new SimpleBobber(Manager, "Sprite",
                        Matrix.CreateTranslation(Vector3.UnitY * 0.25f),
                        new SpriteSheet(layer.Asset, layer.FrameSize.X, layer.FrameSize.Y),
                        layer.Frame, 0.15f, MathFunctions.Rand() + 2.0f, MathFunctions.Rand() * 3.0f)
                    {
                        OrientationType = SimpleSprite.OrientMode.Spherical,
                        WorldHeight = 0.75f,
                        WorldWidth = 0.75f,
                    }) as Tinter;
                    sprite.LocalTransform = Matrix.CreateTranslation(Vector3.UnitY * 0.25f + MathFunctions.RandVector3Cube() * 0.1f);
                }
                else
                {
                    var layers = new List<LayeredSimpleSprite.Layer>();

                    foreach (var layer in type.CompositeLayers)
                    {
                        layers.Add(new LayeredSimpleSprite.Layer
                        {
                            Sheet = new SpriteSheet(layer.Asset, layer.FrameSize.X, layer.FrameSize.Y),
                            Frame = layer.Frame
                        });
                    }

                    sprite = AddChild(new LayeredBobber(Manager, "Sprite",
                        Matrix.CreateTranslation(Vector3.UnitY * 0.25f + MathFunctions.RandVector3Cube() * 0.1f),
                        layers, 0.15f, MathFunctions.Rand() + 2.0f, MathFunctions.Rand() * 3.0f)
                    {
                        OrientationType = LayeredSimpleSprite.OrientMode.Spherical,
                        WorldHeight = 0.75f,
                        WorldWidth = 0.75f,
                    }) as Tinter;
                }

                sprite.LightRamp = type.Tint;
                sprite.SetFlag(Flag.ShouldSerialize, false);
            }
        }
    }
}
