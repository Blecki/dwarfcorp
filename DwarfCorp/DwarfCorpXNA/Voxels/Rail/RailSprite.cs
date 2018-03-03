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
        private const float sqrt2 = 1.41421356237f;
        private SpriteSheet Sheet;
        private Point Frame;
        private RawPrimitive Primitive;
        public float[] VertexHeightOffsets = new float[] { 0.0f, 0.0f, 0.0f, 0.0f };
        private Orientation Orientation;

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
        
        public void SetFrame(Point Frame, Orientation Orientation)
        {
            this.Frame = Frame;
            this.Orientation = Orientation;
            ResetPrimitive();
        }

        public void ResetPrimitive()
        {
            Primitive = null;
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

        private float AngleBetweenVectors(Vector2 A, Vector2 B)
        {
            A.Normalize();
            B.Normalize();
            float DotProduct = Vector2.Dot(A, B);
            DotProduct = MathHelper.Clamp(DotProduct, -1.0f, 1.0f);
            float Angle = (float)System.Math.Acos(DotProduct);
            if (CrossZ(A, B) < 0) return -Angle;
            return Angle;
        }

        private float CrossZ(Vector2 A, Vector2 B)
        {
            return (B.Y * A.X) - (B.X * A.Y);
        }

        private float Sign(float F)
        {
            if (F < 0) return -1.0f;
            return 1.0f;
        }

        public override void Render(DwarfTime gameTime,
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
                var bounds = Vector4.Zero;
                var uvs = Sheet.GenerateTileUVs(Frame, out bounds);

                var transform = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Orientation);

                Primitive = new RawPrimitive();
                Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(-0.5f, VertexHeightOffsets[0], 0.5f), transform), Color.White, Color.White, uvs[0], bounds));
                Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(0.5f, VertexHeightOffsets[1], 0.5f), transform), Color.White, Color.White, uvs[1], bounds));
                Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(0.5f, VertexHeightOffsets[2], -0.5f), transform), Color.White, Color.White, uvs[2], bounds));
                Primitive.AddVertex(new ExtendedVertex(Vector3.Transform(new Vector3(-0.5f, VertexHeightOffsets[3], -0.5f), transform), Color.White, Color.White, uvs[3], bounds));
                Primitive.AddIndicies(new short[] { 0, 1, 3, 1, 2, 3 });

                var bumperBackBounds = Vector4.Zero;
                var bumperBackUvs = Sheet.GenerateTileUVs(new Point(0, 5), out bumperBackBounds);
                var bumperFrontBounds = Vector4.Zero;
                var bumperFrontUvs = Sheet.GenerateTileUVs(new Point(1, 5), out bumperFrontBounds);
                var bumperSideBounds = Vector4.Zero;
                var bumperSideUvs = Sheet.GenerateTileUVs(new Point(2, 5), out bumperSideBounds);

                var railEntity = Parent as RailEntity;
                foreach (var connection in railEntity.GetTransformedConnections())
                {
                    var matchingNeighbor = railEntity.NeighborRails.FirstOrDefault(n => (n.Position - connection.Item1).LengthSquared() < 0.001f);
                    if (matchingNeighbor == null)
                    {
                        var bumperOffset = connection.Item1 - GlobalTransform.Translation;
                        var bumperGap = Vector3.Normalize(bumperOffset) * 0.1f;
                        var bumperAngle = AngleBetweenVectors(new Vector2(bumperOffset.X, bumperOffset.Z), new Vector2(0, 0.5f));

                        var xDiag = bumperOffset.X < -0.001f || bumperOffset.X > 0.001f;
                        var zDiag = bumperOffset.Z < -0.001f || bumperOffset.Z > 0.001f;

                        if (xDiag && zDiag)
                        {
                            var y = bumperOffset.Y;
                            bumperOffset *= sqrt2;
                            bumperOffset.Y = y;

                            var endBounds = Vector4.Zero;
                            var endUvs = Sheet.GenerateTileUVs(new Point(6, 2), out endBounds);
                            Primitive.AddQuad(
                                Matrix.CreateRotationY((float)Math.PI * 1.25f)
                                * Matrix.CreateRotationY(bumperAngle)
                                * Matrix.CreateTranslation(new Vector3(Sign(bumperOffset.X), 0.0f, Sign(bumperOffset.Z))),
                                Color.White, Color.White, endUvs, endBounds);
                        }

                        Primitive.AddQuad(
                            Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                            * Matrix.CreateTranslation(0.0f, 0.3f, -0.2f)
                            * Matrix.CreateRotationY(bumperAngle)
                            * Matrix.CreateTranslation(bumperOffset + bumperGap),
                            Color.White, Color.White, bumperBackUvs, bumperBackBounds);

                        Primitive.AddQuad(
                            Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                            * Matrix.CreateTranslation(0.0f, 0.3f, -0.2f)
                            * Matrix.CreateRotationY(bumperAngle)
                            * Matrix.CreateTranslation(bumperOffset),
                            Color.White, Color.White, bumperFrontUvs, bumperFrontBounds);

                        Primitive.AddQuad(
                            Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                            * Matrix.CreateRotationY(-(float)Math.PI * 0.5f)
                            * Matrix.CreateTranslation(0.3f, 0.3f, 0.18f)
                            * Matrix.CreateRotationY(bumperAngle)
                            * Matrix.CreateTranslation(bumperOffset),
                            Color.White, Color.White, bumperSideUvs, bumperSideBounds);

                        Primitive.AddQuad(
                            Matrix.CreateRotationX(-(float)Math.PI * 0.5f)
                            * Matrix.CreateRotationY(-(float)Math.PI * 0.5f)
                            * Matrix.CreateTranslation(-0.3f, 0.3f, 0.18f)
                            * Matrix.CreateRotationY(bumperAngle)
                            * Matrix.CreateTranslation(bumperOffset),
                            Color.White, Color.White, bumperSideUvs, bumperSideBounds);
                    }
                }
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
                Primitive.Render(graphicsDevice);
            }

            effect.VertexColorTint = origTint;
        }
    }

}
