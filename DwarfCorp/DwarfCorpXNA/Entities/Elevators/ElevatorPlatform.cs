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
    public class ElevatorPlatform : Body
    {
        public RawPrimitive Primitive;
        private Color VertexColor = Color.White;
        private Color LightRamp = Color.White;
        private SpriteSheet Sheet;

        public ElevatorPlatform()
        {
            CollisionType = CollisionType.Static;
        }

        public ElevatorPlatform(ComponentManager Manager, Vector3 Position, List<ResourceAmount> Resources) :
            base(Manager, "Elevator Track", Matrix.CreateTranslation(Position), Vector3.One, Vector3.Zero)
        {
            CollisionType = CollisionType.Static;
            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            Sheet = new SpriteSheet(ContentPaths.rail_tiles, 32, 32);
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
                var uvs = Sheet.GenerateTileUVs(new Point(0, 0), out bounds);

                Primitive = new RawPrimitive();

                Primitive.AddQuad(Matrix.Identity, Color.White, Color.White, uvs, bounds);
            }

            if (Primitive.VertexCount == 0) return;

            var under = new VoxelHandle(chunks.ChunkData,
                    GlobalVoxelCoordinate.FromVector3(Position));

            if (under.IsValid)
            {
                Color color = new Color(under.Sunlight ? 255 : 0, 255, 0);
                LightRamp = color;
            }
            else
                LightRamp = new Color(200, 255, 0);

            Color origTint = effect.VertexColorTint;
            if (!Active)
            {
                DoStipple(effect);
            }
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
            if (!Active)
            {
                EndDraw(effect);
            }
        }

        private string previousEffect = null;

        public void DoStipple(Shader effect)
        {
#if DEBUG
            if (effect.CurrentTechnique.Name == Shader.Technique.Stipple)
            {
                throw new InvalidOperationException("Stipple technique not cleaned up. Was EndDraw called?");
            }
#endif
            if (effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBuffer] && effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBufferInstanced])
            {
                previousEffect = effect.CurrentTechnique.Name;
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.Stipple];
            }
            else
            {
                previousEffect = null;
            }
        }

        public void EndDraw(Shader shader)
        {
            if (!String.IsNullOrEmpty(previousEffect))
            {
                shader.CurrentTechnique = shader.Techniques[previousEffect];
            }
        }
    }
}
