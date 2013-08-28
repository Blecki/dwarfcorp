using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace DwarfCorp
{
    public class InstanceList
    {
        public VertexBuffer Model { get; set; }
        private Dictionary<uint, InstanceData> Instances { get; set; }
        private Dictionary<uint, InstancedVertex> Vertices { get; set; }        
        public DynamicVertexBuffer InstanceBuffer { get; set; }
        Mutex rebuildMutex = new Mutex();
        public bool ShouldRebuild { get; set; }
        public Texture2D Texture { get; set; }
        public IndexBuffer Indicies { get; set; }
        private static RasterizerState rasterState = new RasterizerState()
        {
            CullMode = CullMode.None,
        };

        public InstanceList(VertexBuffer model, Texture2D texture)
        {
            Model = model;
            Vertices = new Dictionary<uint, InstancedVertex>();
            Instances = new Dictionary<uint, InstanceData>();
            InstanceBuffer = null;
            ShouldRebuild = true;
            Texture = texture;
          
        }

        int rebuildIndex = 0;
        private void RebuildInstanceBuffer(GraphicsDevice graphics)
        {
            if (Vertices.Count > 0 && InstanceBuffer != null && !InstanceBuffer.IsDisposed)
            {
                InstanceBuffer.Dispose();
                InstanceBuffer = null;
            }

            if (Vertices.Count > 0)
            {
                InstanceBuffer = new DynamicVertexBuffer(graphics, InstancedVertex.VertexDeclaration, Vertices.Count, Microsoft.Xna.Framework.Graphics.BufferUsage.WriteOnly);
                InstancedVertex[] buffer = new InstancedVertex[Vertices.Count];
                int i = 0;
                foreach(InstancedVertex vert in Vertices.Values)
                {
                    buffer[i] = vert;
                    i++;
                }
                InstanceBuffer.SetData(buffer);
                ShouldRebuild = false;
                Console.Out.WriteLine("REBUILDING! " + rebuildIndex);
                rebuildIndex++;
            }

        } 

        public void Render(GraphicsDevice graphics, Effect effect)
        {

            if (Indicies == null)
            {
                Indicies = new IndexBuffer(graphics, IndexElementSize.SixteenBits, Model.VertexCount, BufferUsage.WriteOnly);
                short[] indices = new short[Model.VertexCount];
                for(int i = 0; i < Model.VertexCount; i++)
                {
                    indices[i] = (short)i;
                }
                Indicies.SetData(indices);
            }

            if (ShouldRebuild)
            {
                RebuildInstanceBuffer(graphics);
            }

            if (InstanceBuffer != null && !InstanceBuffer.IsDisposed)
            {
                RasterizerState r = graphics.RasterizerState;
                graphics.RasterizerState = rasterState;
                
                //DepthStencilState origDepthStencil = graphics.DepthStencilState;
                //DepthStencilState newDepthStencil = DepthStencilState.DepthRead;
                //graphics.DepthStencilState = newDepthStencil;

                effect.CurrentTechnique = effect.Techniques["Instanced"];
                graphics.SetVertexBuffers(Model, new VertexBufferBinding(InstanceBuffer, 0, 1));
                graphics.Indices = Indicies;
                effect.Parameters["xTexture"].SetValue(Texture);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                }
              
                graphics.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, Model.VertexCount, 0, Model.VertexCount / 3, InstanceBuffer.VertexCount);
                effect.CurrentTechnique = effect.Techniques["Textured"];

                //graphics.DepthStencilState = origDepthStencil;
                graphics.RasterizerState = r;
            }
        }

        public void UpdateInstance(InstanceData data)
        {
            Vertices[data.ID] = new InstancedVertex(data.Transform, data.Color);
            ShouldRebuild = true;
        }


        public void AddInstance(InstanceData data)
        {
            Instances[data.ID] = (data);
            Vertices[data.ID] = new InstancedVertex(data.Transform, data.Color);
            ShouldRebuild = true;
        }

        public void RemoveInstance(InstanceData data)
        {
            Instances.Remove(data.ID);
            Vertices.Remove(data.ID);
            ShouldRebuild = true;
        }

        public InstanceData AddInstance(Matrix transform, Color color)
        {
            InstanceData toReturn = new InstanceData(transform, color, true);
            AddInstance(toReturn);
            return toReturn;
        }
    }
}
