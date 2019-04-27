using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Elevators
{
    public class ElevatorPlatform : GameComponent
    {
        private RawPrimitive Primitive;
        private Color VertexColor = Color.White;
        private Color LightRamp = Color.White;
        private SpriteSheet Sheet;

        public ElevatorPlatform()
        {
            CollisionType = CollisionType.Static;
        }

        public ElevatorPlatform(ComponentManager Manager, Vector3 Position) :
            base(Manager, "Elevator Track", Matrix.CreateTranslation(Position), new Vector3(1.0f, 0.1f, 1.0f), Vector3.Zero)
        {
            CollisionType = CollisionType.Static;
            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            Sheet = new SpriteSheet(ContentPaths.Entities.Furniture.elevator, 32, 32);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            return;
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (Primitive == null)
            {
                var bounds = Vector4.Zero;
                var uvs = Sheet.GenerateTileUVs(new Point(2, 0), out bounds);

                Primitive = new RawPrimitive();

                Primitive.AddQuad(Matrix.Identity, Color.White, Color.White, uvs, bounds);
            }

            if (Primitive.VertexCount == 0) return;

            var under = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(Position));

            if (under.IsValid)
            {
                Color color = new Color(under.Sunlight ? 255 : 0, 255, 0);
                LightRamp = color;
            }
            else
                LightRamp = new Color(200, 255, 0);

            Color origTint = effect.VertexColorTint;

            effect.VertexColorTint = VertexColor;
            effect.LightRamp = LightRamp;
            effect.World = GlobalTransform;

            effect.MainTexture = Sheet.GetTexture();

            effect.EnableWind = false;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }

            effect.VertexColorTint = origTint;
        }
    }
}
