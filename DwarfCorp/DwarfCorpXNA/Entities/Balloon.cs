using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    class Balloon : Body
    {
        Vector3 Target;
        ShipmentOrder Order;
        Faction Owner;

        public Balloon(ComponentManager Manager, Vector3 Position, Vector3 Target, ShipmentOrder Order, Faction Owner) :
            base(Manager, "Balloon", Matrix.CreateTranslation(Position), new Vector3(0.5f, 1, 0.5f), new Vector3(0, -2, 0))
        {
            this.Target = Target;
            this.Order = Order;
            this.Owner = Owner;

            InitializeCosmetics();
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            InitializeCosmetics();
        }

        private void InitializeCosmetics()
        {

            var tex = new SpriteSheet(ContentPaths.Entities.Balloon.Sprites.balloon);

            var balloonSprite = AddChild(new SimpleSprite(Manager, "BALLOON", Matrix.Identity, false, tex, Point.Zero)) as SimpleSprite;
            balloonSprite.OrientationType = SimpleSprite.OrientMode.Spherical;
            balloonSprite.SetFlag(Flag.ShouldSerialize, false);
            balloonSprite.WorldWidth = 3;
            balloonSprite.WorldHeight = 4;

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            AddChild(new Shadow(Manager, "shadow", shadowTransform, new SpriteSheet(ContentPaths.Effects.shadowcircle)));
            AddChild(new BalloonAI(Manager, Target, Order, Owner));
            AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 2, 0))).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
