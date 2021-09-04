using Microsoft.Xna.Framework;
using System;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class GenericVoxelListener : GameComponent, IVoxelListener
    {
        private Action<VoxelEvent> Handler;

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
            Action<VoxelEvent> Handler) :
            base(Manager, "New Voxel Listener", Transform, BoundingBoxExtents, BoundingBoxOffset)
        {
            DebugColor = Color.DarkSlateGray;

            CollisionType = CollisionType.Static;
            SetFlag(Flag.DontUpdate, true);
            this.Handler = Handler;
        }

        public void OnVoxelChanged(VoxelEvent V)
        {
            Handler?.Invoke(V);
        }

        public override void OnSpacialStorageUpdate(BoundingBox LastBounds, BoundingBox NewBounds)
        {
            World.RemoveEntityAnchor(this, LastBounds);
            World.AddEntityAnchor(this, NewBounds);
        }

        public override void RemoveFromOctTree()
        {
            base.RemoveFromOctTree();
            World.RemoveEntityAnchor(this, GetBoundingBox());
        }
    }
}
