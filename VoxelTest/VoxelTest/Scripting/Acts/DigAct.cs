using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class DigAct : Act
    {
        public Creature Creature { get; set; }

        public DigAct(Creature creature)
        {
            Creature = creature;
        }

        public override IEnumerable<Status> Run()
        {
            bool targetDead = false;

            while (!targetDead)
            {
                Voxel vox = Creature.AI.TargetVoxel.GetVoxel(Creature.Master.Chunks, false);
                if (vox == null || vox.Health <= 0.0f || !Creature.Master.IsDigDesignation(vox))
                {
                    if (vox != null && vox.Health <= 0.0f)
                    {
                        vox.Kill();
                    }
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;

                    targetDead = true;
                    yield return Status.Success;
                    break;
                }
                else
                {
                    Creature.LocalTarget = vox.Position + new Vector3(0.5f, 0.5f, 0.5f);
                    Vector3 output = Creature.Controller.GetOutput((float)Act.LastTime.ElapsedGameTime.TotalSeconds, Creature.LocalTarget, Creature.Physics.GlobalTransform.Translation);
                    Creature.Physics.ApplyForce(output, (float)Act.LastTime.ElapsedGameTime.TotalSeconds);
                    output.Y = 0.0f;

                    if ((Creature.LocalTarget - Creature.Physics.GlobalTransform.Translation).Y > 0.3)
                    {
                        Creature.AI.Jump(Act.LastTime);
                    }

                    Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.5f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.5f);
                    vox.Health -= Creature.Stats.BaseDigSpeed * (float)Act.LastTime.ElapsedGameTime.TotalSeconds;

                    Creature.CurrentCharacterMode = DwarfCorp.Creature.CharacterMode.Attacking;
                    Creature.Weapon.PlayNoise();

                    yield return Status.Running;
                }
            }
        }
    }
}
