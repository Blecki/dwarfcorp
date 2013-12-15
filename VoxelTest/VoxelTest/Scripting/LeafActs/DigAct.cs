﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class DigAct : CreatureAct
    {
        public string TargetVoxelName { get; set; }
        
        public DigAct(CreatureAIComponent creature, string targetVoxel) :
            base(creature)
        {
            TargetVoxelName = targetVoxel;
            Name = "Dig!";
        }

        public VoxelRef GetTargetVoxel()
        {
            return Agent.Blackboard.GetData<VoxelRef>(TargetVoxelName);
        }

        public override IEnumerable<Status> Run()
        {
            Creature.Sprite.ResetAnimations(Creature.CharacterMode.Attacking);
            while(true)
            {
                VoxelRef blackBoardVoxelRef = GetTargetVoxel();

                if(blackBoardVoxelRef == null)
                {
                    yield return Status.Fail;
                    break;
                }

                Voxel vox = blackBoardVoxelRef.GetVoxel(false);
                if(vox == null || vox.Health <= 0.0f || !Creature.Faction.IsDigDesignation(vox))
                {
                    if(vox != null && vox.Health <= 0.0f)
                    {
                        vox.Kill();
                    }
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;

                    yield return Status.Success;
                    break;
                }
                Creature.LocalTarget = vox.Position + new Vector3(0.5f, 0.5f, 0.5f);
                Vector3 output = Creature.Controller.GetOutput((float) LastTime.ElapsedGameTime.TotalSeconds, Creature.LocalTarget, Creature.Physics.GlobalTransform.Translation);
                Creature.Physics.ApplyForce(output, (float) LastTime.ElapsedGameTime.TotalSeconds);
                output.Y = 0.0f;

                if((Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation).Y > 0.3)
                {
                    Agent.Jump(LastTime);
                }

                Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.5f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.5f);
                vox.Health -= Creature.Stats.BaseDigSpeed * (float) LastTime.ElapsedGameTime.TotalSeconds;

                Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                Creature.Weapon.PlayNoise();

                yield return Status.Running;
            }
        }
    }

}