using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Ladder : Fixture
    {
        public Ladder()
        {

        }

        public Ladder(Vector3 position) :
            base(
            position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 0),
            PlayState.ComponentManager.RootComponent)
        {
            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
            
            Name = "Ladder";
            Tags.Add("Climbable");
            Sprite.OrientationType = Sprite.OrientMode.Fixed;
            Sprite.LocalTransform = Matrix.CreateTranslation(new Vector3(0, 0, 0.45f)) * Matrix.CreateRotationY(0.0f);
            OrientToWalls();
        }
    }

}
