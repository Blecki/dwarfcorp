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
            FoodTag = Resource.ResourceTags.PreparedFood;
            FallbackTag = Resource.ResourceTags.Edible;
        }

        public FindAndEatFoodAct(CreatureAI agent, bool MustPay) :
            base(agent)
        {
            Name = "Find and Eat Edible";
            FoodTag = Resource.ResourceTags.PreparedFood;
            FallbackTag = Resource.ResourceTags.Edible;
            this.MustPay = MustPay;
        }

        public Resource.ResourceTags FoodTag { get; set; }
        public Resource.ResourceTags FallbackTag { get; set; }

        public override void Initialize()
        {
            Tree = new Sequence(new Select(new GetResourcesAct(Agent, FoodTag) { Name = "Get " + FoodTag },
                                            new GetResourcesAct(Agent, FallbackTag) { Name = "Get " + FallbackTag })
            { Name = "Get Food" },
                                new Select(new GoToChairAndSitAct(Agent), true) { Name = "Find a place to eat." },
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
                Agent.Creature.Gather(FoodBody);
            }

            base.OnCanceled();
        }

        public override IEnumerable<Act.Status> Run()
        {
            List<ResourceAmount> foods =
                Agent.Creature.Inventory.GetResources(new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Edible), Inventory.RestockType.Any);

            if (foods.Count == 0 && Agent.Creature.Faction == Agent.World.PlayerFaction)
            {
                Agent.SetMessage("Failed to eat. No food in inventory.");
                yield return Act.Status.Fail;
                yield break;
            }

            FoodBody = null;
            Timer eatTimer = new Timer(3.0f, true);

            foreach (ResourceAmount resourceAmount in foods)
            {
                if (resourceAmount.Count > 0)
                {
                    List<GameComponent> bodies = Agent.Creature.Inventory.RemoveAndCreate(new ResourceAmount(resourceAmount.Type, 1), 
                        Inventory.RestockType.Any);
                    var resource = Library.GetResourceType(resourceAmount.Type);
                    Agent.Creature.NoiseMaker.MakeNoise("Chew", Agent.Creature.AI.Position);
                    if (bodies.Count == 0)
                    {
                        Agent.SetMessage("Failed to eat. No food in inventory.");
                        yield return Act.Status.Fail;
                    }
                    else
                    {
                        FoodBody = bodies[0];

                        while (!eatTimer.HasTriggered)
                        {
                            eatTimer.Update(DwarfTime.LastTime);
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
                            {
                                Agent.Creature.World.ParticleManager.Trigger("crumbs", foodPosition, Color.White, 3);
                            }
                            yield return Act.Status.Running;
                        }

                        Agent.Creature.Stats.Hunger.CurrentValue += resource.FoodContent;

                        if (resource.Tags.Contains(Resource.ResourceTags.Alcohol))
                            Agent.Creature.AddThought("I had good ale recently.", new TimeSpan(0, 8, 0, 0), 10.0f);
                        else
                            Agent.Creature.AddThought("I ate good food recently.", new TimeSpan(0, 8, 0, 0), 5.0f);

                        FoodBody.GetRoot().Delete();

                        if (MustPay)
                        {
                            var depositAct = new DepositMoney(Agent, resource.MoneyValue);
                            foreach (var result in depositAct.Run())
                                if (result == Status.Running)
                                    yield return result;
                        }

                        yield return Act.Status.Success;
                    }
                    yield break;
                }
            }
        }
    }

}
