// BalloonPort.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
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
            Species = animal.GetComponent<Creature>().Species;
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

        public override void Update()
        {
            if (Animals.Count > 0)
            {
                Animals.RemoveAll(body => body.IsDead);
                if (Animals.Count == 0)
                {
                    Species = "";
                }

                foreach (var body in Animals)
                {
                    ClampBody(body);
                }
            }
            base.Update();
        }

        public override void Destroy()
        {
            foreach(var animal in Animals)
            {
                var creature = animal.GetRoot().GetComponent<CreatureAI>();
                if (creature != null)
                {
                    creature.ResetPositionConstraint();
                }
            }
            base.Destroy();
        }
    }
}
