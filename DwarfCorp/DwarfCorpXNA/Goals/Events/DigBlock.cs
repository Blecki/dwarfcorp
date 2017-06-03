using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Goals.Events
{
    public class DigBlock : GameEvent
    {
        public VoxelType VoxelType;
        public Creature Miner;

        public DigBlock(VoxelType VoxelType, Creature Miner)
        {
            this.VoxelType = VoxelType;
            this.Miner = Miner;
        }
    }
}
