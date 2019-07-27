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
    public class GenericVoxelListener : GameComponent, IVoxelListener
    {
        private Action<VoxelChangeEvent> Handler;

        [OnSerializing]
        void Serializer(StreamingContext Context)
        {
            throw new InvalidProgramException("DO NOT SERIALIZE GENERIC VOXEL LISTENERS");
        }

        public GenericVoxelListener()
        {

        }

        public GenericVoxelListener(ComponentManager Manager,
            Matrix Transform,
            Vector3 BoundingBoxExtents,
            Vector3 BoundingBoxOffset,
            Action<VoxelChangeEvent> Handler) :
            base(Manager, "New Voxel Listener", Transform, BoundingBoxExtents, BoundingBoxOffset)
        {
            CollisionType = CollisionType.Static;
            SetFlag(Flag.DontUpdate, true);
            this.Handler = Handler;
        }

        public void OnVoxelChanged(VoxelChangeEvent V)
        {
            if (Handler != null)
                Handler(V);
        }
    }
}
