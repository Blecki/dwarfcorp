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
    public class PrimitiveInstanceGroup : InstanceGroup
    {
        private const int InstanceQueueSize = 64;

        public InstanceRenderData RenderData;
        private InstancedVertex[] Instances = new InstancedVertex[InstanceQueueSize];
        private int InstanceCount = 0;
        private DynamicVertexBuffer InstanceBuffer = null;

        public PrimitiveInstanceGroup()
        {
        }

        public override void Initialize()
        {
            if (RenderData.Model == null)
                RenderData.Model = PrimitiveLibrary.Primitives[RenderData.PrimitiveName];
        }

        public override void RenderInstance(NewInstanceData Instance, GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (Mode == InstanceRenderMode.SelectionBuffer && !RenderData.RenderInSelectionBuffer)
                return;

            Instances[InstanceCount] = new InstancedVertex
            {
                Transform = Instance.Transform,
                LightRamp = Instance.LightRamp,
                SelectionBufferColor = Instance.SelectionBufferColor,
                VertexColorTint = Instance.VertexColorTint
            };

            InstanceCount += 1;
            if (InstanceCount == InstanceQueueSize)
                Flush(Device, Effect, Camera, Mode);
        }

        public override void Flush(GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (InstanceCount == 0) return;

            if (InstanceBuffer == null || InstanceBuffer.IsDisposed || InstanceBuffer.IsContentLost || InstanceBuffer.GraphicsDevice.IsDisposed)
                InstanceBuffer = new DynamicVertexBuffer(Device, InstancedVertex.VertexDeclaration, InstanceQueueSize, BufferUsage.None);

            Effect.EnableWind = RenderData.EnableWind;
            Device.RasterizerState = new RasterizerState { CullMode = CullMode.None };
            if (Mode == InstanceRenderMode.Normal)
                Effect.SetInstancedTechnique();
            else
                Effect.CurrentTechnique = Effect.Techniques[Shader.Technique.SelectionBufferInstanced];
            Effect.EnableLighting = true;
            Effect.VertexColorTint = Color.White;

            if (RenderData.Model.VertexBuffer == null || RenderData.Model.IndexBuffer == null ||
                (RenderData.Model.VertexBuffer != null && RenderData.Model.VertexBuffer.IsContentLost) ||
                (RenderData.Model.IndexBuffer != null && RenderData.Model.IndexBuffer.IsContentLost))
                RenderData.Model.ResetBuffer(Device);

            bool hasIndex = RenderData.Model.IndexBuffer != null;
            Device.Indices = RenderData.Model.IndexBuffer;

            BlendState blendState = Device.BlendState;
            Device.BlendState = Mode == InstanceRenderMode.Normal ? BlendState.NonPremultiplied : BlendState.Opaque;

            Effect.MainTexture = RenderData.Model.Texture.SafeGetImage();
            Effect.LightRamp = Color.White;

            InstanceBuffer.SetData(Instances, 0, InstanceCount, SetDataOptions.Discard);
            Device.SetVertexBuffers(new VertexBufferBinding(RenderData.Model.VertexBuffer), new VertexBufferBinding(InstanceBuffer, 0, 1));

            var ghostEnabled = Effect.GhostClippingEnabled;
            Effect.GhostClippingEnabled = RenderData.EnableGhostClipping && ghostEnabled;
            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                    RenderData.Model.VertexCount, 0,
                    RenderData.Model.Indexes.Length / 3,
                    InstanceCount);
            }
            Effect.GhostClippingEnabled = ghostEnabled;

            Effect.SetTexturedTechnique();
            Effect.World = Matrix.Identity;
            Device.BlendState = blendState;
            Effect.EnableWind = false;

            InstanceCount = 0;
        }
    }
}