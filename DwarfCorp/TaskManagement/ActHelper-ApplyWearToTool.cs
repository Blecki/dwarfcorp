using System.Linq;
using System;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public static void ApplyWearToTool(CreatureAI Agent, float Wear)
        {
            if (Agent.Creature.Equipment.HasValue(out var equipment) && equipment.GetItemInSlot("tool").HasValue(out var tool))
                if (tool.Tool_Breakable)
                {
                    tool.Tool_Wear += Wear;
                    if (tool.Tool_Wear > tool.Tool_Durability)
                    {
                        Agent.World.MakeAnnouncement("Tool broke!", Microsoft.Xna.Framework.Color.Red);
                        equipment.UnequipItem("tool");
                    }
                }
        }
    }
}