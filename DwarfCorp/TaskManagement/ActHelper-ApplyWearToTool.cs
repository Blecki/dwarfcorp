using System.Linq;
using System;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public static void ApplyWearToTool(CreatureAI Creature, float Wear)
        {
            if (Creature.Stats.Equipment.GetItemInSlot("tool").HasValue(out var tool))
                if (tool.Tool_Breakable)
                {
                    tool.Tool_Wear += Wear;
                    if (tool.Tool_Wear > tool.Tool_Durability)
                    {
                        Creature.World.MakeAnnouncement("Tool broke!", Microsoft.Xna.Framework.Color.Red);
                        Creature.Stats.Equipment.UnequipItem("tool");
                    }
                }
        }
    }
}