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
    public class CraftDetails : GameComponent
    {
        // Corresponds to a type in the CraftLibrary
        public string CraftType = "";
        // The Resources used to craft the item.
        public List<ResourceAmount> Resources = new List<ResourceAmount>();

        public CraftDetails()
        {
            UpdateRate = 1000;
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftDetails(ComponentManager manager) :
            base(manager)
        {
            UpdateRate = 1000;
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftDetails(ComponentManager manager, string craftType, List<ResourceAmount> resources = null) :
            this(manager)
        {
            UpdateRate = 1000;
            CraftType = craftType;

            if (resources != null)
                Resources = resources;
            else
            {
                Resources = new List<ResourceAmount>();
                var libraryType = Library.GetCraftable(craftType);

                if (libraryType != null)
                    Resources.AddRange(libraryType.RequiredResources.Select(requirement => new ResourceAmount(ResourceLibrary.FindResourcesWithTag(requirement.Type).OrderBy(r => r.MoneyValue.Value).FirstOrDefault(), requirement.Count)));
            }
        }

        public override void Die()
        {
            var body = Parent.GetRoot().GetComponent<GameComponent>();
            if (body != null)
            {
                var bounds = body.GetBoundingBox();
                Resource resource = Library.GetCraftable(this.CraftType).ToResource(World, Resources);
                Vector3 pos = MathFunctions.RandVector3Box(bounds);
                EntityFactory.CreateEntity<GameComponent>(resource.Name + " Resource", pos);
            }
            base.Die();
        }
    }

    [JsonObject(IsReference = true)]
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
            var resource = ResourceLibrary.GetResourceByName(resourceType);
            foreach (var tag in resource.Tags)
            {
                if (Sprites.ContainsKey(tag))
                {
                    return Sprites[tag];
                }
            }

            return DefaultSpriteFrame;
        }

        public FixtureCraftDetails()
        {
            UpdateRate = 1000;
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public FixtureCraftDetails(ComponentManager manager) :
            base(manager)
        {
            UpdateRate = 1000;
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

    [JsonObject(IsReference = true)]
    public class CraftedFixture : Fixture
    {
        [JsonIgnore]
        public FixtureCraftDetails CraftDetails { get { return GetRoot().GetComponent<FixtureCraftDetails>(); } }

        public CraftedFixture()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftedFixture(ComponentManager manager, Vector3 position, SpriteSheet sheet, Point frame, CraftDetails details, SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical) :
            base(manager, position, sheet, frame, OrientMode)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(details);
        }

        public CraftedFixture(
            ComponentManager Manager,
            Vector3 position,
            SpriteSheet asset,
            FixtureCraftDetails details,
            SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical) :
            base(Manager, position, asset, details.GetSpritesheetFrame(details.Resources[0].Type), OrientMode)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(details);
        }

        public CraftedFixture(
            String Name,
            IEnumerable<String> Tags,
            ComponentManager Manager,
            Vector3 Position,
            SpriteSheet Sheet,
            Point Sprite,
            List<ResourceAmount> Resources)
            : base(Name, Tags, Manager, Position, Sheet, Sprite)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(new CraftDetails(Manager, Name, Resources));
        }
    }

    [JsonObject(IsReference = true)]
    public class CraftedBody : GameComponent
    {
        [JsonIgnore]
        public FixtureCraftDetails CraftDetails { get { return GetRoot().GetComponent<FixtureCraftDetails>(); } }

        public CraftedBody()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftedBody(
            ComponentManager Manager,
            string name,
            Matrix localTransform,
            Vector3 bboxExtents,
            Vector3 bboxPos,
            CraftDetails details) :
            base(Manager, name, localTransform, bboxExtents, bboxPos)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(details);
        }

    }
}
