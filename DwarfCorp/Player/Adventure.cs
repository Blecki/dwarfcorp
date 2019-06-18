using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Scripting.Adventure
{
    public class Adventure
    {
        public string Name;
        public string Description;
        public string OwnerFaction;
        public string DestinationFaction;
        public List<CreatureAI> Party;
        public List<ResourceAmount> Resources;
        public DwarfBux Money;
        public Vector2 Position;
        public Vector2 Start;
        public float ActionTimeMinutes;
        public Timer ActionTimer = null;
        public Balloon Vehicle = null;
        public bool RequiresPeace = false;
        public bool RequiresWar = false;

        public TimeSpan TotalTravelTime = new TimeSpan(0, 0, 0, 0);
        public TimeSpan RemainingTravelTime = new TimeSpan(0, 0, 0, 0);

        public enum State
        {
            None,
            Starting,
            TravelingtoDestination,
            PerformingAction,
            ComingBack,
            Done
        }

        public State AdventureState;
        public string LastEvent;
        public int HourOfLastEating = 0;

        public Adventure()
        {

        }

        public float GetProgress(WorldManager world)
        {
            int totalMinutes = (int)TotalTravelTime.TotalMinutes;
            int remainingMinutes = (int)RemainingTravelTime.TotalMinutes;

            if (AdventureState == State.TravelingtoDestination)
            {
                return 1.0f - (float)remainingMinutes / (float)totalMinutes;
            }
            else if (AdventureState == State.ComingBack)
            {
                return 1.0f - (float)remainingMinutes / (float)totalMinutes;
            }
            else if (AdventureState == State.PerformingAction)
            {
                return (ActionTimer.CurrentTimeSeconds - ActionTimer.StartTimeSeconds) / ActionTimer.TargetTimeSeconds;
            }
            else
            {
                return 0.0f;
            }
        }

        public string GetStatusString(WorldManager world)
        {
            return String.Format("Food supply {0}, {1}", Resources.Sum(r => ResourceLibrary.GetResourceByName(r.Type).Tags.Contains(Resource.ResourceTags.Edible) ? r.Count : 0), GetETAString(world));
        }

        public string GetETAString(WorldManager world)
        {
            TimeSpan eta = RemainingTravelTime;
            switch (AdventureState)
            {
                case State.ComingBack:
                    return String.Format("Returning home in {0}.", TextGenerator.TimeToString(eta));
                case State.Done:
                    return "Done";
                case State.None:
                case State.Starting:
                    return "Preparing.";
                case State.PerformingAction:
                    return "At destination.";
                case State.TravelingtoDestination:
                    return String.Format("Arriving in {0}.", TextGenerator.TimeToString(eta));
                default:
                    return "";
            }
        }

        public static TimeSpan GetETA(List<CreatureAI> party, float dist)
        {
            if (party.Count == 0)
            {
                return new TimeSpan();
            }
            float speed = party.Average(c => c.Stats.Dexterity);
            float time = 100 * (dist / speed);
            return new TimeSpan(0, (int)(time), (int)(60 * (time - (int)(time))));
        }

        public TimeSpan GetETA(WorldManager world)
        {
            if (AdventureState == State.TravelingtoDestination)
            {
                return GetETA(Party, world.Factions.Factions[DestinationFaction].DistanceToCapital);
            }
            else if (AdventureState == State.ComingBack)
            {
                var target = Start;
                return GetETA(Party, world.Factions.Factions[DestinationFaction].DistanceToCapital);
            }
            return new TimeSpan(0, 0, 0);
        }

        public float GetSpeedPerMinute()
        {
            return Party.Average(c => c.Stats.Dexterity) * 10;
        }

        public Vector2 GetTarget(WorldManager world)
        {
            var destFaction = world.Factions.Factions[DestinationFaction];
            return new Vector2(destFaction.Center.X, destFaction.Center.Y);
        }

        public virtual void OnAction(WorldManager world)
        {

        }

        public virtual void OnArrival(WorldManager world)
        {

        }

        public virtual void OnReturn(WorldManager world)
        {
            LastEvent = String.Format("The adventuring party has returned from {0}.", DestinationFaction);
            var owner = world.Factions.Factions[OwnerFaction];
            // TODO: physically move the creatures back home.
            foreach (var creature in Party)
            {
                ReturnCreature(creature);
            }

            foreach(var resource in Resources)
            {
                // TODO: handle case of not enough stockpile space.
                world.AddResources(resource);
            }
            // TODO: handle case of not enough treasury space.
            owner.AddMoney(Money);
        }

        public virtual void OnStart(WorldManager world)
        {
            LastEvent = String.Format("The adventuring party has left for {0}.", DestinationFaction);
            // TODO: physically move the creatures away.
            var owner = world.Factions.Factions[OwnerFaction];
            foreach (var creature in Party)
            {
                DestroyCreature(creature);
            }
            AdventureState = State.TravelingtoDestination;
            TotalTravelTime = GetETA(world);
            RemainingTravelTime = new TimeSpan(TotalTravelTime.Days, TotalTravelTime.Hours, TotalTravelTime.Minutes, TotalTravelTime.Seconds);
        }

        private void ReturnCreature(CreatureAI creature)
        {
            var owner = creature.World.Factions.Factions[OwnerFaction];
            if (!owner.Minions.Contains(creature))
                owner.Minions.Add(creature);
            creature.CancelCurrentTask();
            creature.GetRoot().SetFlagRecursive(GameComponent.Flag.Active, true);
            creature.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);
        }

        private void DestroyCreature(CreatureAI creature)
        {
            var owner = creature.World.Factions.Factions[OwnerFaction];
            owner.Minions.Remove(creature);
            creature.World.PersistentData.SelectedMinions.Remove(creature);
            creature.GetRoot().SetFlagRecursive(GameComponent.Flag.Active, false);
            creature.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, false);
        }

        private bool IsCreatureDead(CreatureAI p)
        {
            return p == null || p.IsDead || p.Creature.Hp <= 0;
        }

        public void Eat(DateTime time)
        {
            bool outOfFood = false;
            foreach(var creature in Party.Where(p => !IsCreatureDead(p)))
            {
                var resource = Resources.FirstOrDefault(r => r.Count > 0 && ResourceLibrary.GetResourceByName(r.Type).Tags.Contains(Resource.ResourceTags.Edible));
                if (resource != null)
                {
                    resource.Count--;
                }
                else if (MathFunctions.RandEvent(0.5f))
                {
                    outOfFood = true;
                    creature.Creature.Hp -= MathFunctions.Rand(1, 10);
                    var thoughts = creature.Creature.GetComponent<DwarfThoughts>();
                    if (thoughts != null)
                    {
                        thoughts.AddThought(Thought.ThoughtType.FeltHungry);
                    }
                }
            }
            var numDied = Party.Count(p => IsCreatureDead(p));

            foreach(var creature in Party.Where(p => IsCreatureDead(p)))
            {
                if (creature != null)
                {
                    creature.Delete();
                }
            }
            
            if (outOfFood)
            {
                if (numDied == 0)
                    LastEvent = "The adventuring party ran out of food!";
                else
                    LastEvent = String.Format("The adventuring party ran out of food! {0} starved to death.", numDied);
            }
            Party.RemoveAll(p => IsCreatureDead(p));

            if (Party.Count == 0)
            {
                AdventureState = State.Done;
                LastEvent = "All of the members of the adventuring party starved to death.";
                Resources.Clear();
                Money = 0;
            }
            HourOfLastEating = time.Hour;
        }

        private int TimeDiff(int hour1, int hour2)
        {
            int diff = hour1 - hour2;
            if (diff >= 0)
            {
                return diff;
            }
            return 24 + diff;
        }

        public bool MaybeCapture(WorldManager world)
        {
            Faction owner = world.Factions.Factions[OwnerFaction];
            Faction target = world.Factions.Factions[DestinationFaction];
            var politics = world.Diplomacy.GetPolitics(owner, target);
            if (politics.GetCurrentRelationship() != Relationship.Hateful)
            {
                return false;
            }

            if (!MathFunctions.RandEvent(0.01f))
            {
                return false;
            }

            float strength = Party.Sum(p => p.Stats.Strength);
            float enemyStrength = MathFunctions.RandInt(5, 50);
            if (enemyStrength > strength)
            {
                int numCaptured = MathFunctions.RandInt(1, Party.Count);
                for (int i = 0; i < numCaptured; i++)
                {
                    int j = MathFunctions.RandInt(0, Party.Count);
                    Party[j].Delete();
                    Party.RemoveAt(j);
                }
                LastEvent = String.Format("{0} dwarf(s) in the adventuring party were captured by {1}!", numCaptured, target.Race.Plural);
                return true;
            }
            return false;
        }

        public bool MaybeStorm(WorldManager world)
        {
            if (!MathFunctions.RandEvent(0.01f))
            {
                return false;
            }
            int randomDelay = MathFunctions.RandInt(1, 12);
            RemainingTravelTime += new TimeSpan(0, randomDelay, 0, 0, 0);
            LastEvent = String.Format("A storm has delayed the adventuring party by {0} hours.", randomDelay);
            return true;
        }

        public void MaybeDelay(WorldManager world)
        {
            if (MaybeCapture(world))
                return;
            if (MaybeStorm(world))
                return;
        }

        private int _prevHour = -1;

        public void Update(WorldManager world, DwarfTime time)
        {
            int hour = world.Time.CurrentDate.Hour;
            bool shouldEat = TimeDiff(hour, HourOfLastEating) > 12;
            if (_prevHour < 0)
            {
                _prevHour = hour;
            }
            int timeDiff = TimeDiff(hour, _prevHour);
            if (timeDiff > 0)
            {
                _prevHour = hour;
                MaybeDelay(world);
            }
            switch (AdventureState)
            {
                case State.None:
                    {
                        var balloonPorts = world.EnumerateZones().OfType<BalloonPort>().ToList();
                        if (balloonPorts.Count == 0)
                        {
                            LastEvent = "The adventuring party was cancelled, no balloon port.";
                            AdventureState = State.Done;
                            break;
                        }
                        foreach (var creature in Party)
                        {
                            if (!(creature.CurrentTask is GoToZoneTask))
                            {
                                creature.AssignTask(new GoToZoneTask(balloonPorts.First()) { Priority = Task.PriorityType.High, Wait = true });
                            }
                        }
                        AdventureState = State.Starting;
                        Vehicle = EntityFactory.CreateEntity<Balloon>("Balloon", balloonPorts.First().GetBoundingBox().Center() + Vector3.Up * 10);
                        Vehicle.GetComponent<BalloonAI>().State = BalloonAI.BalloonState.Waiting;
                        Vehicle.GetComponent<BalloonAI>().WaitTimer.Reset();
                        break;
                    }
                case State.Starting:
                    {
                        if (Vehicle != null)
                        {
                            Vehicle.GetComponent<BalloonAI>().State = BalloonAI.BalloonState.Waiting;
                            Vehicle.GetComponent<BalloonAI>().WaitTimer.Reset();
                        }
                        var balloonPorts = world.EnumerateZones().OfType<BalloonPort>().ToList();
                        if (balloonPorts.Count == 0)
                        {
                            LastEvent = "The adventuring party was cancelled, no balloon port.";
                            foreach(var creature in Party)
                            {
                                ReturnCreature(creature);
                                if (creature.CurrentTask is GoToZoneTask)
                                {
                                    creature.CancelCurrentTask();
                                }
                            }
                            AdventureState = State.Done;
                            break;
                        }
                        var port = balloonPorts.First();
                        Party.RemoveAll(creature => creature.IsDead);
                        if (Party.Count == 0)
                        {
                            LastEvent = "The adventuring party was cancelled, no more members.";
                            AdventureState = State.Done;
                            break;
                        }

                        bool allOnZone = true;
                        foreach (var creature in Party.Where(c => c.Active))
                        {
                            if (!port.IsRestingOnZone(creature.Position))
                            {
                                if (!(creature.CurrentTask is GoToZoneTask))
                                {
                                    creature.AssignTask(new GoToZoneTask(balloonPorts.First()) { Priority = Task.PriorityType.High, Wait = true });
                                }
                                allOnZone = false;
                                //break;
                            }
                            else
                            {
                                //DestroyCreature(creature);
                                //allOnZone = false;
                            }
                        }

                        if (allOnZone)
                        {
                            Vehicle.GetComponent<BalloonAI>().State = BalloonAI.BalloonState.Leaving;
                            OnStart(world);
                            AdventureState = State.TravelingtoDestination;
                        }
                        break;
                    }
                case State.TravelingtoDestination:
                    {
                        RemainingTravelTime -= new TimeSpan(0, timeDiff, 0, 0, 0);
                        if (shouldEat)
                        {
                            Eat(world.Time.CurrentDate);
                            if (AdventureState == State.Done)
                            {
                                break;
                            }
                        }
                
                        if (RemainingTravelTime.TotalHours < 1)
                        {
                            this.LastEvent = String.Format("The adventuring party has arrived at {0}", DestinationFaction);
                            AdventureState = State.PerformingAction;
                            ActionTimer = new Timer(ActionTimeMinutes * 60.0f, true);
                            OnArrival(world);
                        }
                        
                        break;
                    }
                case State.PerformingAction:
                    {
                        ActionTimer.Update(time);
                        if (ActionTimer.HasTriggered)
                        {
                            OnAction(world);
                            AdventureState = State.ComingBack;
                            RemainingTravelTime = GetETA(world);
                        }
                        break;
                    }
                case State.ComingBack:
                    {
                        RemainingTravelTime -= new TimeSpan(0, timeDiff, 0, 0, 0);
                        if (shouldEat)
                        {
                            Eat(world.Time.CurrentDate);
                            if (AdventureState == State.Done)
                            {
                                break;
                            }
                        }
                        if (RemainingTravelTime.TotalHours < 1)
                        {
                            var balloonPorts = world.EnumerateZones().OfType<BalloonPort>().ToList();
                            Vector3 location = world.Renderer.Camera.Position;
                            if (balloonPorts.Count != 0)
                            {
                                location = balloonPorts.First().GetBoundingBox().Center() + Vector3.Up * 10;
                            }

                            if (Vehicle == null || Vehicle.IsDead)
                            {
                                Vehicle = EntityFactory.CreateEntity<Balloon>("Balloon", location);
                            }
                            Vehicle.GetComponent<BalloonAI>().State = BalloonAI.BalloonState.DeliveringGoods;
                            OnReturn(world);
                            AdventureState = State.Done;
                        }
                        break;
                    }
                case State.Done:
                    {
                        if (Vehicle != null)
                        {
                            Vehicle.GetRoot().Delete();
                        }
                        break;
                    }
            }
        }
    }

    public class RaidAdventure : Adventure
    {
        public RaidAdventure()
        {
            Name = "Raiding Party";
            Description = "Send your employees to pillage the natives (WAR).";
            ActionTimeMinutes = 0.1f;
        }

        public override void OnAction(WorldManager world)
        {
            var owner = world.Factions.Factions[OwnerFaction];
            var dest = world.Factions.Factions[DestinationFaction];
            var strength = Party.Sum(p => p.Stats.Strength);
            var politics = world.Diplomacy.GetPolitics(owner, dest);
            List<ResourceAmount> destGoods = dest.Race.GenerateResources(world);

            int turns = MathFunctions.RandInt(1, (int)strength);
            List<ResourceAmount> stolenGoods = new List<ResourceAmount>();
            DwarfBux stolenMoney = 0.0m;
            int numDead = 0;
            for (int turn = 0; turn < turns; turn++)
            {
                numDead += Party.Count(p => p.Creature.Hp <= 0);
                Party.RemoveAll(p => p.Creature.Hp <= 0);
                if (Party.Count == 0)
                    break;
                var randomCritter = Datastructures.SelectRandom(Party);
                var con = randomCritter.Stats.Constitution;
                var enemyAttack = MathFunctions.RandInt(1, 20);
                if (enemyAttack - con > 10 || enemyAttack == 20)
                {
                    randomCritter.Creature.Hp-= MathFunctions.RandInt(5, 100);
                    if (randomCritter.Creature.Hp <= 0)
                    {
                        randomCritter.GetRoot().Delete();
                    }
                    var thoughts = randomCritter.Creature.GetComponent<DwarfThoughts>();
                    if (thoughts != null)
                    {
                        thoughts.AddThought(Thought.ThoughtType.TookDamage);
                    }
                }
                else
                {
                    stolenGoods.Add(new ResourceAmount(Datastructures.SelectRandom(destGoods).Type, MathFunctions.RandInt(1, 5)));
                    stolenMoney += (DwarfBux)(decimal)MathFunctions.RandInt(1, 100);
                }
            }

            politics.AddEvent(new PoliticalEvent()
            {
                Change = -5,
                Description = "You attacked us!",
                Duration = new TimeSpan(5, 0, 0, 0, 0),
                Time = world.Time.CurrentDate
            });

            politics.IsAtWar = true;
            politics.HasMet = true;

            if (Party.Count == 0)
            {
                LastEvent = "All of the raiding party members died!";
                Resources.Clear();
                Money = 0.0m;
                AdventureState = State.Done;
                return;
            }

            Resources.AddRange(stolenGoods);
            Money += stolenMoney;
            
            if (numDead == 0)
            {
                LastEvent = String.Format("The raiding party is returning home unscathed! They stole {0} goods and {1}.", stolenGoods.Sum(g => g.Count), stolenMoney);
                AdventureState = State.ComingBack;
            }
            else
            {
                LastEvent = String.Format("The raiding party is returning home. They stole {0} goods and {1}, but {2} member(s) died.", stolenGoods.Sum(g => g.Count), stolenMoney, numDead);
                AdventureState = State.ComingBack;
            }

            foreach(var creature in Party)
            {
                var thoughts = creature.Creature.GetComponent<DwarfThoughts>();
                if (thoughts != null)
                {
                    thoughts.AddThought(Thought.ThoughtType.KilledThing);
                }
            }
            base.OnAction(world);
        }
    }


    public class PeaceAdventure : Adventure
    {
        public PeaceAdventure()
        {
            Name = "Peace Mission";
            Description = "Send your employees to make peace with the natives. Resources/money will be given as gifts.";
            ActionTimeMinutes = 0.1f;
            RequiresWar = true;
        }


        public DwarfBux GetValue(Resource resource, Faction faction)
        {
            bool common = resource.Tags.Any(t => faction.Race.CommonResources.Contains(t));
            bool rare = resource.Tags.Any(t => faction.Race.RareResources.Contains(t));
            var resourceValue = resource.MoneyValue;
            if (common)
            {
                resourceValue *= 0.75;
            }
            else if (rare)
            {
                resourceValue *= 1.25;
            }
            return resourceValue;
        }


        public override void OnAction(WorldManager world)
        {
            var owner = world.Factions.Factions[OwnerFaction];
            var des = world.Factions.Factions[DestinationFaction];
            var charisma = Party.Max(p => p.Stats.Charisma);
            float tradeGoodness = charisma - MathFunctions.Rand(0, 10.0f);
            var politics = world.Diplomacy.GetPolitics(owner, des);
            if (Resources.Any(r => ResourceLibrary.GetResourceByName(r.Type).Tags.Any(t => des.Race.HatedResources.Contains(t))))
            {
                politics.AddEvent(new PoliticalEvent()
                {
                    Description = "You gave us something we hate!",
                    Change = -5,
                    Duration = new TimeSpan(4, 0, 0, 0),
                    Time = world.Time.CurrentDate
                });
                LastEvent = String.Format("The {0} of {1} were offended by our peace offering. They captured the envoy.", des.Race.Plural, des.ParentFaction.Name);
                Party.Clear();
                Resources.Clear();
                Money = 0;
                AdventureState = State.Done;
                return;
            }

            decimal tradeValue = (Resources.Sum(r => GetValue(ResourceLibrary.GetResourceByName(r.Type), des) * r.Count) + (decimal)Money) * (decimal)charisma;

            if (MathFunctions.Rand(0, 500) < (float)tradeValue)
            {

                politics.AddEvent(new PoliticalEvent()
                {
                    Description = "You sent a peace envoy.",
                    Change = 10,
                    Duration = new TimeSpan(8, 0, 0, 0),
                    Time = world.Time.CurrentDate
                });
                politics.HasMet = true;
                politics.IsAtWar = false;
                LastEvent = String.Format("The adventuring party made peace with the {0} of {1}!", des.Race.Plural, des.ParentFaction.Name);
                Money = 0;
                Resources.Clear();
                AdventureState = State.ComingBack;
            }


            base.OnAction(world);
        }
    }


    public class TradeAdventure : Adventure
    {
        public TradeAdventure()
        {
            Name = "Trade Expedition";
            Description = "Send your employees to trade goods and money with the natives.";
            ActionTimeMinutes = 0.1f;
        }

        public DwarfBux GetValue(Resource resource, Faction faction)
        {
            bool common = resource.Tags.Any(t => faction.Race.CommonResources.Contains(t));
            bool rare = resource.Tags.Any(t => faction.Race.RareResources.Contains(t));
            var resourceValue = resource.MoneyValue;
            if (common)
            {
                resourceValue *= 0.75;
            }
            else if (rare)
            {
                resourceValue *= 1.25;
            }
            return resourceValue;
        }

        public override void OnAction(WorldManager world)
        {
            if (Resources.Count == 0)
            {
                LastEvent = String.Format("The trade party didn't have anything to trade, so they're coming home.");
                return;
            }

            var owner = world.Factions.Factions[OwnerFaction];
            var dest = world.Factions.Factions[DestinationFaction];
            var charisma = Party.Max(p => p.Stats.Charisma);
            float tradeGoodness = charisma - MathFunctions.Rand(0, 10.0f);
            var politics = world.Diplomacy.GetPolitics(owner, dest);
            List<ResourceAmount> destGoods = dest.Race.GenerateResources(world);
            List<ResourceAmount> tradeGoods = new List<ResourceAmount>();
            bool wasBadTrade = false;
            int randIters = 100;
            for (int iter = 0; iter < randIters; iter++)
            {
                var resourceType = Datastructures.SelectRandom(Resources);
                if (resourceType.Count == 0)
                {
                    continue;
                }
                var resource = ResourceLibrary.GetResourceByName(resourceType.Type);
                bool liked = resource.Tags.Any(t => dest.Race.LikedResources.Contains(t));
                bool hated = resource.Tags.Any(t => dest.Race.HatedResources.Contains(t));

                if (tradeGoodness < 0 && liked)
                {
                    LastEvent = String.Format("{0} gave the {1} {2}, which made them very angry!", 
                                               Datastructures.SelectRandom(Party).Stats.FullName,
                                               dest.Race.Name, resourceType.Type);
                    string badTrade = "You gave us something we hate.";
                    if (!politics.HasEvent(badTrade))
                    {
                        politics.AddEvent(new PoliticalEvent()
                        {
                            Description = badTrade,
                            Change = -5,
                            Duration = new TimeSpan(4, 0, 0, 0),
                            Time = world.Time.CurrentDate
                        });
                    }
                    wasBadTrade = true;
                    break;
                }
                else if (tradeGoodness > 0 && liked)
                {
                    string goodTrade = "You gave us something we like.";
                    if (!politics.HasEvent(goodTrade))
                    {
                        politics.AddEvent(new PoliticalEvent()
                        {
                            Description = goodTrade,
                            Change = 5,
                            Duration = new TimeSpan(4, 0, 0, 0),
                            Time = world.Time.CurrentDate
                        });
                    }
                }
                var resourceValue = GetValue(resource, dest);
                if (resourceValue == 0) continue;

                // Now find a resource of equal or greater value and trade for it.
                int randIter2 = 0;
                while (randIter2 < 100)
                {
                    var randGood = Datastructures.SelectRandom(destGoods);
                    if (randGood.Count == 0)
                    {
                        randIter2++;
                        continue;
                    }

                    var good = ResourceLibrary.GetResourceByName(randGood.Type);
                    var randGoodValue = GetValue(good, dest);
                    if (randGoodValue == 0) continue;

                    // Trade the most of this resource we possibly can. If we want to 
                    // trade an item of lesser value, try to trade 1 good for as much of it as possible.
                    if (randGoodValue <= resourceValue)
                    {
                        int numToTrade = Math.Min((int)(resourceValue / randGoodValue), randGood.Count);
                        if (numToTrade * randGoodValue >= 0.75f * resourceValue)
                        {
                            randGood.Count -= numToTrade;
                            resourceType.Count--;
                            tradeGoods.Add(new ResourceAmount(good, numToTrade));
                            break;
                        }
                    }
                    // If we're trading upwards, try trading as much of our resource as possible for the valuable item.
                    else
                    {
                        int numToTrade = Math.Min((int)(randGoodValue / resourceValue), resourceType.Count);
                        if (numToTrade * resourceValue >= 0.75f * randGoodValue)
                        {
                            randGood.Count --;
                            resourceType.Count-=numToTrade;
                            tradeGoods.Add(new ResourceAmount(good, 1));
                            break;
                        }
                    }

                    randIter2++;
                }

                // We failed to find a good of equal value, so let's just trade money.
                if (randIter2 == 100)
                {
                    resourceType.Count--;
                    Money += resourceValue;
                }
            }


            Resources.AddRange(tradeGoods);
            if (!wasBadTrade)
            {
                LastEvent = String.Format("The trade party is returning home with {0} goods, and {1}.", Resources.Sum(r => r.Count), Money);
            }

            base.OnAction(world);
        }
    }
}
