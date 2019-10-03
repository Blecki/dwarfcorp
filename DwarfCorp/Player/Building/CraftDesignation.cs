using System.Collections.Generic;

namespace DwarfCorp
{
    public class CraftDesignation
    {
        public ResourceEntity PreviewResource = null;
        public CraftItem ItemType;
        public VoxelHandle Location;
        public GameComponent WorkPile;
        public bool OverrideOrientation;
        public float Orientation;
        public bool Valid;
        public GameComponent Entity;
        public float Progress = 0.0f;
        public bool Finished = false;
        public bool HasResources = false;
        public CreatureAI ResourcesReservedFor = null;
        public Resource SelectedResource = null;
    }
}
