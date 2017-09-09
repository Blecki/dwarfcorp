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
    public class FarmAct : CompoundCreatureAct
    {
        public FarmTool.FarmTile FarmToWork { get; set; }
        public string PlantToCreate { get; set; }
        public List<ResourceAmount> Resources { get; set; }   
        public enum FarmMode 
        {
            Till,
            Plant
        }

        public FarmMode Mode { get; set; }

        public FarmAct()
        {
            Name = "Work a farm";
        }

        public FarmAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Work a farm";
        }

        bool Satisfied()
        {
            if (Mode == FarmMode.Plant)
            {
                return FarmToWork.PlantExists();
            }
            else
            {
                return FarmToWork.IsTilled();
            }
        }

        public IEnumerable<Status> FarmATile()
        {
            FarmTool.FarmTile tile = FarmToWork;
            if (tile == null) 
            {
                yield return Status.Fail;
                yield break;
            }
            if (tile.IsCanceled)
            {
                yield return Status.Fail;
                yield break;
            }
            else if (tile.PlantExists())
            {
                tile.Farmer = null;
                yield return Status.Success;
            }
            else
            {
                if (tile.Plant != null && tile.Plant.IsDead) tile.Plant = null;
                Creature.CurrentCharacterMode = CharacterMode.Attacking;
                Creature.Sprite.ResetAnimations(CharacterMode.Attacking);
                Creature.Sprite.PlayAnimations(CharacterMode.Attacking);
                while (tile.Progress < 100.0f && !Satisfied())
                {
                    if (tile.IsCanceled)
                    {
                        yield return Status.Fail;
                        yield break;
                    }
                    Creature.Physics.Velocity *= 0.1f;
                    tile.Progress += Creature.Stats.BaseFarmSpeed*DwarfTime.Dt;

                    Drawer2D.DrawLoadBar(Agent.Manager.World.Camera, Agent.Position + Vector3.Up, Color.LightGreen, Color.Black, 64, 4,
                        tile.Progress/100.0f);

                    if (tile.Progress >= 100.0f && !Satisfied())
                    {
                        tile.Progress = 0.0f;
                        if (Mode == FarmAct.FarmMode.Plant)
                        {
                            FarmToWork.CreatePlant(PlantToCreate, Creature.Manager.World);
                            DestroyResources();
                        }
                        else
                        {
                            FarmToWork.Vox.Type = VoxelLibrary.GetVoxelType("TilledSoil");
                        }
                    }
                    if (MathFunctions.RandEvent(0.01f))
                        Creature.Manager.World.ParticleManager.Trigger("dirt_particle", Creature.AI.Position, Color.White, 1);
                    yield return Status.Running;
                    Creature.Sprite.ReloopAnimations(CharacterMode.Attacking);
                }
                Creature.CurrentCharacterMode = CharacterMode.Idle;
                Creature.AI.AddThought(Thought.ThoughtType.Farmed);
                Creature.AI.AddXP(1);
                tile.Farmer = null;
                Creature.Sprite.PauseAnimations(CharacterMode.Attacking);
                yield return Status.Success;
            }
        }


        private bool Validate()
        {
            return !FarmToWork.IsCanceled && FarmToWork.Farmer == Agent && FarmToWork.Vox.IsValid && !FarmToWork.Vox.IsEmpty;
        }

        private IEnumerable<Act.Status> Cleanup()
        {
            OnCanceled();
            yield return Act.Status.Success;
        }

        public override void OnCanceled()
        {
            FarmTool.FarmTile tile = Creature.AI.Blackboard.GetData<FarmTool.FarmTile>("ClosestTile");

            if (tile != null && tile.Farmer == Agent)
            {
                tile.Farmer = null;
            }

            base.OnCanceled();
        }

        public void DestroyResources()
        {
            Agent.Creature.Inventory.Remove(Resources);
        }

        public override void Initialize()
        {
            if (FarmToWork != null)
            {
                if (FarmToWork.Vox.IsValid)
                {
                    FarmToWork.Farmer = Agent;
                    Tree = new Select(new Sequence(
                        new Domain(Validate, new GoToVoxelAct(FarmToWork.Vox, PlanAct.PlanType.Adjacent, Creature.AI)),
                        new Domain(Validate, new StopAct(Creature.AI)),
                        new Domain(Validate, new Wrap(FarmATile)),
                        new Wrap(Cleanup)), new Wrap(Cleanup));

                    if (Mode == FarmMode.Plant)
                    {
                        Tree.Children[0].Children.Insert(0, new Sequence(new GetResourcesAct(Agent, Resources)));
                    }
                }
            }

            base.Initialize();
        }
    }
}
