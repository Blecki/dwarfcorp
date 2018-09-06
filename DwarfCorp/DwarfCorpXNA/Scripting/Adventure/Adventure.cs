using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Scripting.Adventure
{
    public class Adventure
    {
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

        public Adventure()
        {

        }

        public float GetProgress(WorldManager world)
        {
            if (AdventureState == State.TravelingtoDestination)
            {
                var target = GetTarget(world);
                var diff = (target - Position);
                var total = (target - Start);
                return 1.0f - diff.Length() / total.Length();
            }
            else if (AdventureState == State.ComingBack)
            {
                var target = Start;
                var diff = (target - Position);
                var total = (target - Start);
                return 1.0f - diff.Length() / total.Length();
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
            TimeSpan eta = GetETA(world);
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

        public TimeSpan GetETA(WorldManager world)
        {
            if (AdventureState == State.TravelingtoDestination)
            {
                var target = GetTarget(world);
                var vel = (target - Position);
                float time =  100 * (vel.Length() / GetSpeedPerMinute());
                return new TimeSpan(0, (int)(time), (int)(60 * (time - (int)(time))));
            }
            else if (AdventureState == State.ComingBack)
            {
                var target = Start;
                var vel = (target - Position);
                float time = 100 * (vel.Length() / GetSpeedPerMinute());
                return new TimeSpan(0, (int)(time), (int)(60 * (time - (int)(time))));
            }
            return new TimeSpan(0, 0, 0);
        }

        public float GetSpeedPerMinute()
        {
            return Party.Average(c => c.Stats.BuffedDex) * 100;
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
                owner.AddResources(resource);
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

            owner.RemoveResources(Resources, Vector3.Zero, false);
            owner.AddMoney(-Money);
        }

        private void ReturnCreature(CreatureAI creature)
        {
            var owner = creature.World.Factions.Factions[OwnerFaction];
            owner.Minions.Add(creature);
            creature.CancelCurrentTask();
            creature.GetRoot().SetFlagRecursive(GameComponent.Flag.Active, true);
            creature.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);
        }

        private void DestroyCreature(CreatureAI creature)
        {
            if (!creature.Active)
            {
                return;
            }

            var owner = creature.World.Factions.Factions[OwnerFaction];
            owner.Minions.Remove(creature);
            if (creature.World.Master.SelectedMinions.Contains(creature))
            {
                creature.World.Master.SelectedMinions.Remove(creature);
            }
            creature.GetRoot().SetFlagRecursive(GameComponent.Flag.Active, false);
            creature.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, false);
        }

        public void Update(WorldManager world, DwarfTime time)
        { 
            switch (AdventureState)
            {
                case State.None:
                    {
                        var balloonPorts = world.PlayerFaction.GetRooms().OfType<BalloonPort>().ToList();
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
                        var balloonPorts = world.PlayerFaction.GetRooms().OfType<BalloonPort>().ToList();
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
                                break;
                            }
                            else
                            {
                                DestroyCreature(creature);
                                allOnZone = false;
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
                        var target = GetTarget(world);
                        var vel = (target - Position);
                        if (vel.Length() < 1.0f)
                        {
                            this.LastEvent = String.Format("The adventuring party has arrived at {0}", DestinationFaction);
                            AdventureState = State.PerformingAction;
                            ActionTimer = new Timer(ActionTimeMinutes * 60.0f, true);
                            OnArrival(world);
                        }
                        else
                        {
                            vel.Normalize();
                            vel *= GetSpeedPerMinute();
                            float elapsedMinutes = (float)time.ElapsedGameTime.TotalMinutes;
                            Position += elapsedMinutes * vel;
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
                        }
                        break;
                    }
                case State.ComingBack:
                    {
                        var target = Start;
                        var vel = (target - Position);
                        if (vel.Length() < 1.0f)
                        {
                            var balloonPorts = world.PlayerFaction.GetRooms().OfType<BalloonPort>().ToList();
                            Vector3 location = world.Camera.Position;
                            if (balloonPorts.Count != 0)
                            {
                                location = balloonPorts.First().GetBoundingBox().Center() + Vector3.Up * 10;
                            }

                            if (Vehicle == null || Vehicle.IsDead)
                            {
                                Vehicle = EntityFactory.CreateEntity<Balloon>("Balloon", balloonPorts.First().GetBoundingBox().Center() + Vector3.Up * 10);
                            }
                            Vehicle.GetComponent<BalloonAI>().State = BalloonAI.BalloonState.DeliveringGoods;
                            OnReturn(world);
                            AdventureState = State.Done;
                        }
                        else
                        {
                            vel.Normalize();
                            vel *= GetSpeedPerMinute();
                            float elapsedMinutes = (float)time.ElapsedGameTime.TotalMinutes;
                            Position += elapsedMinutes * vel;
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

    public class TradeAdventure : Adventure
    {
        public TradeAdventure()
        {
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
            var charisma = Party.Max(p => p.Stats.BuffedChar);
            float tradeGoodness = charisma - MathFunctions.Rand(0, 10.0f);
            var politics = world.Diplomacy.GetPolitics(owner, dest);
            List<ResourceAmount> destGoods = dest.Race.GenerateResources(world);
            List<ResourceAmount> tradeGoods = new List<ResourceAmount>();
            bool wasBadTrade = false;
            int randIters = 100;
            for (int iter = 0; iter < randIters; iter++)
            {
                var resourceType = Datastructures.SelectRandom(Resources);
                if (resourceType.NumResources == 0)
                {
                    continue;
                }
                var resource = ResourceLibrary.GetResourceByName(resourceType.ResourceType);
                bool liked = resource.Tags.Any(t => dest.Race.LikedResources.Contains(t));
                bool hated = resource.Tags.Any(t => dest.Race.HatedResources.Contains(t));

                if (tradeGoodness < 0 && liked)
                {
                    LastEvent = String.Format("{0} gave the {1} {2}, which made them very angry!", 
                                               Datastructures.SelectRandom(Party).Stats.FullName,
                                               dest.Race.Name, resourceType.ResourceType);
                    string badTrade = "You gave us something we hate.";
                    if (!politics.RecentEvents.Any(ev => ev.Description == badTrade))
                    {
                        politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
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
                    if (!politics.RecentEvents.Any(ev => ev.Description == goodTrade))
                    {
                        politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                        {
                            Description = goodTrade,
                            Change = 5,
                            Duration = new TimeSpan(4, 0, 0, 0),
                            Time = world.Time.CurrentDate
                        });
                    }
                }
                var resourceValue = GetValue(resource, dest);

                // Now find a resource of equal or greater value and trade for it.
                int randIter2 = 0;
                while (randIter2 < 100)
                {
                    var randGood = Datastructures.SelectRandom(destGoods);
                    if (randGood.NumResources == 0)
                    {
                        randIter2++;
                        continue;
                    }

                    var good = ResourceLibrary.GetResourceByName(randGood.ResourceType);
                    var randGoodValue = GetValue(good, dest);

                    // Trade the most of this resource we possibly can. If we want to 
                    // trade an item of lesser value, try to trade 1 good for as much of it as possible.
                    if (randGoodValue <= resourceValue)
                    {
                        int numToTrade = Math.Min((int)(resourceValue / randGoodValue), randGood.NumResources);
                        if (numToTrade * randGoodValue >= 0.75f * resourceValue)
                        {
                            randGood.NumResources -= numToTrade;
                            resourceType.NumResources--;
                            tradeGoods.Add(new ResourceAmount(good, numToTrade));
                            break;
                        }
                    }
                    // If we're trading upwards, try trading as much of our resource as possible for the valuable item.
                    else
                    {
                        int numToTrade = Math.Min((int)(randGoodValue / resourceValue), resourceType.NumResources);
                        if (numToTrade * resourceValue >= 0.75f * randGoodValue)
                        {
                            randGood.NumResources --;
                            resourceType.NumResources-=numToTrade;
                            tradeGoods.Add(new ResourceAmount(good, 1));
                            break;
                        }
                    }

                    randIter2++;
                }

                // We failed to find a good of equal value, so let's just trade money.
                if (randIter2 == 100)
                {
                    resourceType.NumResources--;
                    Money += resourceValue;
                }
            }


            Resources.AddRange(tradeGoods);
            if (!wasBadTrade)
            {
                LastEvent = String.Format("The trade party is returning home with {0} goods, and {1}.", Resources.Sum(r => r.NumResources), Money);
            }

            base.OnAction(world);
        }
    }
}
