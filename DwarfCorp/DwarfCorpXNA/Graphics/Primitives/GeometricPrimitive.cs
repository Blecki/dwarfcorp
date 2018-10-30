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

        public float Width { get; set; }
        public float Height { get; set; }
        public Texture2D Texture { get; set; }

        [JsonIgnore]
        public DynamicIndexBuffer IndexBuffer = null;

        // Todo: Store shorts instead
        public ushort[] Indexes = new ushort[6];

        public ExtendedVertex[] Vertices = new ExtendedVertex[6];

        [JsonIgnore]
        public DynamicVertexBuffer VertexBuffer = null;

        [JsonIgnore]
        protected object VertexLock = new object();

        [JsonIgnore] public RenderTarget2D Lightmap = null;

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            ResetBuffer(GameState.Game.GraphicsDevice);
        }

        /// <summary>
        /// Draws the primitive to the screen.
        /// </summary>
        /// <param Name="device">GPU to draw with.</param>
        public virtual void Render(GraphicsDevice device)
        {
            // Todo: Redesign so locking is not required.
            //lock (VertexLock)
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

                if (VertexBuffer == null ||  VertexBuffer.IsDisposed || VertexBuffer.GraphicsDevice.IsDisposed || VertexBuffer.IsContentLost)
                {
                    ResetBuffer(device);
                }

                if (VertexBuffer == null)
                {
                    return;
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
            //if(DwarfGame.ExitGame)
            //{
            //    return;
            //}

            //lock (VertexLock)
            {
                if (VertexBuffer != null && !VertexBuffer.IsDisposed)
                {
                    VertexBuffer.Dispose();
                }
                VertexBuffer = null;

                if (IndexBuffer != null && !IndexBuffer.IsDisposed)
                {
                    IndexBuffer.Dispose();
                }
                IndexBuffer = null;

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
                    try
                    {
                        DynamicVertexBuffer newBuff = new DynamicVertexBuffer(device, ExtendedVertex.VertexDeclaration, Vertices.Length,
                            BufferUsage.WriteOnly);
                        newBuff.SetData(Vertices, 0, VertexCount);
                        VertexBuffer = newBuff;
                    }
                    catch (Exception exception)
                    {
                        Console.Out.WriteLine(exception.ToString());
                        VertexBuffer = null;
                    }
                }

                if (Indexes != null)
                {
                    try
                    {
                        DynamicIndexBuffer newIndexBuff = new DynamicIndexBuffer(device, typeof(ushort), Indexes.Length, BufferUsage.None);
                        newIndexBuff.SetData(Indexes, 0, IndexCount);
                        IndexBuffer = newIndexBuff;
                    }
                    catch (Exception exception)
                    {
                        Console.Out.WriteLine(exception.ToString());
                        IndexBuffer = null;
                    }
                }

            }

        }
    }

}