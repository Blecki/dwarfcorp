// FindBedAndSleepAct.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item with the tag "bed", goes to it, and sleeps in it.
    /// </summary>
    public class FindBedAndSleepAct : CompoundCreatureAct
    {

        public FindBedAndSleepAct()
        {
            Name = "Find bed and sleep";
        }

        public FindBedAndSleepAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Find bed and sleep";
        }


        public override void OnCanceled()
        {
            foreach(var status in Creature.Unreserve("Bed"))
            {

            }
            base.OnCanceled();
        }

        public override void Initialize()
        {
            Body closestItem = Agent.Faction.FindNearestItemWithTags("Bed", Agent.Position, true);
            Zone closestZone = Agent.Faction.GetNearestRoom(Agent.Position);
           
            if (Agent.Status.Energy.IsDissatisfied() && closestItem != null)
            {
                closestItem.ReservedFor = Agent;
                Creature.AI.Blackboard.SetData("Bed", closestItem);
                closestItem.IsReserved = true;
                closestItem.ReservedFor = Agent;
                Act unreserveAct = new Wrap(() => Creature.Unreserve("Bed"));
                Tree = 
                    new Sequence
                    (
                        new GoToEntityAct(closestItem, Creature.AI),
                        new TeleportAct(Creature.AI) {Location = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f)},
                        new SleepAct(Creature.AI) { RechargeRate = 1.0f, Teleport = true, TeleportLocation = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f) },
                        unreserveAct
                    ) | unreserveAct;
            }
            else if (Agent.Status.Energy.IsDissatisfied() && closestItem == null && closestZone != null)
            {
                Creature.AddThought(Thought.ThoughtType.SleptOnGround);

                Tree = new Sequence(new GoToZoneAct(Creature.AI, closestZone),
                                    new SleepAct(Creature.AI)
                                    {
                                        RechargeRate = 1.0f
                                    });
            }
            else if (Agent.Status.Energy.IsDissatisfied() && closestItem == null && closestZone == null)
            {
                Creature.AddThought(Thought.ThoughtType.SleptOnGround);

                Tree = new SleepAct(Creature.AI)
                {
                    RechargeRate = 1.0f
                };
            }
            else
            {
                Tree = null;
            }
            base.Initialize();
        }
    }

    /// <summary>
    /// A creature finds an item with the tag "bed", goes to it, and sleeps in it.
    /// </summary>
    public class GetHealedAct : CompoundCreatureAct
    {

        public GetHealedAct()
        {
            Name = "Heal thyself";
        }

        public GetHealedAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Heal thyself";
        }

        public override void OnCanceled()
        {
            foreach (var status in Creature.Unreserve("Bed"))
            {

            }
            base.OnCanceled();
        }

        public override void Initialize()
        {
            Body closestItem = Agent.Faction.FindNearestItemWithTags("Bed", Agent.Position, true);


            if (closestItem != null)
            {
                closestItem.ReservedFor = Agent;
                Creature.AI.Blackboard.SetData("Bed", closestItem);
                Act unreserveAct = new Wrap(() => Creature.Unreserve("Bed"));
                Tree =
                    new Sequence
                    (
                        new GoToEntityAct(closestItem, Creature.AI),
                        new TeleportAct(Creature.AI) { Location = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f) },
                        new SleepAct(Creature.AI) { HealRate = 1.0f, RechargeRate = 1.0f, Teleport = true, TeleportLocation = closestItem.GetRotatedBoundingBox().Center() + new Vector3(-0.0f, 0.75f, -0.0f) , Type = SleepAct.SleepType.Heal},
                        unreserveAct
                    ) | unreserveAct;
            }
            else
            {
                if (Agent.Faction == Agent.World.PlayerFaction)
                {
                    Agent.World.MakeAnnouncement(String.Format("{0} passed out.", Agent.Stats.FullName));
                }
                Tree = new SleepAct(Creature.AI) { HealRate = 0.1f, RechargeRate = 1.0f, Teleport = false, Type = SleepAct.SleepType.Heal };
            }
            base.Initialize();
        }
    }
}
