// FindAndEatFoodAct.cs
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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// The creature finds food in a stockpile or BuildRoom, and eats it.
    /// </summary>
    public class FindAndEatFoodAct : CompoundCreatureAct
    {

        public FindAndEatFoodAct()
        {
            Name = "Find and Eat Edible";
            FoodTag = Resource.ResourceTags.PreparedFood;
            FallbackTag = Resource.ResourceTags.Edible;
        }

        public FindAndEatFoodAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Find and Eat Edible";
            FoodTag = Resource.ResourceTags.PreparedFood;
            FallbackTag = Resource.ResourceTags.Edible;
        }

        public Resource.ResourceTags FoodTag { get; set; }
        public Resource.ResourceTags FallbackTag { get; set; }

        public override void Initialize()
        {
            Tree = new Sequence(new Select(new GetResourcesAct(Agent, FoodTag), 
                                            new GetResourcesAct(Agent, FallbackTag)), 
                                new Select(new GoToChairAndSitAct(Agent), true),
                                new EatFootAct(Agent));
                
            base.Initialize();
        }
    }


    public class EatFootAct : CreatureAct
    {
        private Body FoodBody = null;

        public EatFootAct()
        {

        }


        public EatFootAct(CreatureAI creature) :
            base(creature)
        {

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

                yield return Act.Status.Fail;
                yield break;
            }

            FoodBody = null;
            Timer eatTimer = new Timer(3.0f, true);

            foreach (ResourceAmount resourceAmount in foods)
            {
                if (resourceAmount.NumResources > 0)
                {
                    List<Body> bodies = Agent.Creature.Inventory.RemoveAndCreate(new ResourceAmount(resourceAmount.ResourceType, 1), 
                        Inventory.RestockType.Any);
                    var resource = ResourceLibrary.GetResourceByName(resourceAmount.ResourceType);
                    Agent.Creature.NoiseMaker.MakeNoise("Chew", Agent.Creature.AI.Position);
                    if (bodies.Count == 0)
                    {
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
                                Agent.Creature.World.ParticleManager.Trigger("crumbs", foodPosition, Color.White,
                                    3);
                            }
                            yield return Act.Status.Running;
                        }

                        Agent.Creature.Status.Hunger.CurrentValue += resource.FoodContent;
                        Agent.Creature.AI.AddThought(Thought.ThoughtType.AteFood);
                        FoodBody.Delete();
                        yield return Act.Status.Success;
                    }
                    yield break;
                }
            }
        }
    }

}
