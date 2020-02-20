using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class PlantAct : CompoundCreatureAct
    {
        public Farm Farm;

        public PlantAct()
        {
            Name = "Work " + Farm.Voxel.Coordinate;
        }

        public PlantAct(CreatureAI agent, Farm Farm) :
            base(agent)
        {
            this.Farm = Farm;
            Name = "Work " + Farm.Voxel.Coordinate;
        }

        public IEnumerable<Status> FarmATile()
        {
            if (Farm == null) 
            {
                yield return Status.Fail;
                yield break;
            }
            else if (Farm.Finished)
                yield return Status.Success;
            else
            {
                Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
                Creature.Sprite.PlayAnimations(Creature.Stats.CurrentClass.AttackMode);

                while (Farm.Progress < Farm.TargetProgress && !Farm.Finished)
                {
                    Creature.Physics.Velocity *= 0.1f;
                    Farm.Progress += 3 * Creature.Stats.BaseFarmSpeed*DwarfTime.Dt;

                    Drawer2D.DrawLoadBar(Agent.Manager.World.Renderer.Camera, Agent.Position + Vector3.Up, Color.LightGreen, Color.Black, 64, 4,
                        Farm.Progress / Farm.TargetProgress);

                    if (Farm.Progress >= (Farm.TargetProgress * 0.5f) && Farm.Voxel.Type.Name != "TilledSoil"
                        && Library.GetVoxelType("TilledSoil").HasValue(out VoxelType tilledSoil))
                        Farm.Voxel.Type = tilledSoil;

                    if (Farm.Progress >= Farm.TargetProgress && !Farm.Finished)
                    {
                        if (Library.GetResourceType(Farm.SeedType).HasValue(out var seedType))
                        {
                            var plant = EntityFactory.CreateEntity<Plant>(seedType.PlantToGenerate, Farm.Voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f));

                            plant.Farm = Farm;

                            var original = plant.LocalTransform;
                            original.Translation += Vector3.Down;
                            plant.AnimationQueue.Add(new EaseMotion(0.5f, original, plant.LocalTransform.Translation));

                            Creature.Manager.World.ParticleManager.Trigger("puff", original.Translation, Color.White, 20);
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_plant_grow, Farm.Voxel.WorldPosition, true);
                        }

                        Farm.Finished = true;
                        DestroyResources();
                    }

                    if (MathFunctions.RandEvent(0.01f))
                        Creature.Manager.World.ParticleManager.Trigger("dirt_particle", Creature.AI.Position, Color.White, 1);
                    yield return Status.Running;
                    Creature.Sprite.ReloopAnimations(Creature.Stats.CurrentClass.AttackMode);
                }

                Creature.CurrentCharacterMode = CharacterMode.Idle;
                Creature.AddThought("I farmed something!", new TimeSpan(0, 4, 0, 0), 1.0f);
                Creature.AI.AddXP(1);
                ActHelper.ApplyWearToTool(Creature.AI, GameSettings.Current.Wear_Dig);
                Creature.Sprite.PauseAnimations(Creature.Stats.CurrentClass.AttackMode);
                yield return Status.Success;
            }
        }


        private bool Validate()
        {
            bool tileValid = Farm.Voxel.IsValid && !Farm.Voxel.IsEmpty;

            if (!tileValid)
                return false;

            if (Farm.Finished)
                return false;
            
            return true;
        }

        private IEnumerable<Act.Status> Cleanup()
        {
            OnCanceled();
            yield return Act.Status.Success;
        }

        public override void OnCanceled()
        {
            base.OnCanceled();
        }

        public void DestroyResources()
        {
            var stashed = Agent.Blackboard.GetData<List<Resource>>("stashed-resources");
            foreach (var res in stashed)
                Agent.Creature.Inventory.Remove(res, Inventory.RestockType.None);
        }

        public override void Initialize()
        {
            if (Farm != null)
            {
                if (Farm.Voxel.IsValid)
                {
                    Tree = new Select(new Sequence(
                        ActHelper.CreateEquipmentCheckAct(Agent, "Tool", ActHelper.EquipmentFallback.NoFallback, "Hoe"),
                        new GetResourcesOfType(Agent, new List<ResourceTypeAmount> { new ResourceTypeAmount(Farm.SeedType, 1) }) { BlackboardEntry = "stashed-resources" },
                        new Domain(Validate, new GoToVoxelAct(Farm.Voxel, PlanAct.PlanType.Adjacent, Creature.AI)),
                        new Domain(Validate, new StopAct(Creature.AI)),
                        new Domain(Validate, new Wrap(FarmATile)),
                        new Wrap(Cleanup)), new Wrap(Cleanup));
                }
            }

            base.Initialize();
        }
    }
}
