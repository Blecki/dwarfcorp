using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

namespace DwarfCorp
{
    public class FixedInstanceArray
    {

        public InstancedVertex[] Vertices { get; set;}
        private MinBag<InstanceData> SortedData { get; set; }
        private List<InstanceData> Data { get; set;}
        private List<InstanceData> Additions { get; set; }
        private List<InstanceData> Removals { get; set; }
        private int numInstances = 0;
        public int NumInstances { get {return numInstances;}  set{ SetNumInstances(value);}}
        public DynamicVertexBuffer InstanceBuffer { get; set; }
        public VertexBuffer Model { get; set; }
        public Texture2D Texture { get; set; }
        public IndexBuffer Indicies { get; set; }
        public bool ShouldRebuild { get; set; }
        public string Name { get; set; }
        private Mutex DataLock { get; set; }
        private static RasterizerState rasterState = new RasterizerState()
        {
            CullMode = CullMode.None,
        };
        public Camera camera;
        public BlendState BlendMode { get; set;}
        public float CullDistance = 100 * 100;

        public void CreateDepths(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            BoundingFrustum frust = camera.GetFrustrum();
            Vector3 forward = frust.Near.Normal;
            //BoundingSphere conservativeSphere = new BoundingSphere(Vector3.Zero, 2);
            Vector3 z = Vector3.Zero;
            foreach(InstanceData instance in Data)
            {
                z = instance.Transform.Translation - camera.Position;
                instance.Depth = z.LengthSquared();

                if (instance.Depth < CullDistance)
                {
                    //conservativeSphere.Center = instance.Transform.Translation;
                    //if (!frust.Intersects(conservativeSphere))

                    // Half plane test. Faster. Much less accurate.
                    if (Vector3.Dot(z, forward) > 0)
                    {
                        instance.Depth *= 100;
                    }
                }
                else
                {
                    instance.Depth *= 100;
                }
            }
        }

        private class DepthCompareHelper : IComparer<InstanceData>
        {
            int IComparer<InstanceData>.Compare(InstanceData A, InstanceData B)
            {

                if (A == B)
                {
                    return 0;
                }


                if (A.Depth <= B.Depth)
                {
                    if (B.Depth > A.Depth)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else return 1;
            }
        }

        Timer sortTimer = new Timer(0.1f, false);

