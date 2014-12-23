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
    public class Strawman : Fixture
    {
        public Strawman()
        {

        }

        public Strawman(Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), new Point(1, 5), PlayState.ComponentManager.RootComponent)
        {
            Tags.Add("Strawman");
        }
    }
}
