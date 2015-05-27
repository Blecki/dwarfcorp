using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Door : Fixture
    {
        public Door()
        {

        }

        public Door(Vector3 position) :
            base(
            position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 1),
            PlayState.ComponentManager.RootComponent)
        {
            Name = "Door";
            Tags.Add("Door");
            this.Sprite.OrientationType = Sprite.OrientMode.Fixed;
            Sprite.OrientationType = Sprite.OrientMode.Fixed;
            Sprite.LocalTransform = Matrix.CreateRotationY(0.5f * (float)Math.PI);
            OrientToWalls();
        }
    }

}
