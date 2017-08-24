using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A fixed instance array draws the closest K instances of a model. 
    /// It constantly sorts and frustrum culls instances en masse.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class FixedInstanceArray
    {
        private MinBag<InstanceData> SortedData { get; set; }
        private List<InstanceData> Data { get; set; }
        private List<InstanceData> Additions { get; set; }
        private List<InstanceData> Removals { get; set; }
        private int numInstances = 0;
        private int numActiveInstances = 0;
        public int NumInstances
        {
            get { return numInstances; }
            set { SetNumInstances(value); }
        }

        [JsonIgnore]
        public GeometricPrimitive Model { get; set; }
        public Texture2D Texture { get; set; }
        public bool ShouldRebuild { get; set; }
        public string Name { get; set; }
        private Mutex DataLock { get; set; }

        private static RasterizerState rasterState = new RasterizerState()
        {
            CullMode = CullMode.None,
        };

        public Camera Camera;

        public BlendState BlendMode { get; set; }
        public bool HasSelectionBuffer = true;

        public float CullDistance = 100 * 100;

        private DynamicVertexBuffer instanceBuffer;
        private InstancedVertex[] instanceVertexes;
        private static bool HardwareInstancingSupported = true;

        public bool EnableWind = false;

        public void Clear()
        {
            Data.Clear();
            SortedData.Clear();
        }

        public void CreateDepths(Camera inputCamera)
        {
            if (inputCamera == null)
            {
                return;
            }

            BoundingFrustum frust = inputCamera.GetFrustrum();
            BoundingBox box = MathFunctions.GetBoundingBox(frust.GetCorners());
            Vector3 forward = frust.Near.Normal;

            Vector3 z = Vector3.Zero;
            foreach (InstanceData instance in Data)
            {
                z = instance.Transform.Translation - inputCamera.Position;
                instance.Depth = z.LengthSquared();

                if (instance.Depth < CullDistance)
                {

                    // Half plane test. Faster. Much less accurate.
                    //if (Vector3.Dot(z, forward) > 0)
                    if(box.Contains(instance.Transform.Translation) != ContainmentType.Contains)
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

        private Timer sortTimer = new Timer(0.1f, false);

        public void SortDistances()
        {
            CreateDepths(Camera);

            SortedData.Clear();
            foreach (InstanceData t in Data.Where(t => t.Depth < CullDistance))
            {
                SortedData.Add(t, t.Depth);
            }
        }



        public FixedInstanceArray()
        {
            Data = new List<InstanceData>();
            Additions = new List<InstanceData>();
            Removals = new List<InstanceData>();
        }

        public FixedInstanceArray(string name, GeometricPrimitive model, Texture2D texture, int numInstances, BlendState blendMode)
        {
            CullDistance = (GameSettings.Default.ChunkDrawDistance * GameSettings.Default.ChunkDrawDistance) - 40;
            Name = name;
            Model = model;
            Data = new List<InstanceData>();
            Additions = new List<InstanceData>();
            Removals = new List<InstanceData>();

            SortedData = new MinBag<InstanceData>(numInstances);
            NumInstances = numInstances;

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

        public void Update(DwarfTime time, Camera cam, GraphicsDevice graphics, int maxViewingLevel)
        {
            if (DwarfGame.ExitGame)
            {
                return;
            }

            // If we have no data objects at all and we aren't going to be adding new ones there's no point in running
            // any more code here.  The only way to get Data.Count to be zero is after processing AddRemove in the code block
            // which means numActiveInstances will properly be set to zero.  The moment an Addition happens we'll pass this check
            // and start outputting again.
            if (Data.Count + Additions.Count == 0) return;

            DeleteNulls();
            sortTimer.Update(time);

            if (sortTimer.HasTriggered)
            {
                SortDistances();
                sortTimer.Reset(sortTimer.TargetTimeSeconds);
            }

            // There's no reason to go into this function and deal with the Mutex if we have nothing to do.
            if (Additions.Count + Removals.Count > 0)
                AddRemove();

            if (HardwareInstancingSupported)
            {
                if (instanceVertexes == null)
                {
                    instanceVertexes = new InstancedVertex[numInstances];
                }
                int j = 0;
                foreach (InstanceData t in SortedData.Data)
                {
                    if (t.ShouldDraw && t.Transform.Translation.Y < maxViewingLevel)
                    {
                        instanceVertexes[j].Transform = t.Transform;
                        instanceVertexes[j].Color = t.Color;
                        instanceVertexes[j].SelectionBufferColor = t.SelectionBufferColor;
                        j++;
                    }
                }
                numActiveInstances = j;
            }
            else
            {
                instanceVertexes = null;
                numActiveInstances = SortedData.Data.Count(data => data.ShouldDraw);
            }
        }


        private void DrawInstanced(GraphicsDevice graphics, Shader effect, Camera cam)
        {
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphics.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                    Model.VertexCount, 0,
                    Model.Indexes.Length / 3,
                    numActiveInstances);
            }
        }

        private void DrawNonInstanced(GraphicsDevice graphics, Shader effect, Camera cam)
        {
            bool hasIndex = Model.IndexBuffer != null;
            graphics.SetVertexBuffer(Model.VertexBuffer);

            int i = 0;
            foreach (InstanceData instance in SortedData.Data)
            {
                if (!instance.ShouldDraw || i > numActiveInstances) continue;
                i++;
                effect.World = instance.Transform;
                effect.LightRampTint = instance.Color;
                effect.SelectionBufferColor = instance.SelectionBufferColor.ToVector4();
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    if (!hasIndex)
                    {
                        graphics.DrawPrimitives(PrimitiveType.TriangleList, 0, Model.VertexBuffer.VertexCount / 3);
                    }
                    else
                    {
                        graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0,
                            Model.VertexBuffer.VertexCount, 0, Model.IndexBuffer.IndexCount / 3);
                    }
                }
            }
        }

        public void Render(
            GraphicsDevice graphics,
            Shader effect,
            Camera cam, 
            string mode)
        {
            effect.EnableWind = EnableWind;
            Camera = cam;

            if (HardwareInstancingSupported && instanceBuffer == null)
            {
                instanceBuffer = new DynamicVertexBuffer(graphics, InstancedVertex.VertexDeclaration, numInstances,
                    BufferUsage.None);
            }

            if (SortedData.Data.Count > 0 && numActiveInstances > 0)
            {
                graphics.RasterizerState = rasterState;

                effect.CurrentTechnique = effect.Techniques[mode];
                effect.EnableLighting = true;
                effect.VertexColorTint = Color.White;

                if (Model.VertexBuffer == null || Model.IndexBuffer == null)
                {
                    Model.ResetBuffer(graphics);
                }

                bool hasIndex = Model.IndexBuffer != null;
                graphics.Indices = Model.IndexBuffer;

                BlendState blendState = graphics.BlendState;
                graphics.BlendState = BlendMode;

                effect.MainTexture = Texture;
                effect.LightRampTint = Color.White;

                if (HardwareInstancingSupported)
                {
                    instanceBuffer.SetData(instanceVertexes, 0, SortedData.Data.Count, SetDataOptions.Discard);

                    graphics.SetVertexBuffers(Model.VertexBuffer, new VertexBufferBinding(instanceBuffer, 0, 1));

                    try
                    {
                        DrawInstanced(graphics, effect, cam);
                    }
                    catch (NoSuitableGraphicsDeviceException exception)
                    {
                        System.Console.WriteLine(exception.ToString());
                        HardwareInstancingSupported = false;
                    }
                }
                else
                {
                    // Fallback case when hardware instancing is not supported
                    effect.SetTexturedTechnique();
                    DrawNonInstanced(graphics, effect, cam);
                }

                effect.SetTexturedTechnique();
                effect.World = Matrix.Identity;
                graphics.BlendState = blendState;
            }
            effect.EnableWind = false;
        }


        public void Render(GraphicsDevice graphics, Shader effect, Camera cam)
        {
            Render(graphics, effect, cam, Shader.InstancedTechniques[effect.CurrentNumLights]);
        }

        public void RenderSelectionBuffer(GraphicsDevice graphics, Shader effect, Camera cam, bool rebuildVertices)
        {
            if (!HasSelectionBuffer || Model == null || Model.VertexCount < 3 || numActiveInstances < 1)
            {
                return;
            }

            effect.MainTexture = Texture;
            effect.LightRampTint = Color.White;
            effect.VertexColorTint = Color.White;
            graphics.Indices = Model.IndexBuffer;

            if (HardwareInstancingSupported)
            {
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.SelectionBufferInstanced];

                graphics.SetVertexBuffers(Model.VertexBuffer, new VertexBufferBinding(instanceBuffer, 0, 1));
                try
                {
                    DrawInstanced(graphics, effect, cam);
                }
                catch (NoSuitableGraphicsDeviceException exception)
                {
                    System.Console.WriteLine(exception.ToString());
                    HardwareInstancingSupported = false;
                }
            }
            else
            {
                // Fallback case when hardware instancing is not supported
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.SelectionBuffer];
                DrawNonInstanced(graphics, effect, cam);
            }


        }

        public void Add(InstanceData data)
        {
            DataLock.WaitOne();
            Additions.Add(data);
            DataLock.ReleaseMutex();
        }

        public void Remove(InstanceData data)
        {
            DataLock.WaitOne();
            Removals.Add(data);
            DataLock.ReleaseMutex();
        }

        private void AddRemove()
        {
            DataLock.WaitOne();
            foreach (InstanceData t in Additions)
            {
                Data.Add(t);
            }

            foreach (InstanceData t in Removals)
            {
                Data.Remove(t);
            }

            if (Removals.Count > 0 || Additions.Count > 0)
            {
                SortDistances();
            }

            Additions.Clear();
            Removals.Clear();
            DataLock.ReleaseMutex();
        }

        public void SetNumInstances(int nInstances)
        {
            numInstances = nInstances;
        }
    }

}
