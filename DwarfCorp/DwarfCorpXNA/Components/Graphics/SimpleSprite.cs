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
    public class SimpleSprite : Tinter, IUpdateableComponent, IRenderableComponent
    {
        public enum OrientMode
        {
            Fixed,
            Spherical,
            YAxis,
        }

        public OrientMode OrientationType = OrientMode.Spherical;
        public bool DrawSilhouette = false;
        public Color SilhouetteColor = new Color(0.0f, 1.0f, 1.0f, 0.5f);
        public bool EnableWind = false;
        public float WorldWidth = 1.0f;
        public float WorldHeight = 1.0f;
        private Vector3 prevDistortion = Vector3.Zero;
        private GeometricPrimitive Primitive;
        private SpriteSheet Sheet;
        private Point Frame;

        public SimpleSprite(
            ComponentManager Manager,
            String Name,
            Matrix LocalTransform,
            bool AddToCollisionManager,
            SpriteSheet Sheet,
            Point Frame) 
            : base(Manager, Name, LocalTransform, Vector3.Zero, Vector3.Zero, AddToCollisionManager)
        {
            this.Sheet = Sheet;
            this.Frame = Frame;
        }

        public SimpleSprite()
        {
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
            effect.SelectionBufferColor = GetGlobalIDColor().ToVector4();
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

            if (Primitive == null)
            {
                Primitive = new BillboardPrimitive(
                    graphicsDevice,
                    Sheet.GetTexture(),
                    Sheet.FrameWidth,
                    Sheet.FrameHeight,
                    Frame,
                    WorldWidth,
                    WorldHeight,
                    Tint,
                    false);
            }

            GamePerformance.Instance.StartTrackPerformance("Render - Simple Sprite");

            // Everything that draws should set it's tint, making this pointless.
            Color origTint = effect.VertexColorTint;  
            ApplyTintingToEffect(effect);            

            var currDistortion = VertexNoise.GetNoiseVectorFromRepeatingTexture(GlobalTransform.Translation);
            var distortion = currDistortion * 0.1f + prevDistortion * 0.9f;
            prevDistortion = distortion;
            switch (OrientationType)
            {
                case OrientMode.Spherical:
                    {
                        Matrix bill = Matrix.CreateBillboard(GlobalTransform.Translation, camera.Position, camera.UpVector, null) * Matrix.CreateTranslation(distortion);
                        effect.World = bill;
                        break;
                    }
                case OrientMode.Fixed:
                    {
                        Matrix rotation = GlobalTransform;
                        rotation.Translation = rotation.Translation + distortion;
                        effect.World = rotation;
                        break;
                    }
                case OrientMode.YAxis:
                    {
                        Matrix worldRot = Matrix.CreateConstrainedBillboard(GlobalTransform.Translation, camera.Position, Vector3.UnitY, null, null);
                        worldRot.Translation = worldRot.Translation + distortion;
                        effect.World = worldRot;
                        break;
                    }
            }

            effect.MainTexture = Sheet.GetTexture();

            if (DrawSilhouette)
            {
                Color oldTint = effect.VertexColorTint; 
                effect.VertexColorTint = SilhouetteColor;
                graphicsDevice.DepthStencilState = DepthStencilState.None;
                var oldTechnique = effect.CurrentTechnique;
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.Silhouette];
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Primitive.Render(graphicsDevice);
                }

                graphicsDevice.DepthStencilState = DepthStencilState.Default;
                effect.VertexColorTint = oldTint;
                effect.CurrentTechnique = oldTechnique;
            }

                effect.EnableWind = EnableWind;

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }

            effect.VertexColorTint = origTint;
            effect.EnableWind = false;

            GamePerformance.Instance.StopTrackPerformance("Render - Simple Sprite");
        }
    }

}
