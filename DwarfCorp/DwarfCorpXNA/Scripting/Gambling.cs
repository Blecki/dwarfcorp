using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting
{
    public class GambleTask : Task
    {
        public GambleTask()
        {
            Name = "Gamble";
            ReassignOnDeath = false;
            BoredomIncrease = GameSettings.Default.Boredom_Gamble;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            if (agent.IsDead || agent.Status.Money < 10.0m || agent.World.Master.GamblingState.Participants.Count > 4)
            {
                return Feasibility.Infeasible;
            }
            return Feasibility.Feasible;
        }

        public override bool IsComplete(Faction faction)
        {
            return faction.World.Master.GamblingState.State == Gambling.Status.Ended;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GambleAct() { Agent = agent.AI, Game = agent.World.Master.GamblingState };
        }
    }

    public class GambleAct : CompoundCreatureAct
    {
        public Gambling Game { get; set; }

        public IEnumerable<Act.Status> Gamble()
        {
            while (Game.State != Gambling.Status.Ended && Game.Participants.Contains(Agent.Creature))
            {
                Agent.Physics.Face(Game.Location);
                Agent.Physics.Velocity *= 0.9f;
                Agent.Creature.CurrentCharacterMode = CharacterMode.Sitting;
                yield return Act.Status.Running;
            }
            yield return Act.Status.Success;
        }

        public override void Initialize()
        {
            Name = "Gamble";
            Game.Join(Agent.Creature);

            VoxelHandle voxel = new VoxelHandle(Agent.World.ChunkManager.ChunkData, GlobalVoxelCoordinate.FromVector3(Game.Location));

            if (voxel.IsValid)
            {
                Tree = new Sequence(new GoToVoxelAct(voxel, PlanAct.PlanType.Radius, Agent) { Name = "Go to gambling site.", Radius = 3.0f },
                                    new Wrap(Gamble)) | new Wrap(Cleanup);
                                    
            }
            else
            {
                Tree = new Always(Status.Fail);
            }
            base.Initialize();
        }

        public IEnumerable<Act.Status> Cleanup()
        {
            if (Game != null && Game.Participants.Contains(Agent.Creature))
            {
                Game.Participants.Remove(Agent.Creature);
            }
            yield return Act.Status.Success;
        }

        public override void OnCanceled()
        {
            foreach(var status in Cleanup())
            {
                //
            }
            base.OnCanceled();
        }
    }

    public class Gambling
    {
        public Microsoft.Xna.Framework.Vector3 Location = new Microsoft.Xna.Framework.Vector3();
        public List<Creature> Participants = new List<Creature>();
        public DwarfBux Pot = 0.0m;

        public enum Status
        {
            WaitingForPlayers,
            Gaming,
            Ended
        };

        public Status State;
        public int CurrentRound = 1;
        public int MaxRounds = 3;

        public void Join(Creature creature)
        {
            if (!Participants.Contains(creature))
            {
                if (Participants.Count == 0)
                {
                    var zone = creature.Faction.GetNearestRoom(creature.AI.Position);
                    var obj = creature.Faction.OwnedObjects.Where(b => b.Tags.Contains("Table")).OrderBy(b => (b.Position - creature.AI.Position).LengthSquared()).FirstOrDefault();
                    if (obj == null && zone == null)
                    {
                        Location = creature.AI.Position;
                    }
                    else if (obj != null)
                    {
                        var box = obj.GetBoundingBox();
                        Location = box.Center() + (box.Max.Y - box.Min.Y) * 0.5f * Microsoft.Xna.Framework.Vector3.Up;
                    }
                    else
                    {
                        Location = zone.GetBoundingBox().Center() + Microsoft.Xna.Framework.Vector3.Up;
                    }
                }
                Participants.Add(creature);
            }
            State = Status.WaitingForPlayers;
        }

        public Timer RoundTimer = new Timer(5.0f, false);
        public Timer WaitTimer = new Timer(20.0f, true);
        public CoinPileFixture PotFixture = null;

        public void PushParticipants()
        {
            var time = DwarfTime.LastTime;
            List<Creature> removals = new List<Creature>();
            for (int i = 0; i < Participants.Count; i++)
            {
                var p_i = Participants[i];
                if (VoxelHelpers.GetNeighbor(p_i.Physics.CurrentVoxel, new GlobalVoxelOffset(0, -1, 0)).IsEmpty)
                {
                    removals.Add(p_i);
                    continue;
                }

                for (int j = i + 1; j < Participants.Count; j++)
                {
                    var p_j = Participants[j];

                    if ((p_i.AI.Position - p_j.AI.Position).Length() < 1)
                    {
                        var diff = (p_i.AI.Position - p_j.AI.Position) + Microsoft.Xna.Framework.Vector3.Right * 0.01f;
                        diff.Normalize();
                        p_i.Physics.ApplyForce(diff * 10.0f, (float)time.ElapsedGameTime.TotalSeconds);
                        p_j.Physics.ApplyForce(-diff * 10.0f, (float)time.ElapsedGameTime.TotalSeconds);
                    }
                }
                float distToCenter = (p_i.AI.Position - Location).Length() - 2;
                if (Math.Abs(distToCenter) > 0.2f)
                {
                    var diff = (p_i.AI.Position - Location) + Microsoft.Xna.Framework.Vector3.Right * 0.1f;
                    p_i.Physics.ApplyForce(-distToCenter * diff * 10.0f, (float)time.ElapsedGameTime.TotalSeconds);
                }
            }
            foreach(var participant in removals)
            {
                Participants.Remove(participant);
            }
        }

        public void Update(DwarfTime time)
        {
            Participants.RemoveAll(r => r == null || r.IsDead || r.Physics == null || r.AI == null || r.IsAsleep || !r.Active);
            PushParticipants();
            switch (State)
            {
                case Status.WaitingForPlayers:
                    {
                        Participants.RemoveAll(creature => creature == null || creature.IsDead || creature.Status.Money < 10.0m || creature.Physics.IsInLiquid);
                        WaitTimer.Update(time);
                        if (WaitTimer.HasTriggered || Participants.Count >= 4)
                        {
                            if (Participants.Count >= 2)
                            {
                                foreach (var participant in Participants)
                                {
                                    if ((Location - participant.AI.Position).Length() > 4)
                                    {
                                        return;
                                    }
                                }
                                Participants[0].World.LogEvent("A new game of dice has started.", String.Format("There are {0} players.", Participants.Count));
                                State = Status.Gaming;
                                RoundTimer.Reset();
                                CurrentRound = 1;
                                if (PotFixture == null)
                                {
                                    PotFixture = new CoinPileFixture(Participants[0].Manager, Location)
                                    {
                                        Name = "Gambling pot"
                                    };
                                    PotFixture.SetFullness(0);
                                    Participants[0].Manager.RootComponent.AddChild(PotFixture);

                                }
                            }
                            else
                            {
                                if (Participants.Count > 0)
                                {
                                    Participants[0].World.LogEvent("The dice game ended prematurely.", "The dice players couldn't meet up.");
                                }
                                EndGame();
                            }
                            return;
                        }
                        break;
                    }
                case Status.Gaming:
                    {
                        RoundTimer.Update(time);
                        if (RoundTimer.HasTriggered)
                        {
                            NextRound();
                        }
                        return;
                    }
                case Status.Ended:
                    {
                        WaitTimer.Reset();
                        return;
                    }
            }
            
        }

        public void EndGame()
        {
            State = Status.Ended;

            if (Participants.Count > 0 && Pot > 0.0m)
            {
                Participants[0].World.LogEvent(String.Format("Dice: {0} dwarfs split a pot of {1}", Participants.Count, Pot));
                DwarfBux potDistribution = Pot / (decimal)Participants.Count;
                foreach (var participant in Participants)
                {
                    participant.Status.Money += potDistribution;
                    IndicatorManager.DrawIndicator((potDistribution).ToString(),
                     participant.AI.Position + Microsoft.Xna.Framework.Vector3.Up + Microsoft.Xna.Framework.Vector3.Forward * 0.1f, 10.0f,
                        GameSettings.Default.Colors.GetColor("Positive", Microsoft.Xna.Framework.Color.Green));
                }
                Pot = 0.0m;
            }


            if (PotFixture != null)
            {
                PotFixture.Delete();
                PotFixture = null;
            }
        }

        public void NextRound()
        {

            RoundTimer.Reset();
            int countBefore = Participants.Count;
            Participants.RemoveAll(creature => creature == null || creature.IsDead || creature.Status.Money < 10.0m || creature.Physics.IsInLiquid);
            int countAfter = Participants.Count;

            if (countAfter > 0 && countAfter < countBefore)
            {
                Participants[0].World.LogEvent(String.Format("Dice: {0} player(s) left the game.", countBefore - countAfter));
            }

            if (Participants.Count < 2)
            {
                EndGame();
                return;
            }


            foreach (var participant in Participants)
            {
                var money = participant.Status.Money;

                var bet = (decimal)(int)(MathFunctions.Rand(0.1f, 0.25f) * money);
                Pot += (DwarfBux)bet;
                participant.Status.Money -= (DwarfBux)bet;

                IndicatorManager.DrawIndicator((-(DwarfBux)bet).ToString(),
                    participant.AI.Position + Microsoft.Xna.Framework.Vector3.Up , 4.0f, 
                    GameSettings.Default.Colors.GetColor("Negative", Microsoft.Xna.Framework.Color.Red));
            }
            Participants[0].World.LogEvent(String.Format("Dice: Entering round {0}/{1}. The Pot is {2}.", CurrentRound, MaxRounds, Pot.ToString()));

            List<Creature> winners = new List<Creature>();
            List<int> rolls = new List<int>();
            int maxRoll = 0;
            foreach(var participant in Participants)
            {
                Microsoft.Xna.Framework.Vector3 vel = (participant.AI.Position - Location) + MathFunctions.RandVector3Cube() * 0.5f;
                vel.Normalize();
                vel *= 5;
                vel += Microsoft.Xna.Framework.Vector3.Up * 1.5f;
                participant.World.ParticleManager.Create("dice", participant.AI.Position, -vel, Microsoft.Xna.Framework.Color.White);
                int roll = MathFunctions.RandInt(1, 7);
                rolls.Add(roll);
                if (roll >= maxRoll)
                {
                    maxRoll = roll;
                }
            }

            for (int k = 0; k < rolls.Count; k++)
            {
                if (rolls[k] >= maxRoll)
                {
                    winners.Add(Participants[k]);
                }
            }

            if (winners.Count == 1)
            {
                var maxParticipant = winners[0];
                maxParticipant.Status.Money += Pot;
                winners[0].World.LogEvent(String.Format("{0} won {1} at dice.", maxParticipant.Stats.FullName, Pot));
                maxParticipant.NoiseMaker.MakeNoise("Pleased", maxParticipant.AI.Position, true, 0.5f);
                IndicatorManager.DrawIndicator((Pot).ToString(),
                 maxParticipant.AI.Position + Microsoft.Xna.Framework.Vector3.Up + Microsoft.Xna.Framework.Vector3.Forward * 0.1f, 10.0f,
                    GameSettings.Default.Colors.GetColor("Positive", Microsoft.Xna.Framework.Color.Green));
                Pot = 0.0m;
                maxParticipant.AddThought(Thought.ThoughtType.WonGambling);
            }
            else
            {
                Participants[0].World.LogEvent(String.Format("Nobody won this round of dice. The Pot is {0}.", Pot.ToString()));
            }

            if (PotFixture != null)
            {
                PotFixture.SetFullness((float)(decimal)(Pot / 500.0m));
            }

            CurrentRound++;
            if (CurrentRound > MaxRounds)
            {
                CurrentRound = 1;
                EndGame();
            }
        }
    }
}
