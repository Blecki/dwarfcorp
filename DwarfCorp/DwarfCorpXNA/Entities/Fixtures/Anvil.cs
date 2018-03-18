using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Anvil : CraftedFixture
    {
        [EntityFactory("Anvil")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new CraftedFixture("Anvil", new String[] { "Anvil" }, Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(0, 3), Data.GetData<List<ResourceAmount>>("Resources", null));
        }
    }
    
    public class Minecart : OrientedAnimatedSprite
    {
        [EntityFactory("Minecart")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Minecart(Manager, Position, Data.GetData<string>("Animations", ContentPaths.Entities.Dwarf.Sprites.worker_minecart));
        }

        public Minecart()
        {

        }

        public Minecart(ComponentManager manager, Vector3 pos, string animations) :
            base(manager, "Minecart", Matrix.CreateTranslation(pos))
        {
            var animationSet = AnimationLibrary.LoadCompositeAnimationSet(animations, "Minecart");
            foreach(var animation in animationSet)
            {
                AddAnimation(animation);
            }
            currentMode = "";
        }
    }


    
}
