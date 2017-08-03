using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;


namespace DwarfCorp
{
    /// <summary>
    /// Simple class representing a geometric object with verticies, textures, and whatever else.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GeometricPrimitive
    {
        public int IndexCount = 0;
        public int VertexCount = 0;

        [JsonIgnore]
        public IndexBuffer IndexBuffer = null;

        public ushort[] Indexes = new ushort[1024];

        public ExtendedVertex[] Vertices = new ExtendedVertex[1024];

        [JsonIgnore]
        public static Vector3[] FaceDeltas = new Vector3[6];

        [JsonIgnore]
        public VertexBuffer VertexBuffer = null;

        [JsonIgnore]
        protected object VertexLock = new object();

        [JsonIgnore] public RenderTarget2D Lightmap = null;

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            ResetBuffer(GameState.Game.GraphicsDevice);
        }

        private static bool RampSet(RampType ToCheck, RampType For)
        {
            return (int)(ToCheck & For) != 0;
        }

        protected static bool ShouldDrawFace(BoxFace face, RampType neighborRamp, RampType myRamp)
        {
            switch (face)
            {
                case BoxFace.Top:
                case BoxFace.Bottom:
                    return true;
                case BoxFace.Back:
                    return CheckRamps(myRamp, RampType.TopBackLeft, RampType.TopBackRight,
                        neighborRamp, RampType.TopFrontLeft, RampType.TopFrontRight);
                case BoxFace.Front:
                    return CheckRamps(myRamp, RampType.TopFrontLeft, RampType.TopFrontRight,
                        neighborRamp, RampType.TopBackLeft, RampType.TopBackRight);
                case BoxFace.Left:
                    return CheckRamps(myRamp, RampType.TopBackLeft, RampType.TopFrontLeft,
                        neighborRamp, RampType.TopBackRight, RampType.TopFrontRight);
                case BoxFace.Right:
                    return CheckRamps(myRamp, RampType.TopBackRight, RampType.TopFrontRight,
                        neighborRamp, RampType.TopBackLeft, RampType.TopFrontLeft);
                default:
                    return false;
            }
        }

        private static bool CheckRamps(RampType A, RampType A1, RampType A2, RampType B, RampType B1, RampType B2)
        {
            return (!RampSet(A, A1) && RampSet(B, B1)) || (!RampSet(A, A2) && RampSet(B, B2));
        }

        protected static bool IsSideFace(BoxFace face)
        {
            return face != BoxFace.Top && face != BoxFace.Bottom;
        }

        protected static bool IsFaceVisible(TemporaryVoxelHandle voxel, TemporaryVoxelHandle neighbor, BoxFace face)
        {
            return
                !neighbor.IsValid ||
                (neighbor.IsExplored && neighbor.IsEmpty) ||
                (neighbor.Type.IsTransparent && !voxel.Type.IsTransparent) ||
                !neighbor.IsVisible ||
                (neighbor.Type.CanRamp && neighbor.RampType != RampType.None &&
                IsSideFace(face) &&
                ShouldDrawFace(face, neighbor.RampType, voxel.RampType));
        }
        
        /// <summary>
        /// Draws the primitive to the screen.
        /// </summary>
        /// <param Name="device">GPU to draw with.</param>
        public virtual void Render(GraphicsDevice device)
        {
            lock (VertexLock)
            {
#if MONOGAME_BUILD
                device.SamplerStates[0].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[1].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[2].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[3].Filter = TextureFilter.MinLinearMagPointMipLinear;
                device.SamplerStates[4].Filter = TextureFilter.MinLinearMagPointMipLinear;
#endif
                if (Vertices == null || Vertices.Length < 3)
                {
                    return;
                }

                if (VertexBuffer == null ||  VertexBuffer.IsDisposed)
                {
                    ResetBuffer(device);
                }

                if (VertexCount <= 0)
                {
                    VertexCount = Vertices.Length;
                }

                if (IndexCount <= 0 && Indexes != null)
                {
                    IndexCount = Indexes.Length;
                }

                device.SetVertexBuffer(VertexBuffer);
                if (IndexBuffer != null)
                {
                    device.Indices = IndexBuffer;
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexCount, 0, IndexCount / 3);
                }
                else if (Indexes == null || Indexes.Length == 0)
                {
                    device.DrawPrimitives(PrimitiveType.TriangleList, 0, VertexCount/3);
                }
            }
        }

        /// <summary>
        /// Draws the primitive to the screen.
        /// </summary>
        /// <param Name="device">GPU to draw with.</param>
        public virtual void RenderWireframe(GraphicsDevice device)
        {
            lock (VertexLock)
            {
                RasterizerState state = new RasterizerState();
                RasterizerState oldState = device.RasterizerState;
                state.FillMode = FillMode.WireFrame;
                device.RasterizerState = state;
                Render(device);
                device.RasterizerState = oldState;
            }
        }

        

        /// <summary>
        /// Resets the vertex buffer object from the verticies.
        /// <param Name="device">GPU to draw with.</param></summary>
        public virtual void ResetBuffer(GraphicsDevice device)
        {
            if(DwarfGame.ExitGame)
            {
                return;
            }

            lock (VertexLock)
            {
                if (VertexBuffer != null && !VertexBuffer.IsDisposed)
                {
                    VertexBuffer.Dispose();
                    VertexBuffer = null;
                }

                if (IndexBuffer != null && !IndexBuffer.IsDisposed)
                {
                    IndexBuffer.Dispose();
                    IndexBuffer = null;
                }

                if (IndexCount <= 0 && Indexes != null)
                {
                    IndexCount = Indexes.Length;
                }

                if (VertexCount <= 0 && Vertices != null)
                {
                    VertexCount = Vertices.Length;
                }



                if (Vertices != null)
                {
                    VertexBuffer newBuff = new VertexBuffer(device, ExtendedVertex.VertexDeclaration, Vertices.Length,
                        BufferUsage.WriteOnly);
                    newBuff.SetData(Vertices);
                    VertexBuffer = newBuff;
                }

                if (Indexes != null)
                {
                    IndexBuffer newIndexBuff = new IndexBuffer(device, typeof (ushort), Indexes.Length, BufferUsage.None);
                    newIndexBuff.SetData(Indexes);
                    IndexBuffer = newIndexBuff;
                }

            }

        }
    }

}