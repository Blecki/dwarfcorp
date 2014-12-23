﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Anvil : Fixture
    {

        public Anvil()
        {

        }

        public Anvil(Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 3), PlayState.ComponentManager.RootComponent)
        {
            Tags.Add("Anvil");
        }
    }
}
