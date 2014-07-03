namespace DwarfCorp
{

    /// <summary>
    /// A designation is just a voxel which can be assigned a number of creatures.
    /// </summary>
    public class BuildOrder
    {
        public VoxelRef Vox = null;
        public int NumCreaturesAssigned = 0;
    }

}