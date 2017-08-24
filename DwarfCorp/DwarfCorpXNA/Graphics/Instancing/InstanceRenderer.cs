using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
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

        public InstanceRenderer(ContentManager Content)
        {
            CreateInstanceTypes(Content);
        }

        private void CreateBillboard(string name, ContentManager content)
        {
            if (!PrimitiveLibrary.BatchBillboardPrimitives.ContainsKey(name))
                PrimitiveLibrary.CreateIntersecting(name, name, GameState.Game.GraphicsDevice, content);

            InstanceTypes.Add(name, new InstanceGroup
            {
                RenderData = new InstanceRenderData
                {
                    Model = PrimitiveLibrary.BatchBillboardPrimitives[name],
                    Texture = PrimitiveLibrary.BatchBillboardPrimitives[name].Texture,
                    BlendMode = BlendState.NonPremultiplied,
                    EnableWind = true,
                }
            });
        }

        private void CreateInstanceTypes(ContentManager content)
        {
            CreateBillboard("pine", content);
            CreateBillboard("palm", content);
            CreateBillboard("snowpine", content);
            CreateBillboard("appletree", content);
            CreateBillboard("berrybush", content);
            CreateBillboard("cactus", content);
            CreateBillboard("grass", content);
            CreateBillboard("frostgrass", content);
            CreateBillboard("flower", content);
            CreateBillboard("deadbush", content);
            CreateBillboard("vine", content);
            CreateBillboard("gnarled", content);
            CreateBillboard("mushroom", content);
            CreateBillboard("wheat", content);
            CreateBillboard("caveshroom", content);
        }

        public enum RenderMode
        {
            Normal,
            SelectionBuffer
        }

        public void RenderInstance(NewInstanceData Instance,
            GraphicsDevice Device, Shader Effect, Camera Camera, RenderMode Mode)
        {
            System.Diagnostics.Debug.Assert(InstanceTypes.ContainsKey(Instance.Type));

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

            if (Group.RenderData.Model.VertexBuffer == null || Group.RenderData.Model.IndexBuffer == null)
                Group.RenderData.Model.ResetBuffer(Device);

            bool hasIndex = Group.RenderData.Model.IndexBuffer != null;
            Device.Indices = Group.RenderData.Model.IndexBuffer;

            BlendState blendState = Device.BlendState;
            Device.BlendState = Group.RenderData.BlendMode;

            Effect.MainTexture = Group.RenderData.Texture;
            Effect.LightRampTint = Color.White;

            InstanceBuffer.SetData(Group.Instances, 0, Group.InstanceCount, SetDataOptions.Discard);
            Device.SetVertexBuffers(Group.RenderData.Model.VertexBuffer, new VertexBufferBinding(
                InstanceBuffer, 0, 1));

            foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
                    Group.RenderData.Model.VertexCount, 0,
                    Group.RenderData.Model.Indexes.Length / 3,
                    Group.InstanceCount);
            }


            Effect.SetTexturedTechnique();
            Effect.World = Matrix.Identity;
            Device.BlendState = blendState;
            Effect.EnableWind = false;

            Group.InstanceCount = 0;
        }
    }
}