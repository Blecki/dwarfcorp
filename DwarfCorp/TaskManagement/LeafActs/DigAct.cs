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

        public DigAct(CreatureAI creature, KillVoxelTask OwnerTask) :
            base(creature)
        {
            Name = "Dig!";
            EnergyLoss = 10.0f;

            this.OwnerTask = OwnerTask;
        }

        private IEnumerable<Act.Status> PerformOnVoxel(Creature performer, Vector3 pos, KillVoxelTask DigAct, DwarfTime time, float bonus, string faction)
        {
            var tool = ActHelper.GetEquippedItem(performer, "Tool");

            // Play the attack animations.
            Creature.CurrentCharacterMode = tool.Tool_AttackAnimation;
            Creature.OverrideCharacterMode = true;
            Creature.Sprite.ResetAnimations(Creature.CurrentCharacterMode);
            Creature.Sprite.PlayAnimations(Creature.CurrentCharacterMode);

            while (true)
            {
                if (!DigAct.Voxel.IsValid)
                {
                    performer.AI.SetTaskFailureReason("Failed to dig. Voxel was not valid.");
                    yield return Act.Status.Fail;
                    yield break;
                }

                Drawer2D.DrawLoadBar(performer.World.Renderer.Camera, DigAct.Voxel.WorldPosition + Vector3.One * 0.5f, Color.White, Color.Black, 32, 1, (float)DigAct.VoxelHealth / DigAct.Voxel.Type.StartingHealth);

                while (!performer.Sprite.AnimPlayer.HasValidAnimation() || performer.Sprite.AnimPlayer.CurrentFrame < tool.Tool_AttackTriggerFrame)
                {
                    if (performer.Sprite.AnimPlayer.HasValidAnimation())
                        performer.Sprite.AnimPlayer.Play();
                    yield return Act.Status.Running;
                }

                DigAct.VoxelHealth -= (tool.Tool_AttackDamage + bonus);
                DigAct.Voxel.Type.HitSound.Play(DigAct.Voxel.WorldPosition);
                if (!String.IsNullOrEmpty(tool.Tool_AttackHitParticles))
                    performer.Manager.World.ParticleManager.Trigger(tool.Tool_AttackHitParticles, DigAct.Voxel.WorldPosition, Color.White, 5);

                if (!String.IsNullOrEmpty(tool.Tool_AttackHitEffect))
                    IndicatorManager.DrawIndicator(Library.CreateSimpleAnimation(tool.Tool_AttackHitEffect), DigAct.Voxel.WorldPosition + Vector3.One * 0.5f,
                        10.0f, 1.0f, MathFunctions.RandVector2Circle() * 10, tool.Tool_AttackHitColor, MathFunctions.Rand() > 0.5f);

                yield return Act.Status.Success;
                yield break;
            }
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
                    Agent.SetTaskFailureReason("Failed to dig. Invalid voxel.");
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
                    PerformOnVoxel(Creature,
                            Creature.Physics.Position,
                            OwnerTask, DwarfTime.LastTime,
                            Creature.Stats.BaseDigSpeed,
                            Creature.Faction.ParentFaction.Name))
                {
                    if (status == Act.Status.Running)
                    {
                        Creature.Physics.Face(vox.WorldPosition + Vector3.One * 0.5f);
                        Creature.Physics.Velocity *= 0.01f;
                        yield return Act.Status.Running;
                    }
                }

                Creature.OverrideCharacterMode = false;

                // If the voxel has been destroyed by you, gather it.
                if (OwnerTask.VoxelHealth <= 0.0f)
                {
                    if (Library.GetVoxelType(vox.Type.Name).HasValue(out VoxelType voxelType))
                    {
                        Creature.AI.AddXP(Math.Max((int)(voxelType.StartingHealth / 4), 1));
                        Creature.Stats.NumBlocksDestroyed++;
                        ActHelper.ApplyWearToTool(Creature.AI, GameSettings.Current.Wear_Dig);

                        var items = VoxelHelpers.KillVoxel(Creature.World, vox);

                        if (items != null)
                            foreach (GameComponent item in items)
                                Creature.Gather(item, TaskPriority.Eventually);

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

                Creature.CurrentCharacterMode = CharacterMode.Idle;
                yield return Act.Status.Running;
            }
        }
    }
}