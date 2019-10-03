using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Farm
    {
        public VoxelHandle Voxel = VoxelHandle.InvalidHandle;
        public float Progress = 0.0f;
        public float TargetProgress = 100.0f;
        public string SeedString = null; // Todo: Stupid name.
        public List<ResourceTypeAmount> RequiredResources = null;
        public bool Finished = false;
    }
}
