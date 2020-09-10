using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class FindAndEatFoodAct : CompoundCreatureAct
    {
        private bool MustPay = false;

        public FindAndEatFoodAct()
        {
            Name = "Find and Eat Edible";
            FoodTag = "PreparedFood";
            FallbackTag = "Edible";
        }

        public FindAndEatFoodAct(CreatureAI agent, bool MustPay) :
            base(agent)
        {
            Name = "Find and Eat Edible";
            FoodTag = "PreparedFood";
            FallbackTag = "Edible";
            this.MustPay = MustPay;
        }

        public String FoodTag { get; set; }
        public String FallbackTag { get; set; }

        public override void Initialize()
        {
            Tree = new Sequence(new Select(new GetResourcesWithTag(Agent, FoodTag) { Name = "Get " + FoodTag },
                                            new GetResourcesWithTag(Agent, FallbackTag) { Name = "Get " + FallbackTag })
            { Name = "Get Food" },
                                new Select(
                                    new GoToChairAndSitAct(Agent), 
                                    new Always(Status.Success)) { Name = "Find a place to eat." },
                                new EatFoodAct(Agent, MustPay));
                
            base.Initialize();
        }
    }


    public class EatFoodAct : CreatureAct
    {
        private GameComponent FoodBody = null;
        private bool MustPay = false;

        public EatFoodAct(CreatureAI creature, bool MustPay) :
            base(creature)
        {
            Name = "Eat food";
            this.MustPay = MustPay;
        }

        public override void OnCanceled()
        {
            if (FoodBody != null)
            {
                FoodBody.Active = true;
                Agent.Creature.Gather(FoodBody, TaskPriority.Urgent);
            }

            base.OnCanceled();
        }

        public override IEnumerable<Act.Status> Run()
        {
            var foods = Agent.Creature.Inventory.EnumerateResources(new ResourceTagAmount("Edible", 1), Inventory.RestockType.Any);

            if (foods.Count == 0 && Agent.Creature.Faction == Agent.World.PlayerFaction)
            {
                Agent.SetTaskFailureReason("Failed to eat. No food in inventory.");
                yield return Act.Status.Fail;
                yield break;
            }

            FoodBody = null;
            Timer eatTimer = new Timer(3.0f, true);

            foreach (var resourceAmount in foods)
            {
                FoodBody = Agent.Creature.Inventory.RemoveAndCreate(resourceAmount, Inventory.RestockType.Any);
                Agent.Creature.NoiseMaker.MakeNoise("Chew", Agent.Creature.AI.Position);

                if (FoodBody == null)
                {
                    Agent.SetTaskFailureReason("Failed to eat. No food in inventory.");
                    yield return Act.Status.Fail;
                }
                else
                {
                    while (!eatTimer.HasTriggered)
                    {
                        eatTimer.Update(Agent.FrameDeltaTime);
                        Matrix rot = Agent.Creature.Physics.LocalTransform;
                        rot.Translation = Vector3.Zero;
                        FoodBody.LocalTransform = Agent.Creature.Physics.LocalTransform;
                        Vector3 foodPosition = Agent.Creature.Physics.Position + Vector3.Up * 0.05f + Vector3.Transform(Vector3.Forward, rot) * 0.5f;
                        FoodBody.LocalPosition = foodPosition;
                        FoodBody.PropogateTransforms();
                        FoodBody.Active = false;
                        Agent.Creature.Physics.Velocity = Vector3.Zero;
                        Agent.Creature.CurrentCharacterMode = CharacterMode.Sitting;
                        if (MathFunctions.RandEvent(0.05f))
                            Agent.Creature.World.ParticleManager.Trigger("crumbs", foodPosition, Color.White, 3);
                        yield return Act.Status.Running;
                    }

                    if (resourceAmount.ResourceType.HasValue(out var resource))
                    {
                        Agent.Creature.Stats.Hunger.CurrentValue += resourceAmount.FoodContent;

                        if (resource.Tags.Contains("Alcohol"))
                            Agent.Creature.AddThought("I had good ale recently.", new TimeSpan(0, 8, 0, 0), 10.0f);
                        else
                            Agent.Creature.AddThought("I ate good food recently.", new TimeSpan(0, 8, 0, 0), 5.0f);

                        FoodBody.GetRoot().Delete();

                        if (MustPay)
                        {
                            if (Agent.World.PersistentData.CorporateFoodCostPolicy > 1.0f)
                                Agent.Creature.AddThought("I can't believe I have to pay so much for food.", new TimeSpan(4, 0, 0), -10 * Agent.World.PersistentData.CorporateFoodCostPolicy);
                            else if (Agent.World.PersistentData.CorporateFoodCostPolicy < 0.6f)
                                Agent.Creature.AddThought("Food here is cheap!", new TimeSpan(4, 0, 0), 30);

                            var depositAct = new DepositMoney(Agent, resourceAmount.MoneyValue * Agent.World.PersistentData.CorporateFoodCostPolicy);
                            foreach (var result in depositAct.Run())
                                if (result == Status.Running)
                                    yield return result;
                            if (Agent.Stats != null && Agent.Stats.Money < 0)
                                Agent.Creature.AddThought("I'm broke.", new TimeSpan(4, 0, 0), -50);
                        }
                    }

                    yield return Act.Status.Success;
                }
                yield break;
            }
        }
    }
}