        public void SortDistances()
        {
            CreateDepths(camera);

            SortedData.Clear();
            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i].Depth < CullDistance)
                {
                    SortedData.Add(Data[i], Data[i].Depth);
                }
            }
        }

        public FixedInstanceArray(string name, VertexBuffer model, Texture2D texture, int numInstances, BlendState blendMode)
        {
            CullDistance = (GameSettings.Default.ChunkDrawDistance * GameSettings.Default.ChunkDrawDistance) - 40;
            Name = name;
            Model = model;
            Vertices = new InstancedVertex[numInstances];
            Data = new List<InstanceData>();
            Additions = new List<InstanceData>();
            Removals = new List<InstanceData>();

            SortedData = new MinBag<InstanceData>(numInstances);
            NumInstances = numInstances;

            RebuildVertices();

            InstanceBuffer = null;
            ShouldRebuild = true;
            Texture = texture;
            DataLock = new Mutex();

            BlendMode = blendMode;
        }


        public void DeleteNulls()
        {
            for (int j = 0; j < Data.Count; j++)
            {
                if (Data[j] == null)
                {
                    Data.RemoveAt(j);
                    j--;
                }
            }

        }

        public void RebuildVertices()
        {
            for(int i = 0; i < SortedData.Data.Count; i++)
            {
                Vertices[i] = new InstancedVertex(SortedData.Data[i].Transform, SortedData.Data[i].Color);
            }
        }

        public void RebuildInstanceBuffer(GraphicsDevice graphics)
        {

            if (Indicies == null)
            {
                Indicies = new IndexBuffer(graphics, IndexElementSize.SixteenBits, Model.VertexCount, BufferUsage.WriteOnly);
                short[] indices = new short[Model.VertexCount];
                for (int i = 0; i < Model.VertexCount; i++)
                {
                    indices[i] = (short)i;
                }
                Indicies.SetData(indices);
            }


            if ( InstanceBuffer != null && !InstanceBuffer.IsDisposed)
            {
                InstanceBuffer.Dispose();
                InstanceBuffer = null;
            }

            InstanceBuffer = new DynamicVertexBuffer(graphics, InstancedVertex.VertexDeclaration, NumInstances, Microsoft.Xna.Framework.Graphics.BufferUsage.WriteOnly);
            ShouldRebuild = false;
        }

        public void Update(GameTime time, Camera cam, GraphicsDevice graphics)
        {
            bool rebuildVertices = true;
            DeleteNulls();
            sortTimer.Update(time);
            if (Indicies == null)
            {
                Indicies = new IndexBuffer(graphics, IndexElementSize.SixteenBits, Model.VertexCount, BufferUsage.WriteOnly);
                short[] indices = new short[Model.VertexCount];
                for (int i = 0; i < Model.VertexCount; i++)
                {
                    indices[i] = (short)i;
                }
                Indicies.SetData(indices);
            }
            if (ShouldRebuild)
            {
                RebuildInstanceBuffer(graphics);
            }

            if (sortTimer.HasTriggered)
            {
                SortDistances();
                sortTimer.Reset(sortTimer.TargetTimeSeconds);
            }

            if (rebuildVertices)
            {
                RebuildVertices();
            }


            if (rebuildVertices)
            {
                InstanceBuffer.SetData(Vertices);
            }


            AddRemove();
        }

        public void Render(GraphicsDevice graphics, Effect effect, Camera cam, bool rebuildVertices)
        {
            camera = cam;

            

            if (SortedData.Data.Count > 0 && InstanceBuffer != null && !InstanceBuffer.IsDisposed)
            {
                RasterizerState r = graphics.RasterizerState;
                graphics.RasterizerState = rasterState;
;
                effect.CurrentTechnique = effect.Techniques["Textured"];
                int light = 0;
                if (GameSettings.Default.CursorLightEnabled) light = 1;
                effect.Parameters["xEnableLighting"].SetValue(light);
                //graphics.SetVertexBuffers(Model, new VertexBufferBinding(InstanceBuffer, 0, 1));
                graphics.SetVertexBuffer(Model);
                graphics.Indices = Indicies;

                BlendState blendState = graphics.BlendState;
                graphics.BlendState = BlendMode;

                effect.Parameters["xTexture"].SetValue(Texture);

                for (int i = 0; i < SortedData.Data.Count; i++)
                {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                        effect.Parameters["xWorld"].SetValue(SortedData.Data[i].Transform);
                        effect.Parameters["xTint"].SetValue(SortedData.Data[i].Color.ToVector4());
                        graphics.DrawPrimitives(PrimitiveType.TriangleList, 0, Model.VertexCount / 3);
                        //graphics.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, Model.VertexCount, 0, Model.VertexCount / 3, SortedData.Data.Count);

                    }
                }


  
                effect.CurrentTechnique = effect.Techniques["Textured"];


                graphics.RasterizerState = r;
                graphics.BlendState = blendState;
            }


        }

        public void Add(InstanceData data)
        {
            //DataLock.WaitOne();
            Additions.Add(data);
            //DataLock.ReleaseMutex();
        }

        public void Remove(InstanceData data)
        {
            //DataLock.WaitOne();
            Removals.Add(data);
            //DataLock.ReleaseMutex();
        }

        private void AddRemove()
        {
            //DataLock.WaitOne();
            for (int i = 0; i < Additions.Count; i++)
            {
                Data.Add(Additions[i]);
            }

            for (int j = 0; j < Removals.Count; j++)
            {
                Data.Remove(Removals[j]);
            }

            Additions.Clear();
            Removals.Clear();
            //DataLock.ReleaseMutex();
        }

        public void SetNumInstances(int nInstances)
        {
            numInstances = nInstances;
            InstancedVertex[] oldVertices = Vertices;
            Vertices = new InstancedVertex[nInstances];

            for (int i = 0; i < Math.Min(oldVertices.Length, nInstances); i++)
            {
                Vertices[i] = oldVertices[i];
            }

            for (int j = oldVertices.Length; j < nInstances; j++)
            {
                Vertices[j] = new InstancedVertex();
            }

           
        }


    

    }
}
