using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class NewInstanceManager
    {
        private OctTreeNode<NewInstanceData> OctTree;
        private InstanceRenderer Renderer;
        private ulong RenderPass = 0;

        public NewInstanceManager(BoundingBox Bounds, ContentManager Content)
        {
            OctTree = new OctTreeNode<NewInstanceData>(Bounds.Min, Bounds.Max);
            Renderer = new InstanceRenderer(Content);
        }

        public void RemoveInstance(NewInstanceData Instance)
        {
            var box = new BoundingBox(Instance.Position - Instance.HalfSize, Instance.Position + Instance.HalfSize);
            OctTree.RemoveItem(Instance, box);
        }

        public void AddInstance(NewInstanceData Instance)
        {
            var box = new BoundingBox(Instance.Position - Instance.HalfSize, Instance.Position + Instance.HalfSize);
            OctTree.AddItem(Instance, box);
        }

        public void RenderInstances(
            GraphicsDevice Device,
            Shader Effect,
            Camera Camera,
            InstanceRenderer.RenderMode Mode)
        {
            int uniqueInstances = 0;
            RenderPass += 1;
            var frustrum = Camera.GetFrustrum();

            foreach (var item in OctTree.EnumerateItems(frustrum))
            {
                if (item.RenderPass < RenderPass)
                {
                    uniqueInstances += 1;
                    Renderer.RenderInstance(item, Device, Effect, Camera, Mode);
                }
                item.RenderPass = RenderPass;
            }

            Renderer.Flush(Device, Effect, Camera, Mode);
            GamePerformance.Instance.TrackValueType("Instances Drawn", uniqueInstances);
        }
    }
}
