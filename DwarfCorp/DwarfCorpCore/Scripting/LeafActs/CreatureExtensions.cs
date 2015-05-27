using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DwarfCorp.GameStates;
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

        public static IEnumerable<Act.Status> EatStockedFood(this Creature agent)
        {
            List<ResourceAmount> foods = agent.Faction.ListResourcesWithTag(Resource.ResourceTags.Food);

            if (foods.Count == 0)
            {

                if (agent.Allies == "Dwarf")
                {
                    PlayState.AnnouncementManager.Announce("We're out of food!", "Our stockpiles don't have any food. Our employees will starve!");
                }
                yield return Act.Status.Fail;
                yield break;
            }
            else
            {
                foreach (ResourceAmount resource in foods)
                {
                    if (resource.NumResources > 0)
                    {
                        bool removed = agent.Faction.RemoveResources(new List<ResourceAmount>() { new ResourceAmount(resource.ResourceType, 1) }, agent.AI.Position);
                        agent.Status.Hunger.CurrentValue += resource.ResourceType.FoodContent;
                        agent.NoiseMaker.MakeNoise("Chew", agent.AI.Position);
                        if (!removed)
                        {
                            yield return Act.Status.Fail;
                        }
                        else
                        {
                            agent.DrawIndicator(resource.ResourceType.Image);
                            agent.AI.AddThought(Thought.ThoughtType.AteFood);
                            yield return Act.Status.Success;
                        }
                        yield break;
                    }
                }

                if (agent.Allies == "Dwarf")
                {
                    PlayState.AnnouncementManager.Announce("We're out of food!", "Our stockpiles don't have any food. Our employees will starve!");
                }

                yield return Act.Status.Fail;
                yield break;
            }
        }

        public static IEnumerable<Act.Status> FindAndReserve(this Creature agent, string tag, string thing)
        {
            Body closestItem = agent.Faction.FindNearestItemWithTags(tag, agent.AI.Position, true);

            if (closestItem != null)
            {
                //PlayState.AnnouncementManager.Announce("Creature " + agent.GlobalID + " reserves " + closestItem.Name + " " + closestItem.GlobalID, "");
                closestItem.ReservedFor = agent.AI;
                closestItem.IsReserved = true;
                agent.AI.Blackboard.Erase(thing);
                agent.AI.Blackboard.SetData(thing, closestItem);
                yield return Act.Status.Success;
                yield break;
            }

            yield return Act.Status.Fail;
        }

        public static IEnumerable<Act.Status> Reserve(this Creature agent, string thing)
        {
            Body objectToHit = agent.AI.Blackboard.GetData<Body>(thing);

            if (objectToHit != null && objectToHit.ReservedFor == null && !objectToHit.IsReserved)
            {
                //PlayState.AnnouncementManager.Announce("Creature " + agent.GlobalID + " reserves " + objectToHit.Name + " " + objectToHit.GlobalID, "");
                objectToHit.IsReserved = true;
                objectToHit.ReservedFor = agent.AI;
            }

            yield return Act.Status.Success;
        }

        public static IEnumerable<Act.Status> Unreserve(this Creature agent, string thing)
        {
            Body objectToHit = agent.AI.Blackboard.GetData<Body>(thing);

            if (objectToHit != null && objectToHit.ReservedFor == agent.AI)
            {
                //PlayState.AnnouncementManager.Announce("Creature " + agent.GlobalID + " unreserves " + objectToHit.Name + " " + objectToHit.GlobalID, "");
                objectToHit.IsReserved = false;
                objectToHit.ReservedFor = null;
            }

            yield return Act.Status.Success;
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

        
        public static IEnumerable<Act.Status> Dig(this Creature agent, string voxel, float energyLoss)
        {
            Vector3 LocalTarget = agent.AI.Position;
            agent.Sprite.ResetAnimations(Creature.CharacterMode.Attacking);
            while(true)
            {
                agent.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                Voxel blackBoardVoxel = agent.AI.Blackboard.GetData<Voxel>(voxel);

                if(blackBoardVoxel == null)
                {
                    agent.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Act.Status.Fail;
                    break;
                }

                Voxel vox = blackBoardVoxel;
                if(vox.Health <= 0.0f || !agent.Faction.IsDigDesignation(vox))
                {
                    agent.AI.AddXP(Math.Max((int)(VoxelLibrary.GetVoxelType(blackBoardVoxel.TypeName).StartingHealth / 4), 1));
                    if(vox.Health <= 0.0f)
                    {
                        vox.Kill();
                    }
                    agent.Stats.NumBlocksDestroyed++;
                    agent.CurrentCharacterMode = Creature.CharacterMode.Idle;
                    yield return Act.Status.Success;
                    break;
                }
                agent.Physics.Face(vox.Position + Vector3.One * 0.5f);
                agent.Physics.Velocity *= 0.9f;
                agent.Attacks[0].Perform(agent.Physics.Position, vox, DwarfTime.LastTime, agent.Stats.BaseDigSpeed, agent.Faction.Name);
                yield return Act.Status.Running;
            }


        }
 
    
    }


}
