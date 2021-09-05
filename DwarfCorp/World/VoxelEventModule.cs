using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class VoxelEventModule : EngineModule
    {
        [UpdateSystemFactory]
        private static EngineModule __factory(WorldManager World)
        {
            return new VoxelEventModule();
        }

        public override ModuleManager.UpdateTypes UpdatesWanted => ModuleManager.UpdateTypes.VoxelChange;
        private static Dictionary<String, System.Reflection.MethodInfo> VoxelTriggerHooks;

        private static void DiscoverHooks()
        {
            VoxelTriggerHooks = new Dictionary<string, System.Reflection.MethodInfo>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(VoxelEventHookAttribute), typeof(void), new Type[] { typeof(VoxelEvent), typeof(WorldManager) }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is VoxelEventHookAttribute) as VoxelEventHookAttribute;
                if (attribute == null) continue;
                VoxelTriggerHooks[attribute.Name] = method;
            }
        }

        public override void VoxelEvent(List<VoxelEvent> Events, WorldManager World)
        {
            if (VoxelTriggerHooks == null)
                DiscoverHooks();

            foreach (var @event in Events)
            {
                if (@event.Voxel.IsEmpty)
                    continue;

                if (!String.IsNullOrEmpty(@event.Voxel.Type.EventHook) && VoxelTriggerHooks.ContainsKey(@event.Voxel.Type.EventHook))
                {
                    try
                    {
                        VoxelTriggerHooks[@event.Voxel.Type.EventHook].Invoke(null, new Object[] { @event, World });
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
        }
    }
}
