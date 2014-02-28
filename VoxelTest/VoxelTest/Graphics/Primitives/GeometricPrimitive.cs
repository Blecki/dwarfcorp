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
        protected short[] Indices = null;

        public ExtendedVertex[] Vertices = null;

        [JsonIgnore]
        public VertexBuffer VertexBuffer = null;

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

            if(VertexBuffer == null)
            {
                ResetBuffer(device);
            }

            if(Vertices == null || VertexBuffer == null || VertexBuffer.IsDisposed)
            {
                return;
            }

            device.SetVertexBuffer(VertexBuffer);

            if(Indices != null)
            {
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexBuffer.VertexCount, 0, VertexBuffer.VertexCount / 3);
            }
            else
            {
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, Vertices.Length / 3);
            }
        }

        /// <summary>
        /// Draws the primitive to the screen.
        /// </summary>
        /// <param Name="device">GPU to draw with.</param>
        public virtual void RenderWireframe(GraphicsDevice device)
        {
            RasterizerState state = new RasterizerState();
            RasterizerState oldState = device.RasterizerState;
            state.FillMode = FillMode.WireFrame;
            device.RasterizerState = state;
            device.SetVertexBuffer(VertexBuffer);
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, Vertices.Length / 3);
            device.RasterizerState = oldState;
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

            if(VertexBuffer != null && !VertexBuffer.IsDisposed)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }

            if(Vertices == null || Vertices.Length <= 0 || device == null || device.IsDisposed)
            {
                return;
            }

            VertexBuffer = new VertexBuffer(device, ExtendedVertex.VertexDeclaration, Vertices.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(Vertices);
        }
    }

}