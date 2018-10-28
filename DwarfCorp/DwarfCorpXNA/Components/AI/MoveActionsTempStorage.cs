using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class MoveActionTempStorage
    {
        public VoxelHandle[,,] Neighborhood = new VoxelHandle[3, 3, 3];
        public HashSet<Body> NeighborObjects = new HashSet<Body>();
    }
}
