using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
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
            SetFlagRecursive(Flag.ShouldSerialize, false);
        }
    }


    
}
