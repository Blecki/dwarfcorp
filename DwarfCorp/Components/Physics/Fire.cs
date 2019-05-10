using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Fire : GameComponent
    {
        public Timer LifeTimer = new Timer(5.0f, true);

        [EntityFactory("Fire")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Fire(Manager, Position);
        }

        public Fire() :
            base()
        {
            LifeTimer = new Timer(MathFunctions.Rand(4.0f, 10.0f), true);
        }

        public Fire(ComponentManager manager, Vector3 pos) :
            base(manager, "Fire", Matrix.CreateTranslation(pos), Vector3.One, Vector3.Zero)
        {
            CollisionType = CollisionType.Static;
            Tags.Add("Fire");
            AddChild(new Flammable(manager, "Flammable")
            {
                Heat = 999,
            });
            AddChild(new Health(manager, "Health", 10.0f, 0.0f, 10.0f));
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            base.Update(Time, Chunks, Camera);

            LifeTimer.Update(Time);
            
            if (LifeTimer.HasTriggered)
            {
                Die();
            }
        }
    }
}