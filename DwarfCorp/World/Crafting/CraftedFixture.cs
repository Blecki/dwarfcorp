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
    public class CraftedFixture : Fixture
    {
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
}
