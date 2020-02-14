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
        public Resource Resource { get; set; }
        public Timer LifeTimer = new Timer(3600, true);
       
        public ResourceEntity()
        {
            
        }

        public ResourceEntity(ComponentManager manager, Resource Resource, Vector3 position) :
            base(manager, Resource.TypeName, Matrix.CreateTranslation(position), new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0))
        {
            this.Resource = Resource;
            Restitution = 0.1f;
            Friction = 0.1f;

            if (Library.GetResourceType(Resource.TypeName).HasValue(out var type))
            {
                Tags.Add(type.TypeName);
                Tags.Add("Resource");

                // Todo: Clean this whole thing up
                if (type.Tags.Contains("Flammable"))
                {
                    AddChild(new Health(Manager, "health", 10.0f, 0.0f, 10.0f));
                    AddChild(new Flammable(Manager, "Flames"));
                }
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
            var tint = Library.GetResourceType(this.Resource.TypeName).HasValue(out var res) ? res.Tint : Color.White;
            if (tint != Color.White)
                this.SetVertexColorRecursive(tint);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (Resource.Gui_NewStyle)
            {
                var sheetName = String.Format("{0}&{1}-{2}&{3}", Resource.Gui_Graphic.AssetPath, Resource.Gui_Graphic.Frame.X, Resource.Gui_Graphic.Frame.Y, Resource.Gui_Palette);
                var tiledInstanceGroup = Manager.World.Renderer.InstanceRenderer.GetCombinedTiledInstance();
                Texture2D fixedTex = null;
                if (!tiledInstanceGroup.DoesInstanceSheetExist(sheetName))
                    if (DwarfSprites.LayerLibrary.FindPalette(Resource.Gui_Palette).HasValue(out var palette))
                        fixedTex = TextureTool.CropAndColorSprite(manager.World.Renderer.GraphicsDevice, AssetManager.GetContentTexture(Resource.Gui_Graphic.AssetPath),
                            Resource.Gui_Graphic.FrameSize, Resource.Gui_Graphic.Frame, DwarfSprites.LayerLibrary.BasePalette.CachedPalette, palette.CachedPalette);

                var sheet = new SpriteSheet(fixedTex)
                {
                    FrameWidth = Resource.Gui_Graphic.FrameSize.X,
                    FrameHeight = Resource.Gui_Graphic.FrameSize.Y,
                    AssetName = sheetName
                };

                var sprite = AddChild(new SimpleBobber(Manager, "Sprite", Matrix.CreateTranslation(Vector3.UnitY * 0.25f), sheet, Point.Zero, 0.15f, 
                    MathFunctions.Rand() + 2.0f, MathFunctions.Rand() * 3.0f)) as Tinter;
                sprite.LocalTransform = Matrix.CreateTranslation(Vector3.UnitY * 0.25f + MathFunctions.RandVector3Cube() * 0.1f);
                sprite.LightRamp = Resource.Tint;
                sprite.SetFlag(Flag.ShouldSerialize, false);
            }
            else
            {
                var compositeLayers = Resource.CompositeLayers;
                var tint = Resource.Tint;

                Tinter sprite = null;

                // Minor optimization for single layer resources.
                if (compositeLayers.Count == 1)
                {
                    var layer = compositeLayers[0];
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

                    foreach (var layer in compositeLayers)
                        layers.Add(new LayeredSimpleSprite.Layer
                        {
                            Sheet = new SpriteSheet(layer.Asset, layer.FrameSize.X, layer.FrameSize.Y),
                            Frame = layer.Frame
                        });

                    sprite = AddChild(new LayeredBobber(Manager, "Sprite",
                        Matrix.CreateTranslation(Vector3.UnitY * 0.25f + MathFunctions.RandVector3Cube() * 0.1f),
                        layers, 0.15f, MathFunctions.Rand() + 2.0f, MathFunctions.Rand() * 3.0f)
                    {
                        OrientationType = LayeredSimpleSprite.OrientMode.Spherical,
                        WorldHeight = 0.75f,
                        WorldWidth = 0.75f,
                    }) as Tinter;
                }

                sprite.LightRamp = tint;
                sprite.SetFlag(Flag.ShouldSerialize, false);
            }
        }
    }
}
