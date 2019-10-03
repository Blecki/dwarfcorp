using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Stove : CraftedFixture
    {
        [EntityFactory("Stove")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Stove(Manager, Position, Data.GetData<Resource>("Resource", null));
        }

        public Stove()
        {

        }

        public Stove(ComponentManager manager, Vector3 position, Resource Resource) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 4), new CraftDetails(manager, Resource))
        {
            Name = "Stove";
            Tags.Add("Stove");
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);
            if (!Active)
                return;
            if (MathFunctions.RandEvent(0.01f))
                Manager.World.ParticleManager.Trigger("smoke", GlobalTransform.Translation + Vector3.Up * .5f, Color.White, 1);
        }
    }
}
