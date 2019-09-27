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
        // Corresponds to a type in the CraftLibrary
        public string CraftType = "";
        // The Resources used to craft the item.
        public List<ResourceAmount> Resources = new List<ResourceAmount>();

        public CraftDetails()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftDetails(ComponentManager manager) :
            base(manager)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftDetails(ComponentManager manager, string craftType, List<ResourceAmount> resources = null) :
            this(manager)
        {
            CraftType = craftType;

            if (resources != null)
                Resources = resources;
            else
            {
                Resources = new List<ResourceAmount>();
                if (Library.GetCraftable(craftType).HasValue(out var libraryType))
                    Resources.AddRange(libraryType.RequiredResources.Select(requirement => new ResourceAmount(Library.EnumerateResourceTypesWithTag(requirement.Tag).OrderBy(r => r.MoneyValue.Value).FirstOrDefault().Name, requirement.Count)));
            }
        }

        public override void Die()
        {
            try
            {
                if (Parent != null)
                {
                    var body = Parent.GetRoot();

                    if (body != null)
                    {
                        if (Library.GetCraftable(this.CraftType).HasValue(out var craftable))
                        {
                            var bounds = body.GetBoundingBox();
                            var resource = craftable.ToResource(World, Resources);
                            var pos = MathFunctions.RandVector3Box(bounds);
                            EntityFactory.CreateEntity<GameComponent>(resource.Name + " Resource", pos);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while destroying crafted item - " + e.Message);
                Program.WriteExceptionLog(e);
            }

            base.Die();
        }
    }
}
