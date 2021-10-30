using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp
{
    public static class PitTrapEventHook
    {
        [VoxelEventHook("pittrap_hook")]
        private static void _hook(VoxelEvent Event, WorldManager World)
        {
            if (Event.Type == VoxelEventType.SteppedOn)
            {
                if (Event.Creature.Faction != World.PlayerFaction)
                {
                    var politics = World.Overworld.GetPolitics(Event.Creature.Faction.ParentFaction, World.PlayerFaction.ParentFaction);
                    if (politics.GetCurrentRelationship() == Relationship.Hateful)
                    {
                        World.ParticleManager.Trigger("dwarf_puff", Event.Voxel.Center, Microsoft.Xna.Framework.Color.White, 90);
                        Event.Voxel.Type = Library.EmptyVoxelType;
                    }
                }
            }
        }
    }
}
