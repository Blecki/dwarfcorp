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
            base(manager, Resource == null ? "INVALID" : Resource.DisplayName, Matrix.CreateTranslation(position), new Vector3(0.75f, 0.75f, 0.75f), Vector3.Zero, 0.5f, 0.5f, 0.999f, 0.999f, new Vector3(0, -10, 0))
        {
            this.Resource = Resource;
            Friction = 0.1f;

            if (Resource != null && Library.GetResourceType(Resource.TypeName).HasValue(out var type))
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
                Die();
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (Resource != null && Resource.Gui_Graphic != null)
            {
                var sheetName = Resource.Gui_Graphic.GetSheetIdentifier();
                var tiledInstanceGroup = Manager.World.Renderer.InstanceRenderer.GetCombinedTiledInstance();

                Texture2D fixedTex = null;
                if (!tiledInstanceGroup.DoesInstanceSheetExist(sheetName))
                    fixedTex = ResourceGraphicsHelper.GetResourceTexture(manager.World.Renderer.GraphicsDevice, Resource.Gui_Graphic);

                var sheet = new SpriteSheet(fixedTex) // The tiled instance renderer will automatically grab the texture from this.
                {
                    FrameWidth = Resource.Gui_Graphic.FrameSize.X,
                    FrameHeight = Resource.Gui_Graphic.FrameSize.Y,
                    AssetName = sheetName
                };

                var sprite = AddChild(new SimpleBobber(Manager, "Sprite", Matrix.CreateTranslation(Vector3.UnitY * 0.25f), sheet, Point.Zero, 0.15f, 
                    MathFunctions.Rand() + 2.0f, MathFunctions.Rand() * 3.0f)
                    {
                    OrientationType = SimpleSprite.OrientMode.Spherical,
                        WorldHeight = 0.75f,
                        WorldWidth = 0.75f,
                    }) as Tinter;
                sprite.LocalTransform = Matrix.CreateTranslation(Vector3.UnitY * 0.25f + MathFunctions.RandVector3Cube() * 0.1f);
                sprite.LightRamp = new Color(255, 255, 255, 255);
                sprite.SetFlag(Flag.ShouldSerialize, false);
            }
        }
    }
}
