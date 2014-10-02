using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public static class CreatureExtensions
    {
        public static IEnumerable<Act.Status> ClearBlackboardData(this Creature agent, string data)
        {
            if(data == null)
            {
                yield return Act.Status.Fail;
            }
            else
            {
                agent.AI.Blackboard.Erase(data);
                yield return Act.Status.Success;
            }
        }


        public static IEnumerable<Act.Status> RestockAll(this Creature agent)
        {
            foreach (ResourceAmount resource in agent.Inventory.Resources)
            {
                if (resource.NumResources > 0)
                    agent.AI.GatherManager.StockOrders.Add(new GatherManager.StockOrder()
                    {
                        Destination = null,
                        Resource = resource
                    });
            }

            yield return Act.Status.Success;
        }

        public static IEnumerable<Act.Status> ConsumeItem(this Creature agent, string item)
        {
            Body target = agent.AI.Blackboard.GetData<Body>(item);
            bool targetInHands = target == agent.Hands.GetFirstGrab();
            bool targetIsFood = target.GetChildrenOfTypeRecursive<Food>().Count > 0;
            ;
            if(targetInHands && targetIsFood)
            {
                Food food = target.GetChildrenOfTypeRecursive<Food>().First();

                while(food.FoodAmount > 1e-12)
                {
                    float eatAmount = (float) (Act.LastTime.ElapsedGameTime.TotalSeconds) * agent.Stats.EatSpeed;

                    food.FoodAmount -= eatAmount;
                    agent.Status.Hunger.CurrentValue += eatAmount;
                    agent.NoiseMaker.MakeNoise("Chew", agent.AI.Position);
                    yield return Act.Status.Running;
                }

                agent.Hands.UngrabFirst(agent.AI.Position);

                food.GetRootComponent().Die();
                agent.DrawIndicator(IndicatorManager.StandardIndicators.Happy);
                yield return Act.Status.Success;
                yield break;
            }
            agent.DrawIndicator(IndicatorManager.StandardIndicators.Question);
            yield return Act.Status.Fail;
        }

        public static IEnumerable<Act.Status> Dig(this Creature agent, string voxel, float energyLoss)
        {
            agent.Sprite.ResetAnimations(Creature.CharacterMode.Attacking);
            while(true)
            {
                agent.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                VoxelRef blackBoardVoxelRef = agent.AI.Blackboard.GetData<VoxelRef>(voxel);

                if(blackBoardVoxelRef == null)
                {
                    agent.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Act.Status.Fail;
                    break;
                }

                Voxel vox = blackBoardVoxelRef.GetVoxel(false);
                if(vox == null || vox.Health <= 0.0f || !agent.Faction.IsDigDesignation(vox))
                {
                    if(vox != null && vox.Health <= 0.0f)
                    {
                        vox.Kill();
                    }
                    agent.Stats.NumBlocksDestroyed++;
                    agent.Stats.XP += Math.Max((int)(VoxelLibrary.GetVoxelType(blackBoardVoxelRef.TypeName).StartingHealth / 10), 1);
                    agent.CurrentCharacterMode = Creature.CharacterMode.Idle;
                    yield return Act.Status.Success;
                    break;
                }

                agent.LocalTarget = vox.Position + new Vector3(0.5f, 0.5f, 0.5f);
                Vector3 output = agent.Controller.GetOutput((float) Act.LastTime.ElapsedGameTime.TotalSeconds, agent.LocalTarget, agent.Physics.GlobalTransform.Translation);
                agent.Physics.ApplyForce(output, Act.Dt);
                output.Y = 0.0f;

                if((agent.LocalTarget - agent.Physics.GlobalTransform.Translation).Y > 0.3)
                {
                    agent.AI.Jump(Act.LastTime);
                }

                agent.Physics.Velocity = new Vector3(agent.Physics.Velocity.X * 0.5f, agent.Physics.Velocity.Y, agent.Physics.Velocity.Z * 0.5f);

                agent.Attacks[0].Perform(vox, Act.LastTime, agent.Stats.BaseDigSpeed);
                agent.Status.Energy.CurrentValue -= energyLoss * Act.Dt * agent.Stats.Tiredness;
                yield return Act.Status.Running;
            }


        }
 
    
    }


}
