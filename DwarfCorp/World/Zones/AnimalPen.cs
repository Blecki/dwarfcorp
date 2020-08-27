using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class AnimalPen : Zone
    {
        [ZoneFactory("Animal Pen")]
        private static Zone _factory(String ZoneTypeName, WorldManager World)
        {
            return new AnimalPen(ZoneTypeName, World);
        }

        public List<GameComponent> Animals = new List<GameComponent>();

        [JsonProperty] private string Species = "";

        public AnimalPen()
        {

        }
        
        private AnimalPen(String ZoneTypeName, WorldManager World) :
            base(ZoneTypeName, World)
        {
        }

        public override string GetDescriptionString()
        {
            return "Animal Pen " + ID + " - contains " + Species + " (" + Animals.Count + ").";
        }

        public bool CanHold(String Species)
        {
            return String.IsNullOrEmpty(this.Species) || this.Species == Species;
        }

        public void SetSpecies(String Species)
        {
            this.Species = Species;
        }

        public override void OnBuilt()
        {
            foreach(var body in ZoneBodies)
            {
                body.GetRoot().Delete();
            }
            ZoneBodies.Clear();
            foreach (
                var fence in
                    Fence.CreateFences(World.ComponentManager, ContentPaths.Entities.DwarfObjects.fence, Voxels,
                        false))
            {
                AddBody(fence);
                fence.Manager.RootComponent.AddChild(fence);
            }
        }

        public IEnumerable<Act.Status> AddAnimal(GameComponent animal, WorldManager World)
        {
            Animals.Add(animal);
            BoundingBox animalBounds = GetBoundingBox();
            animalBounds = animalBounds.Expand(-0.25f);
            animalBounds.Max.Y += 2;
            animalBounds.Min.Y -= 0.25f;

            if (animal.GetComponent<Physics>().HasValue(out var animalPhysics))
            {
                World.PersistentData.Designations.RemoveEntityDesignation(animalPhysics, DesignationType.Wrangle);
                animalPhysics.ReservedFor = null;
            }

            if (animal.GetComponent<CreatureAI>().HasValue(out var animalAI))
                animalAI.PositionConstraint = animalBounds;

            yield return Act.Status.Success;
        }

        public IEnumerable<Act.Status> RemoveAnimal(GameComponent animal)
        {
            if (!Animals.Contains(animal))
            {
                yield return Act.Status.Fail;
                yield break;
            }

            Animals.Remove(animal);

            if (animal.GetComponent<CreatureAI>().HasValue(out var animalAI))
                animalAI.ResetPositionConstraint();

            if (animal.GetComponent<Creature>().HasValue(out var creature) && creature.Stats.CurrentClass.HasValue(out var c)) 
                Species = c.Name;

            yield return Act.Status.Success;
        }

        private void ClampBody(GameComponent body)
        {
            bool inZone = Voxels.Any(v => VoxelHelpers.GetVoxelAbove(v).GetBoundingBox().Contains(body.Position) == ContainmentType.Contains);
            if (!inZone)
            {
                float minDist = float.MaxValue;
                VoxelHandle minVoxel = VoxelHandle.InvalidHandle;
                foreach(var voxel in Voxels)
                {
                    float dist = (voxel.GetBoundingBox().Center() - body.Position).LengthSquared();
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minVoxel = voxel;
                    }
                }

                if (minDist < float.MaxValue)
                {
                    body.LocalPosition = body.LocalPosition * 0.5f + (minVoxel.WorldPosition + Vector3.Up + Vector3.One) * 0.5f;
                    if (body.GetRoot().GetComponent<CreatureAI>().HasValue(out var creature))
                        creature.CancelCurrentTask();
                }
            }
        }

        public override void Update(DwarfTime Time)
        {
            if (Animals.Count > 0)
            {
                Animals.RemoveAll(body => body.IsDead);
                if (Animals.Count == 0)
                    Species = "";

                foreach (var body in Animals)
                    ClampBody(body);
            }

            base.Update(Time);
        }

        public override void Destroy()
        {
            foreach(var animal in Animals)
                if (animal.GetRoot().GetComponent<CreatureAI>().HasValue(out var creature))
                    creature.ResetPositionConstraint();

            base.Destroy();
        }
    }
}
