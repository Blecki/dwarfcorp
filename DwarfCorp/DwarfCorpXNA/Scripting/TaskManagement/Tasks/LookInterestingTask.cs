// LookInterestingTask.cs
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
    /// Tells a creature that it should do something (anything) since there 
    /// is nothing else to do.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class LookInterestingTask : Task
    {
        public LookInterestingTask()
        {
            Name = "Look Interesting";
            Priority = PriorityType.Eventually;
        }

        public override Task Clone()
        {
            return new LookInterestingTask();
        }

        public IEnumerable<Act.Status> ConverseFriends(CreatureAI c)
        {
            CreatureAI minionToConverse = null;
            foreach (CreatureAI minion in c.Faction.Minions)
            {
                if (minion == c || minion.Creature.IsAsleep)
                    continue;

                float dist = (minion.Position - c.Position).Length();

                if (dist < 2 && MathFunctions.Rand(0, 1) < 0.1f)
                {
                    minionToConverse = minion;
                    break;
                }
            }
            if (minionToConverse != null)
            {
                c.Converse(minionToConverse);
                Timer converseTimer = new Timer(5.0f, true);
                while (!converseTimer.HasTriggered)
                {
                    converseTimer.Update(DwarfTime.LastTime);
                    yield return Act.Status.Running;
                }
            }
            yield return Act.Status.Success;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return !agent.AI.IsPosessed ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature creature)
        {
            if (creature.AI.IsPosessed)
            {
                return null;
            }

            if (!creature.Faction.Race.IsIntelligent || !creature.IsOnGround)
            {
                return creature.AI.ActOnWander();
            }
            
            var rooms = creature.Faction.GetRooms();
            var items = creature.Faction.OwnedObjects.OfType<Flag>().ToList();

            bool goToItem = MathFunctions.RandEvent(0.2f);
            if (goToItem && items.Count > 0)
            {
                return new GoToEntityAct(Datastructures.SelectRandom(items), creature.AI) & new Wrap(() => ConverseFriends(creature.AI));
            }

            bool getDrink = MathFunctions.RandEvent(0.005f);
            if (getDrink && creature.Faction.HasResources(new List<Quantitiy<Resource.ResourceTags>>(){new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Alcohol)}))
            {
                return new FindAndEatFoodAct(creature.AI) { FoodTag = Resource.ResourceTags.Alcohol, FallbackTag = Resource.ResourceTags.Alcohol};
            }

            return creature.AI.ActOnWander();
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }
    }
}
