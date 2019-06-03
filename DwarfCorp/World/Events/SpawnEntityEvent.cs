using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Events
{
    public class SpawnEntityEvent : ScheduledEvent
    {
        public string EntityToSpawn;
        public string EntityFaction;
        public bool ZoomToEntity;
        public EntitySpawnLocation SpawnLocation;
        public FactionFilter EntityFactionFilter;
        public int MinEntityLevel = 1;
        public int MaxEntityLevel = 1;

        public SpawnEntityEvent()
        {

        }

        public override void Trigger(WorldManager world)
        {
            GameComponent entity = null;
            Microsoft.Xna.Framework.Vector3 location = GetSpawnLocation(world, SpawnLocation);

            string faction = GetFaction(world, EntityFaction, EntityFactionFilter);

            bool validFaction = (!String.IsNullOrEmpty(faction) && world.Factions.Factions.ContainsKey(faction));

            if (!String.IsNullOrEmpty(EntityToSpawn))
            {
                entity = EntityFactory.CreateEntity<GameComponent>(EntityToSpawn, location);
                if (validFaction)
                {
                    var creatures = entity.EnumerateAll().OfType<CreatureAI>();
                    foreach (var creature in creatures)
                    {
                        if (creature.Faction != null)
                        {
                            creature.Faction.Minions.Remove(creature);
                        }

                        creature.Faction = world.Factions.Factions[faction];
                        creature.Stats.LevelIndex = MathFunctions.RandInt(MinEntityLevel, MaxEntityLevel);
                    }
                }
            }

            Announce(world, entity, ZoomToEntity);
        }
    }
}
