using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public class InstanceRenderer
    {
        private const int InstanceQueueSize = 64;
        private DynamicVertexBuffer InstanceBuffer;

        private class InstanceGroup
        {
            public InstanceRenderData RenderData;
            public InstancedVertex[] Instances = new InstancedVertex[InstanceQueueSize];
            public int InstanceCount = 0;
        }

        private Dictionary<string, InstanceGroup> InstanceTypes = new Dictionary<string, InstanceGroup>();

        public InstanceRenderer(GraphicsDevice Device, ContentManager Content)
        {
            List<Matrix> treeTransforms = new List<Matrix>();
            treeTransforms.Add(Matrix.Identity);
            treeTransforms.Add(Matrix.CreateRotationY(1.57f));

            List<Color> treeTints = new List<Color>();
            treeTints.Add(Color.White);
            treeTints.Add(Color.White);

            foreach (var member in typeof(ContentPaths.Entities.Plants).GetFields())
                if (member.IsStatic && member.FieldType == typeof(String))
                {
                    bool isMote = false;
                    foreach (var attribute in member.GetCustomAttributes(false))
                        if (attribute is MoteAttribute)
                            isMote = true;

                    InstanceTypes.Add(member.Name, new InstanceGroup
                    {
                        RenderData = new InstanceRenderData
                        {
                            Model = PrimitiveLibrary.BatchBillboardPrimitives[member.Name],
                            BlendMode = BlendState.NonPremultiplied,
                            EnableWind = true,
                            RenderInSelectionBuffer = !isMote,
                            EnableGhostClipping = !isMote
                        }
                    });

                }
        }

        public enum RenderMode
        {
            Normal,
            SelectionBuffer
        }

        public void RenderInstance(NewInstanceData Instance,
            GraphicsDevice Device, Shader Effect, Camera Camera, RenderMode Mode)
        {
            if(Instance.Type == null || !InstanceTypes.ContainsKey(Instance.Type))
                return;

            var group = InstanceTypes[Instance.Type];

            if (Mode == RenderMode.SelectionBuffer && !group.RenderData.RenderInSelectionBuffer)
                return;

            group.Instances[group.InstanceCount] = new InstancedVertex
            {
                Transform = Instance.Transform,
                Color = Instance.Color,
                SelectionBufferColor = Instance.SelectionBufferColor
            };

            group.InstanceCount += 1;
            if (group.InstanceCount == InstanceQueueSize)
                Flush(group, Device, Effect, Camera, Mode);
        }

        public void Flush(
            GraphicsDevice Device,
            Shader Effect,
            Camera Camera,
            RenderMode Mode)
        {
            foreach (var group in InstanceTypes)
            {
                if (group.Value.InstanceCount != 0)
                    Flush(group.Value, Device, Effect, Camera, Mode);
            }
        }

        private void Flush(
            InstanceGroup Group,
            GraphicsDevice Device,
            Shader Effect,
            Camera Camera,
            RenderMode Mode)
        {
            if (InstanceBuffer == null)
            {
                InstanceBuffer = new DynamicVertexBuffer(Device, InstancedVertex.VertexDeclaration, InstanceQueueSize, BufferUsage.None);
            }

            Effect.EnableWind = Group.RenderData.EnableWind;
            Device.RasterizerState = new RasterizerState { CullMode = CullMode.None };
            if (Mode == RenderMode.Normal)
                Effect.SetInstancedTechnique();
            else
                Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.SelectionBufferInstanced];
            Effect.EnableLighting = true;
            Effect.VertexColorTint = Color.White;

            if (Group.RenderData.Model.VertexBuffer == null || Group.RenderData.Model.IndexBuffer == null ||
                Group.RenderData.Model.VertexBuffer.IsContentLost || Group.RenderData.Model.IndexBuffer.IsContentLost)
                Group.RenderData.Model.ResetBuffer(Device);

            bool hasIndex = Group.RenderData.Model.IndexBuffer != null;
            Device.Indices = Group.RenderData.Model.IndexBuffer;

            BlendState blendState = Device.BlendState;
            Device.BlendState = Mode == RenderMode.Normal ? Group.RenderData.BlendMode : BlendState.Opaque;

            Effect.MainTexture = Group.RenderData.Model.Texture;
            Effect.LightRampTint = Color.White;

            InstanceBuffer.SetData(Group.Instances, 0, Group.InstanceCount, SetDataOptions.Discard);
            Device.SetVertexBuffers(Group.RenderData.Model.VertexBuffer, new VertexBufferBinding(
                InstanceBuffer, 0, 1));

            var ghostEnabled = Effect.GhostClippingEnabled;
            Effect.GhostClippingEnabled = Group.RenderData.EnableGhostClipping && ghostEnabled;
            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                    Group.RenderData.Model.VertexCount, 0,
                    Group.RenderData.Model.Indexes.Length / 3,
                    Group.InstanceCount);
            }
            Effect.GhostClippingEnabled = ghostEnabled;

            Effect.SetTexturedTechnique();
            Effect.World = Matrix.Identity;
            Device.BlendState = blendState;
            Effect.EnableWind = false;

            Group.InstanceCount = 0;
        }
    }
}