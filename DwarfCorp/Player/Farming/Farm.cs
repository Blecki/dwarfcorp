namespace DwarfCorp
{
    public class Farm
    {
        public VoxelHandle Voxel = VoxelHandle.InvalidHandle;
        public float Progress = 0.0f;
        public float TargetProgress = 100.0f;
        public string SeedType = null; 
        public bool Finished = false;
    }
}
