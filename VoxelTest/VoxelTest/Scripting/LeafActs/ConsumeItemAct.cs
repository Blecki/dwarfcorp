using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature eats an item in its hands to satisfy its hunger.
    /// </summary>
    public class ConsumeItemAct : CreatureAct
    {
        public string TargetID = "HeldObject";

        public LocatableComponent Target { get { return Agent.Blackboard.GetData<LocatableComponent>(TargetID); } set{ Agent.Blackboard.SetData(TargetID, value);} }

        public ConsumeItemAct(string itemtoConsume)
        {
            TargetID = itemtoConsume;
        }


        public ConsumeItemAct()
        {
            
        }

        public ConsumeItemAct(CreatureAIComponent agent) :
            base(agent)
        {
            Name = "Consume " + TargetID;
        } 

        public bool TargetIsInHands()
        {
            return Target == Agent.Hands.GetFirstGrab();
        }

        public bool TargetIsFood()
        {
            return Target.GetChildrenOfTypeRecursive<FoodComponent>().Count > 0;
        }

        public override IEnumerable<Status> Run()
        {
            if(TargetIsInHands() && TargetIsFood())
            {
                FoodComponent food = Target.GetChildrenOfTypeRecursive<FoodComponent>().First();

                while(food.FoodAmount > 1e-12)
                {
                    float eatAmount = (float)(LastTime.ElapsedGameTime.TotalSeconds) * Creature.Stats.EatSpeed;

                    food.FoodAmount -= eatAmount;
                    Creature.Status.Hunger.CurrentValue += eatAmount;
                    Creature.NoiseMaker.MakeNoise("Chew", Creature.AI.Position);
                    yield return Status.Running;
                }

                Creature.Hands.UngrabFirst(Creature.AI.Position);

                food.GetRootComponent().Die();
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Happy);
                yield return Status.Success;
                yield break;
            }
            Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
            yield return Status.Fail;
        }
    }
}
