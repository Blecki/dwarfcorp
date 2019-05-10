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
        public Farm FarmToWork { get; set; }
        public List<ResourceAmount> Resources { get; set; }   

        public PlantAct()
        {
            Name = "Work a farm";
        }

        public PlantAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Work a farm";
        }

        public IEnumerable<Status> FarmATile()
        {
            if (FarmToWork == null) 
            {
                yield return Status.Fail;
                yield break;
            }
            else if (FarmToWork.Finished)
            {
                yield return Status.Success;
            }
            else
            {
                Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
                Creature.Sprite.PlayAnimations(Creature.Stats.CurrentClass.AttackMode);
                while (FarmToWork.Progress < FarmToWork.TargetProgress && !FarmToWork.Finished)
                {
                    Creature.Physics.Velocity *= 0.1f;
                    FarmToWork.Progress += 3 * Creature.Stats.BaseFarmSpeed*DwarfTime.Dt;

                    Drawer2D.DrawLoadBar(Agent.Manager.World.Camera, Agent.Position + Vector3.Up, Color.LightGreen, Color.Black, 64, 4,
                        FarmToWork.Progress / FarmToWork.TargetProgress);

                    if (FarmToWork.Progress >= (FarmToWork.TargetProgress * 0.5f) && FarmToWork.Voxel.Type.Name != "TilledSoil")
                    {
                        FarmToWork.Voxel.Type = Library.GetVoxelType("TilledSoil");
                    }

                    if (FarmToWork.Progress >= FarmToWork.TargetProgress && !FarmToWork.Finished)
                    {
                        var plant = EntityFactory.CreateEntity<Plant>(
                            ResourceLibrary.GetResourceByName(FarmToWork.SeedString).PlantToGenerate, 
                            FarmToWork.Voxel.WorldPosition + new Vector3(0.5f, 1.0f, 0.5f));

                        plant.Farm = FarmToWork;

                        Matrix original = plant.LocalTransform;
                        original.Translation += Vector3.Down;
                        plant.AnimationQueue.Add(new EaseMotion(0.5f, original, plant.LocalTransform.Translation));

                        Creature.Manager.World.ParticleManager.Trigger("puff", original.Translation, Color.White, 20);

                        SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_plant_grow, FarmToWork.Voxel.WorldPosition, true);

                        FarmToWork.Finished = true;

                        DestroyResources();
                    }

                    if (MathFunctions.RandEvent(0.01f))
                        Creature.Manager.World.ParticleManager.Trigger("dirt_particle", Creature.AI.Position, Color.White, 1);
                    yield return Status.Running;
                    Creature.Sprite.ReloopAnimations(Creature.Stats.CurrentClass.AttackMode);
                }
                Creature.CurrentCharacterMode = CharacterMode.Idle;
                Creature.AddThought(Thought.ThoughtType.Farmed);
                Creature.AI.AddXP(1);
                Creature.Sprite.PauseAnimations(Creature.Stats.CurrentClass.AttackMode);
                yield return Status.Success;
            }
        }


        private bool Validate()
        {
            bool tileValid = FarmToWork.Voxel.IsValid && !FarmToWork.Voxel.IsEmpty;

            if (!tileValid)
            {
                return false;
            }

            if (FarmToWork.Finished)
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
            
            base.OnCanceled();
        }

        public void DestroyResources()
        {
            Agent.Creature.Inventory.Remove(Resources, Inventory.RestockType.None);
        }

        public override void Initialize()
        {
            if (FarmToWork != null)
            {
                if (FarmToWork.Voxel.IsValid)
                {
                    Tree = new Select(new Sequence(
                        new GetResourcesAct(Agent, Resources),
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
