using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Balloon : GameComponent
    {
        [EntityFactory("Balloon")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Balloon(Manager, Position, Position + new Vector3(0, 1000, 0), Manager.World.PlayerFaction);
        }

        public static GameComponent CreateBalloon(
            Vector3 target, 
            Vector3 position, 
            ComponentManager componentManager, 
            Faction master)
        {
            return new Balloon(componentManager, position, target, master);
        }


        Vector3 Target;
        Faction Owner;

        public Balloon()
        {

        }

        public Balloon(ComponentManager Manager, Vector3 Position, Vector3 Target, Faction Owner) :
            base(Manager, "Balloon", Matrix.CreateTranslation(Position), new Vector3(0.5f, 1, 0.5f), new Vector3(0, -2, 0))
        {
            this.Target = Target;
            this.Owner = Owner;

            CreateCosmeticChildren(Manager);
            CollisionType = CollisionType.Dynamic;

            AddChild(new BalloonAI(Manager, Target, Owner));
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            var tex = new SpriteSheet(ContentPaths.Entities.Balloon.Sprites.balloon);

            var balloonSprite = AddChild(new SimpleSprite(Manager, "BALLOON", Matrix.Identity, tex, Point.Zero)) as SimpleSprite;
            balloonSprite.OrientationType = SimpleSprite.OrientMode.Spherical;
            balloonSprite.SetFlag(Flag.ShouldSerialize, false);
            balloonSprite.AutoSetWorldSize();

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            AddChild(Shadow.Create(1.0f, Manager));
            AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 2, 0))).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
