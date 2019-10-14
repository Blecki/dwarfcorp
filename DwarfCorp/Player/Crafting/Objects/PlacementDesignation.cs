using System.Collections.Generic;

namespace DwarfCorp
{
    public class PlacementDesignation
    {
        public CraftItem ItemType;
        public VoxelHandle Location;
        public GameComponent WorkPile;
        public bool OverrideOrientation;
        public float Orientation;
        public GameComponent Entity;
        public float Progress = 0.0f;
        public bool Finished = false;
        public bool HasResources = false;
        public Resource SelectedResource = null;
    }
}
