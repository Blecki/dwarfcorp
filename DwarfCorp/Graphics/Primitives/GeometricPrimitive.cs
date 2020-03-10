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
    public class GeometricPrimitive : IDisposable
    {
        public int IndexCount = 0;
        public int VertexCount = 0;

        public float Width { get; set; }
        public float Height { get; set; }
        public NamedImageFrame Texture { get; set; }

        [JsonIgnore]
        public DynamicIndexBuffer IndexBuffer = null;

        // Todo: Store shorts instead
        public ushort[] Indexes = new ushort[6];

        public ExtendedVertex[] Vertices = new ExtendedVertex[6];

        [JsonIgnore]
        public DynamicVertexBuffer VertexBuffer = null;

        [JsonIgnore]
        protected object VertexLock = new object(); // Todo: Need this?

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
                    return;

                if (VertexBuffer == null ||  VertexBuffer.IsDisposed || VertexBuffer.GraphicsDevice.IsDisposed || VertexBuffer.IsContentLost)
                    ResetBuffer(device);

                if (VertexBuffer == null)
                    return;

                if (VertexCount <= 0)
                    VertexCount = Vertices.Length;

                if (IndexCount <= 0 && Indexes != null)
                    IndexCount = Indexes.Length;

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
                    VertexBuffer.Dispose();
                VertexBuffer = null;

                if (IndexBuffer != null && !IndexBuffer.IsDisposed)
                    IndexBuffer.Dispose();
                IndexBuffer = null;

                if (IndexCount <= 0 && Indexes != null)
                    IndexCount = Indexes.Length;

                if (VertexCount <= 0 && Vertices != null)
                    VertexCount = Vertices.Length;

                if (Vertices != null)
                {
                    try
                    {
                        VertexBuffer = new DynamicVertexBuffer(device, ExtendedVertex.VertexDeclaration, Vertices.Length, BufferUsage.WriteOnly);
                        VertexBuffer.SetData(Vertices, 0, VertexCount);
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
                        IndexBuffer = new DynamicIndexBuffer(device, typeof(ushort), Indexes.Length, BufferUsage.None);
                        IndexBuffer.SetData(Indexes, 0, IndexCount);
                    }
                    catch (Exception exception)
                    {
                        Console.Out.WriteLine(exception.ToString());
                        IndexBuffer = null;
                    }
                }

            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                if (IndexBuffer != null)
                    IndexBuffer.Dispose();
                IndexBuffer = null;

                if (VertexBuffer != null)
                    VertexBuffer.Dispose();
                VertexBuffer = null;

                Vertices = null;
                Indexes = null;

                disposedValue = true;
            }
        }

        ~GeometricPrimitive()
        {
           // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
           Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

}