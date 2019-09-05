namespace DwarfCorp
{
    /// <summary>
    /// The behavior of the voxel selector depends on its type.
    /// </summary>
    public enum VoxelSelectionType
    {
        /// <summary>
        /// Selects only filled voxels
        /// </summary>
        SelectFilled,
        /// <summary>
        /// Selects only empty voxels
        /// </summary>
        SelectEmpty,
        SelectPrism,
    }
}
