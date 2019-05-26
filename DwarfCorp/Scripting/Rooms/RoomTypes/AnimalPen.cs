using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class AnimalPen : Room
    {
        [RoomFactory("Animal Pen")]
        private static Room _factory(RoomData Data, Faction Faction, WorldManager World)
        {
            return new AnimalPen(Data, Faction, World);
        }

        public List<GameComponent> Animals = new List<GameComponent>();

        public string Species = "";

        public AnimalPen()
        {

        }
        
        private AnimalPen(RoomData Data, Faction Faction, WorldManager World) :
            base(Data, World, Faction)
        {
        }

        public override string GetDescriptionString()
        {
            return "Animal Pen " + ID + " - contains " + Species + " (" + Animals.Count + ").";
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

        public IEnumerable<Act.Status> AddAnimal(GameComponent animal, Faction faction)
        {
            Animals.Add(animal);
            BoundingBox animalBounds = GetBoundingBox();
            animalBounds = animalBounds.Expand(-0.25f);
            animalBounds.Max.Y += 2;
            animalBounds.Min.Y -= 0.25f;
            animal.GetComponent<Physics>().ReservedFor = null; ;
            animal.GetComponent<CreatureAI>().PositionConstraint = animalBounds;
            faction.Designations.RemoveEntityDesignation(animal.GetComponent<Physics>(), DesignationType.Wrangle);
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
            animal.GetComponent<CreatureAI>().ResetPositionConstraint();
            Species = animal.GetComponent<Creature>().Stats.CurrentClass.Name;
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
                    var creature = body.GetRoot().GetComponent<CreatureAI>();
                    if (creature != null)
                    {
                        creature.CancelCurrentTask();
                    }
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
            {
                var creature = animal.GetRoot().GetComponent<CreatureAI>();
                if (creature != null)
                    creature.ResetPositionConstraint();
            }
            base.Destroy();
        }
    }
}
