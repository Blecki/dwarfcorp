// DigAct.cs
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
    /// A creature attacks a voxel until it is destroyed.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class DigAct : CreatureAct
    {
        public float EnergyLoss { get; set; }
        public KillVoxelTask OwnerTask;
        public bool CheckOwnership = true;
        public DigAct(CreatureAI creature, KillVoxelTask OwnerTask) :
            base(creature)
        {
            Name = "Dig!";
            EnergyLoss = 10.0f;

            this.OwnerTask = OwnerTask;
        }

        public override IEnumerable<Status> Run()
        { 
           Creature.Sprite.ResetAnimations(Creature.AttackMode);

            // Block since we're in a coroutine.
            while (true)
            {
                var vox = OwnerTask.Voxel;

                // Somehow, there wasn't a voxel to mine.
                if (!vox.IsValid)
                {
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    Agent.SetMessage("Failed to dig. Invalid voxel.");
                    yield return Act.Status.Fail;
                    break;
                }

                // If the voxel has already been destroyed, just ignore it and return.
                if (OwnerTask.VoxelHealth <= 0 || (CheckOwnership && !Creature.Faction.Designations.IsVoxelDesignation(vox, DesignationType.Dig)))
                {
                    Creature.CurrentCharacterMode = CharacterMode.Idle;
                    yield return Act.Status.Success;
                    break;
                }

                // Look at the block and slow your velocity down.
                Creature.Physics.Face(vox.WorldPosition + Vector3.One * 0.5f);
                Creature.Physics.Velocity *= 0.01f;

                // Play the attack animations.
                Creature.CurrentCharacterMode = Creature.AttackMode;
                Creature.OverrideCharacterMode = true;
                Creature.Sprite.ResetAnimations(Creature.CurrentCharacterMode);
                Creature.Sprite.PlayAnimations(Creature.CurrentCharacterMode);

                // Wait until an attack was successful...
                foreach (var status in
                    Creature.Attacks[0].PerformOnVoxel(Creature,
                            Creature.Physics.Position,
                            OwnerTask, DwarfTime.LastTime,
                            Creature.Stats.BaseDigSpeed,
                            Creature.Faction.Name))
                {
                    if (status == Act.Status.Running)
                    {
                        Creature.Physics.Face(vox.WorldPosition + Vector3.One * 0.5f);
                        Creature.Physics.Velocity *= 0.01f;

                        // Debug drawing.
                        //if (agent.AI.DrawPath)
                        //    Drawer3D.DrawLine(vox.WorldPosition, agent.AI.Position, Color.Green, 0.25f);
                        yield return Act.Status.Running;
                    }
                }

                Creature.OverrideCharacterMode = false;

                // If the voxel has been destroyed by you, gather it.
                if (OwnerTask.VoxelHealth <= 0.0f)
                {
                    var voxelType = VoxelLibrary.GetVoxelType(vox.Type.Name);
                    if (MathFunctions.RandEvent(0.5f))
                    {
                        Creature.AI.AddXP(Math.Max((int)(voxelType.StartingHealth / 4), 1));
                    }
                    Creature.Stats.NumBlocksDestroyed++;
                    Creature.World.GoalManager.OnGameEvent(new Goals.Triggers.DigBlock(voxelType, Creature));

                    var items = VoxelHelpers.KillVoxel(Creature.World, vox);

                    if (items != null)
                        foreach (Body item in items)
                            Creature.Gather(item);

                    yield return Act.Status.Success;
                }

                // Wait until the animation is done playing before continuing.
                while (!Creature.Sprite.AnimPlayer.IsDone() && Creature.Sprite.AnimPlayer.IsPlaying)
                {
                    Creature.Physics.Face(vox.WorldPosition + Vector3.One * 0.5f);
                    Creature.Physics.Velocity *= 0.01f;
                    yield return Act.Status.Running;
                }

                // Pause the animation and wait for a recharge timer.
                Creature.Sprite.PauseAnimations(Creature.CurrentCharacterMode);


                // Wait for a recharge timer to trigger.
                Creature.Attacks[0].RechargeTimer.Reset();
                while (!Creature.Attacks[0].RechargeTimer.HasTriggered)
                {
                    Creature.Attacks[0].RechargeTimer.Update(DwarfTime.LastTime);
                    Creature.Physics.Face(vox.WorldPosition + Vector3.One * 0.5f);
                    Creature.Physics.Velocity *= 0.01f;
                    yield return Act.Status.Running;
                }

                Creature.CurrentCharacterMode = CharacterMode.Idle;
                yield return Act.Status.Running;
            }
        }
    }
}