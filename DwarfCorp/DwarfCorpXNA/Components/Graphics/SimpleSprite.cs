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
    public class SimpleSprite : Tinter
    {
        public enum OrientMode
        {
            Fixed,
            Spherical,
            YAxis,
        }

        public OrientMode OrientationType = OrientMode.Spherical;
        public float WorldWidth = 1.0f;
        public float WorldHeight = 1.0f;
        private Vector3 prevDistortion = Vector3.Zero;
        [JsonProperty]
        private SpriteSheet Sheet;
        [JsonProperty]
        private Point Frame;

        private NewInstanceData InstanceData = null;

        public SimpleSprite(
            ComponentManager Manager,
            String Name,
            Matrix LocalTransform,
            SpriteSheet Sheet,
            Point Frame)
            : base(Manager, Name, LocalTransform, Vector3.Zero, Vector3.Zero)
        {
            this.Sheet = Sheet;
            this.Frame = Frame;
            AutoSetWorldSize();
        }

        public SimpleSprite()
        {
        }

        public void SetFrame(Point Frame)
        {
            this.Frame = Frame;
        }

        // Perhaps should be handled in base class?
        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch (messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    HasMoved = true;
                    break;
            }

            base.ReceiveMessageRecursive(messageToReceive);
        }

        private void PrepareInstanceData(Camera Camera)
        {
            if (InstanceData == null) InstanceData = new NewInstanceData("combined-tiled-instances", Matrix.Identity, Color.White);

            InstanceData.Transform = GetWorldMatrix(Camera);
            InstanceData.LightRamp = LightRamp;
            InstanceData.SpriteBounds = new Rectangle(Sheet.FrameWidth * Frame.X, Sheet.FrameHeight * Frame.Y, Sheet.FrameWidth, Sheet.FrameHeight);
            InstanceData.TextureAsset = Sheet.AssetName; // Todo: Cache the raw texture info so the renderer doesn't need to look it up all the time.
            InstanceData.SelectionBufferColor = this.GetGlobalIDColor();
            InstanceData.VertexColorTint = VertexColorTint;
            if (Stipple)
            {
                InstanceData.VertexColorTint.A = 256 / 2;
            }
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            if (!IsVisible) return;

            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);

            PrepareInstanceData(camera);

            Manager.World.InstanceRenderer.RenderInstance(InstanceData, graphicsDevice, effect, camera, InstanceRenderMode.SelectionBuffer);
        }

        public void AutoSetWorldSize()
        {
            if (Sheet == null)
                return;
            WorldWidth = Sheet.FrameWidth / 32.0f;
            WorldHeight = Sheet.FrameHeight / 32.0f;
        }

        override public void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Shader effect,
            bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (!IsVisible) return;

            PrepareInstanceData(camera);

            Manager.World.InstanceRenderer.RenderInstance(InstanceData, graphicsDevice, effect, camera, InstanceRenderMode.Normal);
        }

        private Matrix GetWorldMatrix(Camera camera)
        {
            var currDistortion = VertexNoise.GetNoiseVectorFromRepeatingTexture(GlobalTransform.Translation);
            var distortion = currDistortion * 0.1f + prevDistortion * 0.9f;
            prevDistortion = distortion;

            switch (OrientationType)
            {
                case OrientMode.Spherical:
                    return Matrix.CreateScale(WorldWidth, WorldHeight, 1.0f) * Matrix.CreateBillboard(GlobalTransform.Translation, camera.Position, camera.UpVector, null) * Matrix.CreateTranslation(distortion);
                case OrientMode.Fixed:
                    {
                        Matrix rotation = GlobalTransform;
                        rotation.Translation = rotation.Translation + distortion;
                        return Matrix.CreateScale(WorldWidth, WorldHeight, 1.0f) * rotation;
                    }
                case OrientMode.YAxis:
                    {
                        Matrix worldRot = Matrix.CreateConstrainedBillboard(GlobalTransform.Translation, camera.Position, Vector3.UnitY, null, null);
                        worldRot.Translation = worldRot.Translation + distortion;
                        return Matrix.CreateScale(WorldWidth, WorldHeight, 1.0f) * worldRot;
                    }
                default:
                    throw new InvalidProgramException();
            }
        }
    }
}
