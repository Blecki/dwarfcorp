using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static partial class ActHelper
    {
        public static Act CreateToolCheckAct(Resource.ResourceTags ToolType, CreatureAI Creature)
        {
            return new Select(
                new Condition(() =>
                {
                    if (!Creature.Stats.CurrentClass.RequiresTools) return true;
                    if (Creature.Stats.Equipment.GetItemInSlot("tool").HasValue(out var tool) && Library.GetResourceType(tool.Resource).HasValue(out var res))
                        return res.Tags.Contains(ToolType);
                    return false;
                }),
                new Sequence(
                    new GetResourcesAct(Creature, new List<Quantitiy<Resource.ResourceTags>>() { new Quantitiy<Resource.ResourceTags>(ToolType, 1) }),
                    new EquipToolAct(Creature))
                );
        }
    }
}