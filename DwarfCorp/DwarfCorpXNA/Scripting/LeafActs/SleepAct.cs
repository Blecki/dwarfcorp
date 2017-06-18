// SleepAct.cs
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
    /// A creature remains inactive, recharging its energy until it is satisfied.
    /// </summary>
    public class SleepAct : CreatureAct
    {
        public float RechargeRate { get; set; }

        public bool Teleport { get; set; }

        public Vector3 TeleportLocation { get; set; }

        public SleepAct()
        {
            Name = "Sleep";
            RechargeRate = 1.0f;
            Teleport = false;
        }

        public SleepAct(CreatureAI creature) :
            base(creature)
        {
            Name = "Sleep";
            RechargeRate = 1.0f;
            Teleport = false;
        }

        public override void OnCanceled()
        {
            Creature.Status.IsAsleep = false;
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            Creature.OverrideCharacterMode = false;
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            float startingHealth = Creature.Status.Health.CurrentValue;
            while(!Creature.Status.Energy.IsSatisfied() && Creature.Manager.World.Time.IsNight())
            {
                if(Teleport)
                {
                    Creature.AI.Position = TeleportLocation;
                    Creature.Physics.Velocity = Vector3.Zero;
                    Creature.Physics.LocalPosition = TeleportLocation;
                    Creature.Physics.IsSleeping = true;
                }
                Creature.CurrentCharacterMode = CharacterMode.Sleeping;
                Creature.Status.Energy.CurrentValue += DwarfTime.Dt * RechargeRate;
                if (Creature.Status.Health.CurrentValue < startingHealth)
                {
                    Creature.Status.IsAsleep = false;
                    Creature.CurrentCharacterMode = CharacterMode.Idle;
                    Creature.OverrideCharacterMode = false;
                    yield return Status.Fail;
                }

                Creature.Status.Health.CurrentValue = Creature.Status.Health.MaxValue;
                Creature.Status.IsAsleep = true;
                Creature.OverrideCharacterMode = false;
                yield return Status.Running;
            }
            Creature.AI.AddThought(Thought.ThoughtType.Slept);
            Creature.Status.IsAsleep = false;
            yield return Status.Success;
        }
    }
}
