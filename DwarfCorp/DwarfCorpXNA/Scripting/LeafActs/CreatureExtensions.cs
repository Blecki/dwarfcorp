// CreatureExtensions.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
            List<ResourceAmount> foods =
                agent.Inventory.Resources.GetResources(new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Edible));

            if (foods.Count == 0 && agent.Allies == "Dwarf")
            {

                agent.Manager.World.MakeAnnouncement("We're out of food!");
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
                yield return Act.Status.Fail;
                yield break;
            }

            Body foodBody = null;
            Timer eatTimer = new Timer(3.0f, true);

            foreach (ResourceAmount resourceAmount in foods)
            {
                if (resourceAmount.NumResources > 0)
                {
                    List<Body> bodies = agent.Inventory.RemoveAndCreate(new ResourceAmount(resourceAmount.ResourceType, 1));
                    var resource = ResourceLibrary.GetResourceByName(resourceAmount.ResourceType);
                    agent.Status.Hunger.CurrentValue += resource.FoodContent;
                    agent.NoiseMaker.MakeNoise("Chew", agent.AI.Position);
                    if (bodies.Count == 0)
                    {
                        agent.Manager.World.MakeAnnouncement("We're out of food!");
                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.5f);
                        yield return Act.Status.Fail;
                    }
                    else
                    {
                        foodBody = bodies[0];

                        while (!eatTimer.HasTriggered)
                        {
                            eatTimer.Update(DwarfTime.LastTime);
                            Matrix rot = agent.Physics.LocalTransform;
                            rot.Translation = Vector3.Zero;
                            foodBody.LocalTransform = agent.Physics.LocalTransform;
                            Vector3 foodPosition = agent.Physics.Position + Vector3.Up * 0.05f + Vector3.Transform(Vector3.Forward, rot) * 0.5f;
                            foodBody.LocalPosition = foodPosition;
                            foodBody.Active = false;
                            agent.Physics.Velocity = Vector3.Zero;
                            agent.CurrentCharacterMode = CharacterMode.Sitting;
                            if (MathFunctions.RandEvent(0.05f))
                            {
                                agent.World.ParticleManager.Trigger("crumbs", foodPosition, Color.White,
                                    3);
                            }
                            yield return Act.Status.Running;
                        }


                        agent.AI.AddThought(Thought.ThoughtType.AteFood);
                        foodBody.Delete();
                        yield return Act.Status.Success;
                    }
                    yield break;
                }
            }

            /*
            List<ResourceAmount> foods = agent.Faction.ListResourcesWithTag(Resource.ResourceTags.Edible);

            if (foods.Count == 0)
            {

                if (agent.Allies == "Dwarf")
                {
<<<<<<< HEAD
                    agent.Manager.World.MakeAnnouncement("Our stockpiles don't have any food. Our employees will starve!");
=======
                    agent.Manager.World.MakeAnnouncement("We're out of food!", "Our stockpiles don't have any food. Our employees will starve!");
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
>>>>>>> d00c96c811e5559197158a0b31e018c5ccc324e3
                }
                yield return Act.Status.Fail;
                yield break;
            }
            else
            {
                foreach (ResourceAmount resourceAmount in foods)
                {
                    if (resourceAmount.NumResources > 0)
                    {
                        Resource resource = ResourceLibrary.GetResourceByName(resourceAmount.ResourceType);
                        bool removed = agent.Faction.RemoveResources(new List<ResourceAmount>() { new ResourceAmount(resourceAmount.ResourceType, 1) }, agent.AI.Position);
                        agent.Status.Hunger.CurrentValue += resource.FoodContent;
                        agent.NoiseMaker.MakeNoise("Chew", agent.AI.Position);
                        if (!removed)
                        {
                            yield return Act.Status.Fail;
                        }
                        else
                        {
                            agent.DrawIndicator(resource.Image, resource.Tint);
                            agent.AI.AddThought(Thought.ThoughtType.AteFood);
                            yield return Act.Status.Success;
                        }
                        yield break;
                    }
                }

                if (agent.Allies == "Dwarf")
                {
<<<<<<< HEAD
                    agent.Manager.World.MakeAnnouncement("Our stockpiles don't have any food. Our employees will starve!");
=======
                    agent.Manager.World.MakeAnnouncement("We're out of food!", "Our stockpiles don't have any food. Our employees will starve!");
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
>>>>>>> d00c96c811e5559197158a0b31e018c5ccc324e3
                }

                yield return Act.Status.Fail;
                yield break;
            }
             */
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

        /// <summary>
        /// This is the underlying Dig behavior that dwarves follow while digging.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="voxel">The voxel.</param>
        /// <param name="energyLoss">The energy loss the dwarf gets per block mined.</param>
        /// <returns>Success when the block is mined, fail if it fails to be mined, and running otherwise.</returns>
        public static IEnumerable<Act.Status> Dig(this Creature agent, string voxel, float energyLoss)
        {
            agent.Sprite.ResetAnimations(CharacterMode.Attacking);

            // Block since we're in a coroutine.
            while(true)
            {
                // Get the voxel stored in the agent's blackboard.
                var vox = agent.AI.Blackboard.GetData<Voxel>(voxel);

                // Somehow, there wasn't a voxel to mine.
                if(vox == null)
                {
                    agent.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Act.Status.Fail;
                    break;
                }

                // If the voxel has already been destroyed, just ignore it and return.
                if(vox.Health <= 0.0f || !agent.Faction.IsDigDesignation(vox))
                {
                    agent.CurrentCharacterMode = CharacterMode.Idle;
                    yield return Act.Status.Success;
                    break;
                }

                // Look at the block and slow your velocity down.
                agent.Physics.Face(vox.Position + Vector3.One * 0.5f);
                agent.Physics.Velocity *= 0.9f;

                // Play the attack animations.
                agent.CurrentCharacterMode = CharacterMode.Attacking;
                agent.Sprite.ResetAnimations(agent.CurrentCharacterMode);
                agent.Sprite.PlayAnimations(agent.CurrentCharacterMode);

                // Wait until an attack was successful...
                foreach (var status in 
                    agent.Attacks[0].Perform(agent, 
                            agent.Physics.Position, 
                            vox, DwarfTime.LastTime, 
                            agent.Stats.BaseDigSpeed, 
                            agent.Faction.Name))
                {
                    if (status == Act.Status.Running)
                    {
                        agent.Physics.Face(vox.Position + Vector3.One*0.5f);
                        agent.Physics.Velocity *= 0.9f;

                        // Debug drawing.
                        if (agent.AI.DrawPath)
                            Drawer3D.DrawLine(vox.Position, agent.AI.Position, Color.Green, 0.25f);
                        yield return Act.Status.Running;
                    }
                }

                // If the voxel has been destroyed by you, gather it.
                if (vox.Health <= 0.0f)
                {
                    var voxelType = VoxelLibrary.GetVoxelType(vox.TypeName);
                    agent.AI.AddXP(Math.Max((int)(voxelType.StartingHealth / 4), 1));
                    agent.Stats.NumBlocksDestroyed++;
                    agent.World.GoalManager.OnGameEvent(new Goals.Events.DigBlock(voxelType, agent));

                    List<Body> items = vox.Kill();

                    if (items != null)
                    {
                        foreach (Body item in items)
                        {
                            agent.Gather(item);
                        }
                    }                   
                }

                // Wait until the animation is done playing before continuing.
                while (!agent.Sprite.CurrentAnimation.IsDone() && agent.Sprite.CurrentAnimation.IsPlaying)
                {
                    agent.Physics.Face(vox.Position + Vector3.One * 0.5f);
                    agent.Physics.Velocity *= 0.9f;
                    yield return Act.Status.Running;
                }

                // Pause the animation and wait for a recharge timer.
                agent.Sprite.PauseAnimations(agent.CurrentCharacterMode);


                // Wait for a recharge timer to trigger.
                agent.Attacks[0].RechargeTimer.Reset();
                while (!agent.Attacks[0].RechargeTimer.HasTriggered)
                {
                    agent.Attacks[0].RechargeTimer.Update(DwarfTime.LastTime);
                    agent.Physics.Face(vox.Position + Vector3.One * 0.5f);
                    agent.Physics.Velocity *= 0.9f;
                    yield return Act.Status.Running;
                }

                agent.CurrentCharacterMode = CharacterMode.Idle;

                yield return Act.Status.Running;
            }


        }
 
    
    }


}
