using System.Collections.Generic;

namespace DwarfCorp
{
    public class WorldRendererPersistentSettings
    {
        public int MaxViewingLevel = 1;
        public DesignationType VisibleTypes = DesignationType._All;
        public Dictionary<int, CameraPositiionSnapshot> SavedCameraPositions = new Dictionary<int, CameraPositiionSnapshot>();
    }
}
