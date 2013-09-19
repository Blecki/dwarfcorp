using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace DwarfCorp
{
    /// <summary>
    /// Simple class representing a geometric object with verticies, textures, and whatever else.
    /// </summary>
    public class GeometricPrimitive
    {
        public static bool ExitGame = false;

        protected short[] m_indices = null;

        // Array of vertex information - contains position, normal and texture data
        protected ExtendedVertex[] m_vertices = null;

        // Stored on the GPU memory to make everything faster when drawing.
        protected VertexBuffer m_vertexBuffer = null;


        public ExtendedVertex[] Vertices { get { return m_vertices; } }
        public VertexBuffer VertexBuffer { get { return m_vertexBuffer; } }

        /// <summary>
        /// Draws the primitive to the screen.
        /// </summary>
        /// <param name="device">GPU to draw with.</param>
        public virtual void Render(GraphicsDevice device)
        {

            if (m_vertices == null || m_vertexBuffer == null || m_vertexBuffer.IsDisposed)
            {
                return;
            }

            device.SetVertexBuffer(m_vertexBuffer);

            if (m_indices != null)
            {
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, m_vertexBuffer.VertexCount, 0, m_vertexBuffer.VertexCount / 3);
            }
            else
            {
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, m_vertices.Length / 3);
            }

        }

        /// <summary>
        /// Draws the primitive to the screen.
        /// </summary>
        /// <param name="device">GPU to draw with.</param>
        public virtual void RenderWireframe(GraphicsDevice device)
        {
            RasterizerState state = new RasterizerState();
            RasterizerState oldState = device.RasterizerState;
            state.FillMode = FillMode.WireFrame;
            device.RasterizerState = state;
            device.SetVertexBuffer(m_vertexBuffer);
            device.DrawPrimitives(PrimitiveType.TriangleList, 0, m_vertices.Length / 3);
            device.RasterizerState = oldState;
        }

        /// <summary>
        /// Resets the vertex buffer object from the verticies.
        /// <param name="device">GPU to draw with.</param>
        public virtual void ResetBuffer(GraphicsDevice device)
        {

            if (!ExitGame)
            {
                if (m_vertexBuffer != null && !m_vertexBuffer.IsDisposed)
                {
                    m_vertexBuffer.Dispose();
                    m_vertexBuffer = null;
                }

                if (m_vertices != null && m_vertices.Length > 0 && !device.IsDisposed)
                {
                    m_vertexBuffer = new VertexBuffer(device, ExtendedVertex.VertexDeclaration, m_vertices.Length, BufferUsage.WriteOnly);
                    m_vertexBuffer.SetData(m_vertices);
                }
            }
        }

    }
}
