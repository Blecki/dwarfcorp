using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using LibNoise.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class RadiusBuffer : RadiusSensor
    {
        public StatusEffect Buff = null;

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (Buff == null)
                return;

            base.Update(gameTime, chunks, camera);

            foreach (var creature in Creatures)
                creature.Stats.AddBuff(Buff.Clone()); // Todo: Check if the creature already has this kind of buff and dissalow if so.
        }

        public RadiusBuffer(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
        }
    }
}