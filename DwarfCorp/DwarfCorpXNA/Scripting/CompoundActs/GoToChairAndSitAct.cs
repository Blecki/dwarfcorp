// GoToChairAndSitAct.cs
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
using System.Runtime.Remoting.Messaging;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class GoToChairAndSitAct : CompoundCreatureAct
    {
        public float SitTime { get; set; }

        public GoToChairAndSitAct()
        {
            Name = "Go to chair and sit";
            SitTime = 30.0f;
        }

        public GoToChairAndSitAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Go to chair and sit";
            SitTime = 30.0f;
        }


        public void ConverseFriends()
        {
            foreach (CreatureAI minion in Creature.Faction.Minions)
            {
                if (minion == Creature.AI || minion.Creature.IsAsleep)
                    continue;

                float dist = (minion.Position - Creature.AI.Position).Length();

                if (dist < 2 && MathFunctions.Rand(0, 1) < 0.1f)
                {
                    Creature.AI.Converse(minion);
                }
            }
        }

        public IEnumerable<Status> WaitUntilBored()
        {
            Timer waitTimer = new Timer(SitTime, false);
            Vector3 snapPosition = Agent.Position + new Vector3(0, 0.2f, 0);
            Body body = Creature.AI.Blackboard.GetData<Body>("Chair");

            if (body == null || body.IsDead)
            {
                Creature.OverrideCharacterMode = false;
                yield return Status.Success;
                yield break;
            }

            while (true)
            {
                if (Creature.AI.Tasks.Count > 1)
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Status.Energy.IsUnhappy())
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Status.Hunger.IsUnhappy())
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                if (Creature.AI.Sensor.Enemies.Count > 0)
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                waitTimer.Update(DwarfTime.LastTime);

                if (waitTimer.HasTriggered)
                {
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Success;
                }

                ConverseFriends();


                Agent.Position = snapPosition;
                Agent.Physics.IsSleeping = true;
                Agent.Physics.Velocity = Vector3.Zero;
                Creature.CurrentCharacterMode = Creature.CharacterMode.Sitting;
                Creature.OverrideCharacterMode = true;
                yield return Status.Running;
            }
        }

        public override void Initialize()
        {
            Creature.OverrideCharacterMode = false;
           
            Tree = new Sequence(new ClearBlackboardData(Creature.AI, "Chair"),
                                new Wrap(() => Creature.FindAndReserve("Chair", "Chair")),
                                new GoToTaggedObjectAct(Creature.AI) {Tag = "Chair", Teleport = true, TeleportOffset = new Vector3(0, 0.1f, 0), ObjectName = "Chair"},
                                new Wrap(WaitUntilBored),
                                new Wrap(() => Creature.Unreserve("Chair"))) | new Wrap(() => Creature.Unreserve("Chair"));
            base.Initialize();
        }

        public override void OnCanceled()
        {
            foreach (var statuses in Creature.Unreserve("Chair"))
            {
                continue;
            }
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            return base.Run();
        }
    }
}
