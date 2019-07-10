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
    public class DigAct : CreatureAct
    {
        public float EnergyLoss { get; set; }
        public KillVoxelTask OwnerTask;
        public bool CheckOwnership = true;
        public Attack Attack;

        public DigAct(CreatureAI creature, KillVoxelTask OwnerTask) :
            base(creature)
        {
            Name = "Dig!";
            EnergyLoss = 10.0f;

            this.OwnerTask = OwnerTask;
            Attack = creature.Creature.Attacks[0];
        }

        public override IEnumerable<Status> Run()
        { 
           Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);

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
                if (OwnerTask.VoxelHealth <= 0 || (CheckOwnership && !Creature.World.PersistentData.Designations.IsVoxelDesignation(vox, DesignationType.Dig)))
                {
                    Creature.CurrentCharacterMode = CharacterMode.Idle;
                    yield return Act.Status.Success;
                    break;
                }

                // Look at the block and slow your velocity down.
                Creature.Physics.Face(vox.WorldPosition + Vector3.One * 0.5f);
                Creature.Physics.Velocity *= 0.01f;

                // Play the attack animations.
                Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                Creature.OverrideCharacterMode = true;
                Creature.Sprite.ResetAnimations(Creature.CurrentCharacterMode);
                Creature.Sprite.PlayAnimations(Creature.CurrentCharacterMode);

                // Wait until an attack was successful...
                foreach (var status in
                    Attack.PerformOnVoxel(Creature,
                            Creature.Physics.Position,
                            OwnerTask, DwarfTime.LastTime,
                            Creature.Stats.BaseDigSpeed,
                            Creature.Faction.ParentFaction.Name))
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
                    if (Library.GetVoxelType(vox.Type.Name).HasValue(out VoxelType voxelType))
                    {
                        if (MathFunctions.RandEvent(0.5f))
                            Creature.AI.AddXP(Math.Max((int)(voxelType.StartingHealth / 4), 1));
                        Creature.Stats.NumBlocksDestroyed++;

                        var items = VoxelHelpers.KillVoxel(Creature.World, vox);

                        if (items != null)
                            foreach (GameComponent item in items)
                                Creature.Gather(item);

                        yield return Act.Status.Success;
                    }
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
                Attack.RechargeTimer.Reset();
                while (!Attack.RechargeTimer.HasTriggered)
                {
                    Attack.RechargeTimer.Update(DwarfTime.LastTime);
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