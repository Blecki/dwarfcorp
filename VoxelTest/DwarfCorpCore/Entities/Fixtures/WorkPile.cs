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
    public class WorkPile : Fixture
    {
        public WorkPile()
        {
            
        }

        public WorkPile(Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.DwarfObjects.underconstruction), new Point(0, 0), PlayState.ComponentManager.RootComponent)
        {
        }
    }
}
