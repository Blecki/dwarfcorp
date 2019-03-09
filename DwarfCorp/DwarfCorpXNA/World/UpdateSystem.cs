using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class UpdateSystem
    {
        public virtual void Update(DwarfTime GameTime) { }
        public virtual void ComponentCreated(GameComponent C) { }
        public virtual void ComponentDestroyed(GameComponent C) { }
    }
}
