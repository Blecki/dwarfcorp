using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public class FixtureCraftDetails : CraftDetails
    {
        // Defines a mapping from specific resource tags in a crafted
        // resource to frames in a sprite sheet.
        [JsonIgnore]
        public Dictionary<Resource.ResourceTags, Point> Sprites;
        // The default sprite sheet to use if there is no such mapping.
        [JsonIgnore]
        public Point DefaultSpriteFrame;

        public Point GetSpritesheetFrame(String resourceType)
        {
            if (Library.GetResourceType(resourceType).HasValue(out var resource))
                foreach (var tag in resource.Tags)
                    if (Sprites.ContainsKey(tag))
                        return Sprites[tag];

            return DefaultSpriteFrame;
        }

        public FixtureCraftDetails()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public FixtureCraftDetails(ComponentManager manager) :
            base(manager)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public FixtureCraftDetails Clone()
        {
            var details = new FixtureCraftDetails(Manager)
            {
                Resources = new List<ResourceAmount>(),
                Sprites = new Dictionary<Resource.ResourceTags, Point>(),
                DefaultSpriteFrame = DefaultSpriteFrame,
                CraftType = CraftType
            };
            details.Resources.AddRange(Resources);
            foreach (var pair in Sprites)
            {
                details.Sprites[pair.Key] = pair.Value;
            }
            return details;
        }

    }
}
