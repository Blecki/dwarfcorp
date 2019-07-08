using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class InstanceRenderer
    {
        private Dictionary<string, InstanceGroup> InstanceTypes = new Dictionary<string, InstanceGroup>();
        private int _instanceCounter = 0;

        public bool DoesGroupExist(string Name)
        {
            return InstanceTypes.ContainsKey(Name);
        }

        public void AddInstanceGroup(InstanceGroup Group)
        {
            Group.Initialize();
            lock (InstanceTypes)
            {
                InstanceTypes[Group.Name] = Group;
            }
        }

        public void RenderInstance(
            NewInstanceData Instance,
            GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            lock (InstanceTypes)
            {
                if (Instance.Type == null || !InstanceTypes.ContainsKey(Instance.Type))
                    return;

                InstanceTypes[Instance.Type].RenderInstance(Instance, Device, Effect, Camera, Mode);

                _instanceCounter += 1;
            }
        }

        public void Flush(
            GraphicsDevice Device,
            Shader Effect,
            Camera Camera,
            InstanceRenderMode Mode)
        {
            lock (InstanceTypes)
            {
                foreach (var group in InstanceTypes)
                    group.Value.Flush(Device, Effect, Camera, Mode);

                PerformanceMonitor.SetMetric("INSTANCES DRAWN", _instanceCounter);
                _instanceCounter = 0;
            }
        }

        internal String PrepareCombinedTiledInstance()
        {
            lock (InstanceTypes)
            {
                if (!DoesGroupExist("combined-tiled-instance"))
                    AddInstanceGroup(new TiledInstanceGroup
                    {
                        RenderData = new InstanceRenderData
                        {
                            EnableWind = false,
                            RenderInSelectionBuffer = true,
                            EnableGhostClipping = true,
                            Model = new BatchBillboardPrimitive(new NamedImageFrame("newgui\\error"), 32, 32,
                            new Point(0, 0), 1.0f, 1.0f, false,
                            new List<Matrix> { Matrix.Identity },
                            new List<Color> { Color.White },
                            new List<Color> { Color.White })
                        },
                        Name = "combined-tiled-instance"
                    });

                return "combined-tiled-instance";
            }
        }
    }
}