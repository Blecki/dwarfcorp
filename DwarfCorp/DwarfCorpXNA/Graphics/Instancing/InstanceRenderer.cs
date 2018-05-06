using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public class InstanceRenderer
    {
        private Dictionary<string, InstanceGroup> InstanceTypes = new Dictionary<string, InstanceGroup>();

        public InstanceRenderer(GraphicsDevice Device, ContentManager Content)
        {
            var instanceGroups = FileUtils.LoadJsonListFromMultipleSources<InstanceGroup>(ContentPaths.instance_groups, null, g => g.Name);
            foreach (var group in instanceGroups)
            {
                group.Initialize();
                InstanceTypes.Add(group.Name, group);
            }
        }

        public void RenderInstance(
            NewInstanceData Instance,
            GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode)
        {
            if (Instance.Type == null || !InstanceTypes.ContainsKey(Instance.Type))
                return;

            InstanceTypes[Instance.Type].RenderInstance(Instance, Device, Effect, Camera, Mode);
        }

        public void Flush(
            GraphicsDevice Device,
            Shader Effect,
            Camera Camera,
            InstanceRenderMode Mode)
        {
            foreach (var group in InstanceTypes)
                group.Value.Flush(Device, Effect, Camera, Mode);
        }
    }
}