using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting.NewMagic
{
    public class Spell
    {
        public string Name;
        public string Description;
        
        public enum TargetType
        {
            Objects,
            Blocks,
            Self
        }

        public TargetType Target;

        public virtual bool ApplySelf(Creature self)
        {
            return true;
        }

        public virtual bool ApplyObjects(Creature self, IEnumerable<Body> objects)
        {
            return true;
        }

        public virtual bool ApplyBlocks(Creature self, IEnumerable<VoxelHandle> blocks)
        {
            return true;
        }
    }
}
