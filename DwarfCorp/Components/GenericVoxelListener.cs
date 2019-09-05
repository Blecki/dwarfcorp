using Microsoft.Xna.Framework;
using System;
using System.Runtime.Serialization;

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
            DebugColor = Color.DarkSlateGray;
        }

        public GenericVoxelListener(ComponentManager Manager,
            Matrix Transform,
            Vector3 BoundingBoxExtents,
            Vector3 BoundingBoxOffset,
            Action<VoxelChangeEvent> Handler) :
            base(Manager, "New Voxel Listener", Transform, BoundingBoxExtents, BoundingBoxOffset)
        {
            DebugColor = Color.DarkSlateGray;

            CollisionType = CollisionType.Static;
            SetFlag(Flag.DontUpdate, true);
            this.Handler = Handler;
        }

        public void OnVoxelChanged(VoxelChangeEvent V)
        {
            Handler?.Invoke(V);
        }
    }
}
