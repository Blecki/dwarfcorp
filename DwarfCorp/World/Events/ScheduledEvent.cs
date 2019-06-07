using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Events
{
    public class ScheduledEvent
    {
        public int Likelihood = 1;
        public string Name;
        public float Difficulty = 0.0f;
        public bool SpawnOnTranquil = true;

        public enum TimeRestriction
        {
            All,
            OnlyDayTime,
            OnlyNightTime,
        }

        public TimeRestriction AllowedTime = TimeRestriction.All;

        public enum FactionHostilityFilter
        {
            Any,
            Hostile,
            Neutral,
            Ally,
            NotHostile,
            NotAlly
        }

        public enum FactionClaimFilter
        {
            Any,
            ClaimsTerritory,
            DoesNotClaimTerritory
        }

        public enum FactionSpecification
        {
            Specific,
            Random,
            Player,
            Motherland,
        }

        public struct FactionFilter
        {
            public FactionHostilityFilter Hostility;
            public FactionSpecification Specification;
            public FactionClaimFilter Claim;
        }

        public enum EntitySpawnLocation
        {
            BalloonPort,
            RandomZone,
            WorldEdge
        }

        public int CooldownHours = 0;
        public string AnnouncementText;
        public string AnnouncementDetails;
        public string AnnouncementSound;
        public bool PauseOnAnnouncementDetails;

        protected void Announce(WorldManager world, GameComponent entity, bool zoomToEntity)
        {
            if (!String.IsNullOrEmpty(AnnouncementSound))
            {
                SoundManager.PlaySound(AnnouncementSound, 0.2f);
            }

            if (!String.IsNullOrEmpty(AnnouncementText))
            {
                Func<bool> keep = null;
                if (entity != null)
                {
                    keep = () => !entity.IsDead;
                }
                ScheduledEvent scheduledEvent = this;
                world.MakeAnnouncement(AnnouncementText, (sender, args) =>
                {
                    if (!String.IsNullOrEmpty(scheduledEvent.AnnouncementDetails))
                    {
                        if (entity == null)
                        {
                            world.UserInterface.Gui.ShowModalPopup(new Gui.Widgets.Confirm()
                            {
                                Text = scheduledEvent.AnnouncementDetails
                            });
                        }
                        else
                            world.UserInterface.MakeWorldPopup(scheduledEvent.AnnouncementDetails, entity, -10);
                    }

                    if (scheduledEvent.PauseOnAnnouncementDetails)
                    {
                        world.Paused = true;
                    }

                    if (zoomToEntity && entity != null)
                    {
                        world.Renderer.Camera.ZoomTo(entity.Position);
                    }

                }, keep);
            }
        }


        private bool CanSpawnFaction(WorldManager world, Faction faction, string EntityFaction, FactionFilter filter)
        {
            if (filter.Specification != FactionSpecification.Player && faction == world.PlayerFaction)
            {
                return false;
            }

            switch (filter.Specification)
            {
                case FactionSpecification.Motherland:
                    return faction.ParentFaction.IsCorporate;

                case FactionSpecification.Player:
                    return faction == world.PlayerFaction;

                case FactionSpecification.Random:
                    switch (filter.Claim)
                    {
                        case FactionClaimFilter.ClaimsTerritory:
                            if (!faction.ClaimsColony)
                            {
                                return false;
                            }
                            break;
                        case FactionClaimFilter.DoesNotClaimTerritory:
                            if (faction.ClaimsColony)
                            {
                                return false;
                            }
                            break;
                        case FactionClaimFilter.Any:
                            break;
                    }
                    var relationship = world.Diplomacy.GetPolitics(faction, world.PlayerFaction).GetCurrentRelationship();
                    switch (filter.Hostility)
                    {
                        case FactionHostilityFilter.Ally:
                            return relationship == Relationship.Loving;
                        case FactionHostilityFilter.Neutral:
                            return relationship == Relationship.Indifferent;
                        case FactionHostilityFilter.Hostile:
                            return relationship == Relationship.Hateful;
                        case FactionHostilityFilter.NotHostile:
                            return relationship != Relationship.Hateful;
                        case FactionHostilityFilter.NotAlly:
                            return relationship != Relationship.Loving;
                        case FactionHostilityFilter.Any:
                            return true;
                    }
                    return true;

                case FactionSpecification.Specific:
                    return faction.ParentFaction.Name == EntityFaction;
            }
            return false;
        }

        public string GetFaction(WorldManager world, string EntityFaction, FactionFilter EntityFactionFilter)
        {
            var factions = world.Factions.Factions.Where(f => f.Value.Race.IsIntelligent && f.Value.ParentFaction.InteractiveFaction &&
                CanSpawnFaction(world, f.Value, EntityFaction, EntityFactionFilter)).Select(f => f.Value).ToList();
            factions.Sort((f1, f2) => f1.DistanceToCapital.CompareTo(f2.DistanceToCapital));

            if (factions.Count == 0)
                return EntityFaction;

            float sumDistance = factions.Sum(f => 1.0f / (f.DistanceToCapital + 1.0f));
            float randPick = MathFunctions.Rand(0, sumDistance);

            float dist = 0;
            foreach(var faction in factions)
            {
                dist += 1.0f / (1.0f + faction.DistanceToCapital);
                if (randPick < dist)
                {
                    return faction.ParentFaction.Name;
                }
            }

            return EntityFaction;
        }

        public Microsoft.Xna.Framework.Vector3 GetSpawnLocation(WorldManager world, EntitySpawnLocation SpawnLocation)
        {
            Microsoft.Xna.Framework.Vector3 location = location = VoxelHelpers.FindFirstVoxelBelowIncludingWater(new VoxelHandle(world.ChunkManager, GlobalVoxelCoordinate.FromVector3(MonsterSpawner.GetRandomWorldEdge(world)))).WorldPosition + Microsoft.Xna.Framework.Vector3.Up * 1.5f;
            switch (SpawnLocation)
            {
                case EntitySpawnLocation.BalloonPort:
                    {
                        var balloonport = world.EnumerateZones().OfType<BalloonPort>();
                        if (balloonport.Any())
                        {
                            location = Datastructures.SelectRandom(balloonport).GetBoundingBox().Center() + Microsoft.Xna.Framework.Vector3.Up * 1.5f;
                        }
                        break;
                    }
                case EntitySpawnLocation.RandomZone:
                    {
                        var zones = world.EnumerateZones();
                        if (zones.Any())
                        {
                            location = Datastructures.SelectRandom(zones).GetBoundingBox().Center() + Microsoft.Xna.Framework.Vector3.Up * 1.5f;
                        }
                        break;
                    }
                case EntitySpawnLocation.WorldEdge:
                    {
                        // already computed
                        break;
                    }
            }

            return location;
        }

        /// <summary>
        /// Called when the event is first triggered.
        /// </summary>
        public virtual void Trigger(WorldManager world)
        {
            Announce(world, null, false);
        }

        /// <summary>
        /// Called continuously while the event is active (ShouldKeep() returns true)
        /// </summary>
        public virtual void Update(WorldManager world)
        {

        }

        /// <summary>
        /// Gets called when ShouldKeep() returns false
        /// </summary>
        public virtual void Deactivate(WorldManager world)
        {

        }

        /// <summary>
        /// Whether or not the event should stick around in the event manager and get updated.
        /// Deactivate will get called once this returns false.
        /// </summary>
        public virtual bool ShouldKeep(WorldManager world)
        {
            return false;
        }
    }
}
