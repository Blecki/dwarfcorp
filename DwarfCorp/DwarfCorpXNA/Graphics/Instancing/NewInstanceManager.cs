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

        public NewInstanceManager(GraphicsDevice Device, BoundingBox Bounds, ContentManager Content)
        {
            OctTree = new OctTreeNode<NewInstanceData>(Bounds.Min, Bounds.Max);
            Renderer = new InstanceRenderer(Device, Content);
        }

        public void RemoveInstance(NewInstanceData Instance)
        {
            var box = new BoundingBox(Instance.Position - Instance.HalfSize, Instance.Position + Instance.HalfSize);
            OctTree.RemoveItem(Instance, box);
            Instance.Visible = false;
        }

        public void AddInstance(NewInstanceData Instance)
        {
            var box = new BoundingBox(Instance.Position - Instance.HalfSize, Instance.Position + Instance.HalfSize);
            OctTree.AddItem(Instance, box);
            _cachedRenderItems = null;
        }

        private List<NewInstanceData> _cachedRenderItems = new List<NewInstanceData>();
        private Matrix _prevCameraView = new Matrix();

        private float MaxMatrixDiff(Matrix a, Matrix b)
        {
            float diffZ = (a.Forward - b.Forward).Length();
            float diffTrans = (a.Translation - b.Translation).Length();
            return Math.Max(diffZ, diffTrans);
        }

        public void RenderInstances(
            GraphicsDevice Device,
            Shader Effect,
            Camera Camera,
            InstanceRenderer.RenderMode Mode)
        {
            RenderPass += 1;

            if (_cachedRenderItems == null || MaxMatrixDiff(_prevCameraView, Camera.ViewMatrix) > 1e-1)
            {
                _cachedRenderItems = OctTree.EnumerateItems(Camera.GetDrawFrustum()).ToList();
                _prevCameraView = Camera.ViewMatrix;
            }

            foreach (var item in _cachedRenderItems)
            {
                if (!item.Visible) continue;
                if (item.RenderPass < RenderPass)
                    Renderer.RenderInstance(item, Device, Effect, Camera, Mode);
                item.RenderPass = RenderPass;
            }

            Renderer.Flush(Device, Effect, Camera, Mode);
        }
    }
}
