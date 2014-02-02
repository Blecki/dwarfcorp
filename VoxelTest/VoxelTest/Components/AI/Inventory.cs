using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class Inventory : LocatableComponent
    {
        public ResourceContainer Resources { get; set; }

        public override void Die()
        {
            foreach(var resource in Resources)
            {
                EntityFactory.GenerateComponent(resource.ResourceType.ResourceName, GlobalTransform.Translation + MathFunctions.RandVector3Cube() * 0.5f, 
                    Manager, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics, PlayState.ChunkManager, Manager.Factions, PlayState.Camera);
            }
            base.Die();
        }
    }
}
