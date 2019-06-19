using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Politics
    {
        public OverworldFaction OwnerFaction;
        public OverworldFaction OtherFaction;

        [JsonProperty] private List<PoliticalEvent> RecentEvents = new List<PoliticalEvent>();

        public IEnumerable<PoliticalEvent> GetEvents() { return RecentEvents; }

        public bool HasMet = false;
        public bool IsAtWar = false;

        [JsonProperty] private float Feeling = 0.0f;

        public void AddEvent(PoliticalEvent E)
        {
            RecentEvents.Add(E);
            if (RecentEvents.Count > 8)
                RecentEvents.RemoveAt(0);
            Feeling += E.Change;
        }

        public Politics()
        {
        }

        public Relationship GetCurrentRelationship()
        {
            if (Feeling < -0.5f)
                return Relationship.Hateful;
            else if (Feeling < 0.5f)
                return Relationship.Indifferent;
            else
                return Relationship.Loving;
        }

        public bool HasEvent(string text)
        {
            return RecentEvents.Any(e => e.Description == text);
        }

        public float GetCurrentFeeling()
        {
            return Feeling;
        }

        public static void Initialize(Overworld Overworld)
        {
            foreach (var otherFaction in Overworld.Natives)
                foreach (var thisFaction in Overworld.Natives)
                {
                    if (thisFaction.Name == otherFaction.Name)
                    {
                        var politics = new Politics()
                        {
                            OwnerFaction = thisFaction,
                            OtherFaction = otherFaction,
                            HasMet = true
                        };

                        politics.AddEvent(new PoliticalEvent()
                        {
                            Change = 1.0f,
                            Description = "we are of the same faction",
                        });

                        thisFaction.Politics[otherFaction.Name] = politics;
                    }
                    else
                    {
                        Politics politics = new Politics()
                        {
                            OwnerFaction = thisFaction,
                            OtherFaction = otherFaction,
                            HasMet = false,
                        };

                        if (thisFaction.Race == otherFaction.Race)
                        {
                            politics.AddEvent(new PoliticalEvent()
                            {
                                Change = 0.5f,
                                Description = "we are of the same people",
                            });

                        }

                        var thisFactionRace = Library.GetRace(thisFaction.Race);
                        var otherRace = Library.GetRace(otherFaction.Race);
                        if (thisFactionRace.NaturalEnemies.Any(name => name == otherRace.Name))
                        {
                            if (!politics.HasEvent("we are taught to hate your kind"))
                            {
                                politics.AddEvent(new PoliticalEvent()
                                {
                                    Change = -10.0f, // Make this negative and we get an instant war party rush.
                                    Description = "we are taught to hate your kind",
                                });
                            }
                        }

                        if (thisFactionRace.IsIntelligent && otherRace.IsIntelligent)
                        {
                            float trustingness = thisFaction.GoodWill;

                            if (trustingness < -0.8f)
                            {
                                if (!politics.HasEvent("we just don't trust you"))
                                {
                                    politics.AddEvent(new PoliticalEvent()
                                    {
                                        Change = -10.0f, // Make this negative and we get an instant war party rush.
                                        Description = "we just don't trust you",
                                    });
                                    politics.IsAtWar = true;
                                }

                                if (!politics.HasEvent("you stole our land"))
                                {
                                    politics.AddEvent(new PoliticalEvent()
                                    {
                                        Change = -1.0f,
                                        Description = "you stole our land",
                                    });
                                }
                            }
                            else if (trustingness > 0.8f)
                            {
                                if (!politics.HasEvent("we just trust you"))
                                {
                                    politics.AddEvent(new PoliticalEvent()
                                    {
                                        Change = 10.0f,
                                        Description = "we just trust you",
                                    });
                                }
                            }
                            //else if (faction.Value.ClaimsColony && !faction.Value.ParentFaction.IsCorporate)
                            //{
                            //    if (!politics.HasEvent("you stole our land"))
                            //    {
                            //        politics.AddEvent(new PoliticalEvent()
                            //        {
                            //            Change = -0.1f,
                            //            Description = "you stole our land",
                            //        });
                            //    }
                            //}
                        }

                        thisFaction.Politics[otherFaction.Name] = politics;
                    }

                }
        }
    }
}
