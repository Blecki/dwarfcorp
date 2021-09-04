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
    public class VoxelRevealer : GameComponent
    {
        private GameComponent Body;
        private int Radius;
        private GlobalVoxelCoordinate OwnerCoordinate;

        [OnSerializing]
        void Serializer(StreamingContext Context)
        {
            throw new InvalidProgramException("DO NOT SERIALIZE VOXEL REVEALER");
        }

        public VoxelRevealer(ComponentManager Manager, GameComponent Body, int Radius) :
            base(Manager)
        {
            this.Body = Body;
            this.Radius = Radius;
            OwnerCoordinate = new GlobalVoxelCoordinate(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            var currentCoordinate = GlobalVoxelCoordinate.FromVector3(Body.Position);
            if (currentCoordinate != OwnerCoordinate)
            {
                VoxelHelpers.RadiusReveal(Manager.World.ChunkManager, new VoxelHandle(Manager.World.ChunkManager, currentCoordinate), Radius);
                OwnerCoordinate = currentCoordinate;
            }
        }
    }
}
