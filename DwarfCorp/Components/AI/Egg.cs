using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public class Egg : GameComponent
    {
        public string Adult { get; set; }
        public DateTime Birthday { get; set; }
        public GameComponent ParentBody { get; set; }
        public BoundingBox PositionConstrain { get; set; }
        public Egg()
        {
        }

        public Egg(GameComponent body, string adult, ComponentManager manager, Vector3 position, BoundingBox positionConstraint) :
            base(manager)
        {
            PositionConstrain = positionConstraint;
            Adult = adult;
            Birthday = Manager.World.Time.CurrentDate + new TimeSpan(0, 12, 0, 0);
            ParentBody = body;
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Manager.World.Time.CurrentDate > Birthday)
            {
                Hatch();
            }
        }

        public void Hatch()
        {
            var adult = EntityFactory.CreateEntity<GameComponent>(Adult, ParentBody.Position);
            if (adult != null)
            {
                adult.GetRoot().GetComponent<CreatureAI>().PositionConstraint = PositionConstrain;
            }
            GetRoot().Die();
        }
    }
}
