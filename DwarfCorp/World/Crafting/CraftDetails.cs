using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public class CraftDetails : GameComponent
    {
        public Resource Resource;

        public CraftDetails()
        {
        }

        public CraftDetails(ComponentManager manager, Resource Resource) :
            base(manager)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            this.Resource = Resource;
        }

        public override void Die()
        {
            var bounds = this.GetRoot().GetBoundingBox();
            var pos = MathFunctions.RandVector3Box(bounds);
            new ResourceEntity(Manager, Resource, pos);

            // Todo: Use craft type to create a thing? Or store underlying resource used to place object?
            //try
            //{
            //    if (Parent != null)
            //    {
            //        var body = Parent.GetRoot();

            //        if (body != null)
            //        {
            //            if (Library.GetCraftable(this.CraftType).HasValue(out var craftable))
            //            {
            //                var bounds = body.GetBoundingBox();
            //                var resource = craftable.ToResource(World, Resources);
            //                var pos = MathFunctions.RandVector3Box(bounds);
            //                EntityFactory.CreateEntity<GameComponent>(resource.Name + " Resource", pos);
            //            }
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Error while destroying crafted item - " + e.Message);
            //    Program.WriteExceptionLog(e);
            //}

            base.Die();
        }
    }
}
