// FarmAct.cs
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
    public class TillAct : CompoundCreatureAct
    {
        public FarmTile FarmToWork { get; set; }

        public TillAct()
        {
            Name = "Till Soil";
        }

        public TillAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Work a farm";
        }

        bool Satisfied()
        {
                return FarmToWork.IsTilled();
        }

        public IEnumerable<Status> FarmATile()
        {
            if (FarmToWork == null) 
            {
                yield return Status.Fail;
                yield break;
            }

            if (FarmToWork.Farmer == null)
            {
                yield return Status.Fail;
                yield break;
            }

            else if (FarmToWork.PlantExists())
            {
                FarmToWork.Farmer = null;
                yield return Status.Success;
            }
            else
            {
                if (FarmToWork.Plant != null && FarmToWork.Plant.IsDead) FarmToWork.Plant = null;
                Creature.CurrentCharacterMode = CharacterMode.Attacking;
                Creature.Sprite.ResetAnimations(CharacterMode.Attacking);
                Creature.Sprite.PlayAnimations(CharacterMode.Attacking);
                while (FarmToWork.Progress < 100.0f && !Satisfied())
                {
                    if (FarmToWork.Farmer == null)
                    {
                        yield return Status.Fail;
                        yield break;
                    }
                    Creature.Physics.Velocity *= 0.1f;
                    FarmToWork.Progress += 3 * Creature.Stats.BaseFarmSpeed*DwarfTime.Dt;

                    Drawer2D.DrawLoadBar(Agent.Manager.World.Camera, Agent.Position + Vector3.Up, Color.LightGreen, Color.Black, 64, 4,
                        FarmToWork.Progress/100.0f);

                    if (FarmToWork.Progress >= 100.0f && !Satisfied())
                    {
                        FarmToWork.Progress = 0.0f;

                        FarmToWork.Voxel.Type = VoxelLibrary.GetVoxelType("TilledSoil");
                        Creature.Faction.Designations.RemoveVoxelDesignation(FarmToWork.Voxel, DesignationType._AllFarms);
                        Creature.Faction.Designations.AddVoxelDesignation(FarmToWork.Voxel, DesignationType._InactiveFarm, FarmToWork, null);
                    }

                    if (MathFunctions.RandEvent(0.01f))
                        Creature.Manager.World.ParticleManager.Trigger("dirt_particle", Creature.AI.Position, Color.White, 1);
                    yield return Status.Running;
                    Creature.Sprite.ReloopAnimations(CharacterMode.Attacking);
                }

                Creature.CurrentCharacterMode = CharacterMode.Idle;
                Creature.AI.AddThought(Thought.ThoughtType.Farmed);
                Creature.AI.AddXP(1);
                FarmToWork.Farmer = null;
                Creature.Sprite.PauseAnimations(CharacterMode.Attacking);
                yield return Status.Success;
            }
        }

        private bool Validate()
        {
            bool tileValid = FarmToWork.Farmer == Agent && FarmToWork.Voxel.IsValid && !FarmToWork.Voxel.IsEmpty;

            if (!tileValid)
            {
                return false;
            }
            
            if (FarmToWork.Voxel.Type.Name == "TilledSoil")
            {
                return false;
            }

            return true;
        }

        private IEnumerable<Act.Status> Cleanup()
        {
            OnCanceled();
            yield return Act.Status.Success;
        }

        public override void OnCanceled()
        {
            var tile = FarmToWork;

            if (tile != null && tile.Farmer == Agent)
            {
                tile.Farmer = null;
            }

            base.OnCanceled();
        }

        public override void Initialize()
        {
            if (FarmToWork != null)
            {
                if (FarmToWork.Voxel.IsValid)
                {
                    FarmToWork.Farmer = Agent;
                    Tree = new Select(new Sequence(
                        new Domain(Validate, new GoToVoxelAct(FarmToWork.Voxel, PlanAct.PlanType.Adjacent, Creature.AI)),
                        new Domain(Validate, new StopAct(Creature.AI)),
                        new Domain(Validate, new Wrap(FarmATile)),
                        new Wrap(Cleanup)), new Wrap(Cleanup));
                }
            }

            base.Initialize();
        }
    }
}
