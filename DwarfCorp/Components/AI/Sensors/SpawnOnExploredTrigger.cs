using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SpawnOnExploredTrigger : GameComponent, IVoxelListener
    {
        public VoxelHandle Voxel;
        public string EntityToSpawn { get; set; }
        public Vector3 SpawnLocation { get; set; }
        public Blackboard BlackboardData { get; set; }

        public SpawnOnExploredTrigger()
        {
            CollisionType = CollisionType.Static;
        }

        public SpawnOnExploredTrigger(ComponentManager Manager, VoxelHandle Voxel) :
            base(Manager, "ExplorationSpawner", Matrix.CreateTranslation(Voxel.GetBoundingBox().Center()), new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
        {
            CollisionType = CollisionType.Static;
            this.Voxel = Voxel;
            this.CollisionType = CollisionType.Static;
        }

        public void OnVoxelChanged(VoxelChangeEvent V)
        {
            if (V.Type == VoxelChangeEventType.Explored)
            {
                Delete();
                EntityFactory.CreateEntity<GameComponent>(EntityToSpawn, SpawnLocation, BlackboardData);
            }
        }
    }
}
