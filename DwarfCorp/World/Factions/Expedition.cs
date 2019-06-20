using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DwarfCorp
{
    [JsonObject(IsReference =true)]
    public class Expedition
    {
        public enum State
        {
            Leaving,
            Arriving,
            Fighting,
            Trading
        }

        public DateTimer DeathTimer { get; set; }
        public List<CreatureAI> Creatures { get; set; }
        public Faction OwnerFaction { get; set; }
        public Faction OtherFaction { get; set; }
        public State ExpiditionState { get; set; }
        public bool ShouldRemove { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            // nothing for now.
        }

        public Expedition()
        {

        }

        public Expedition(DateTime date)
        {
            ExpiditionState = State.Arriving;
            ShouldRemove = false;
            Creatures = new List<CreatureAI>();
            DeathTimer = new DateTimer(date, new TimeSpan(1, 12, 0, 0));
        }
    }
}