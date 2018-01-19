using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Rail
{
    public class RailSprite : Tinter, IRenderableComponent
    {
        private SpriteSheet Sheet;
        private Point Frame;
        private ExtendedVertex[] Verticies;
        private int[] Indicies;
        public float[] VertexHeightOffsets = new float[] { 0.0f, 0.0f, 0.0f, 0.0f };

        public RailSprite(
            ComponentManager Manager,
            String Name,
            Matrix LocalTransform,
            SpriteSheet Sheet,
            Point Frame) 
            : base(Manager, Name, LocalTransform, Vector3.Zero, Vector3.Zero, false)
        {
            this.Sheet = Sheet;
            this.Frame = Frame;
       }

        public RailSprite()
        {
        }
        
        public void SetFrame(Point Frame)
        {
            this.Frame = Frame;
            Verticies = null;
        }

        // Perhaps should be handled in base class?
        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch(messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    HasMoved = true;
                    break;
            }


            base.ReceiveMessageRecursive(messageToReceive);
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
            effect.SelectionBufferColor = this.GetGlobalIDColor().ToVector4();
            Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, false);
        }

        public void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Shader effect,
            bool renderingForWater)
        {
            if (!IsVisible)
                return;

            if (Verticies == null)
            {
                float normalizeX = Sheet.FrameWidth / (float)(Sheet.Width);
                float normalizeY = Sheet.FrameHeight / (float)(Sheet.Height);

                List<Vector2> uvs = new List<Vector2>
                {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(0.0f, 1.0f)
                };

                Vector2 pixelCoords = new Vector2(Frame.X * Sheet.FrameWidth, Frame.Y * Sheet.FrameHeight);
                Vector2 normalizedCoords = new Vector2(pixelCoords.X / (float)Sheet.Width, pixelCoords.Y / (float)Sheet.Height);
                var bounds = new Vector4(normalizedCoords.X + 0.001f, normalizedCoords.Y + 0.001f, normalizedCoords.X + normalizeX - 0.001f, normalizedCoords.Y + normalizeY - 0.001f);

                for (int vert = 0; vert < 4; vert++)
                    uvs[vert] = new Vector2(normalizedCoords.X + uvs[vert].X * normalizeX, normalizedCoords.Y + uvs[vert].Y * normalizeY);

                Verticies = new[]
                {
                    new ExtendedVertex(new Vector3(-0.5f, VertexHeightOffsets[0], 0.5f), Color.White, Color.White, uvs[0], bounds),
                    new ExtendedVertex(new Vector3(0.5f, VertexHeightOffsets[1], 0.5f), Color.White, Color.White, uvs[1], bounds),
                    new ExtendedVertex(new Vector3(0.5f, VertexHeightOffsets[2], -0.5f), Color.White, Color.White, uvs[2], bounds),
                    new ExtendedVertex(new Vector3(-0.5f, VertexHeightOffsets[3], -0.5f), Color.White, Color.White, uvs[3], bounds)
                };

                Indicies = new int[]
                {
                    0, 1, 3,
                    1, 2, 3
                };
            }

            // Everything that draws should set it's tint, making this pointless.
            Color origTint = effect.VertexColorTint;
            ApplyTintingToEffect(effect);

            effect.World = GlobalTransform;

            effect.MainTexture = Sheet.GetTexture();


            effect.EnableWind = false;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, Verticies, 0, 4, Indicies, 0, 2);
            }

            effect.VertexColorTint = origTint;
        }
    }

}
