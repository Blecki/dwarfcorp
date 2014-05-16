using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference=true)]
    public class Inventory : Body
    {
        public ResourceContainer Resources { get; set; }


        public Inventory()
        {
            
        }

        public Inventory(ComponentManager manager, string name, Body parent) :
            base(manager, name, parent, Matrix.Identity, parent.BoundingBox.Extents(), parent.BoundingBoxPos)
        {
            
        }

        public bool Pickup(ResourceAmount resourceAmount)
        {
            return Resources.AddResource(resourceAmount);
        }


        public bool Remove(IEnumerable<ResourceAmount> resourceAmount)
        {
            return resourceAmount.Aggregate(true, (current, resource) => current && Resources.RemoveResource(resource));
        }

        public bool Remove(ResourceAmount resourceAmount)
        {
            return Resources.RemoveResource(resourceAmount);
        }

        public bool Pickup(Body component)
        {
            return Pickup(Item.CreateItem(null, component));
        }

        public bool Pickup(Item item)
        {
            if(item == null || item.UserData == null || item.UserData.IsDead)
            {
                return false;
            }

            bool success =  Resources.AddResource
            (new ResourceAmount
            {
                NumResources = 1,
                ResourceType = ResourceLibrary.Resources[item.UserData.Tags[0]]
            });

            if(!success)
            {
                return false;
            }

            if(item.IsInZone)
            {
                item.Zone = null;
            }
            item.UserData.GetRootComponent().Delete();

            return true;
        }

        public List<Body> RemoveAndCreate(ResourceAmount resources)
        {
            List<Body> toReturn = new List<Body>();

            if(!Resources.RemoveResource(resources))
            {
                return toReturn;
            }

            for(int i = 0; i < resources.NumResources; i++)
            {
                Body newEntity = EntityFactory.GenerateComponent(resources.ResourceType.ResourceName, GlobalTransform.Translation + MathFunctions.RandVector3Cube() * 0.5f,
                    Manager, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics, PlayState.ChunkManager, Manager.Factions, PlayState.Camera);
                toReturn.Add(newEntity);
            }

            return toReturn;
        }


 
        public override void Die()
        {
            foreach(var resource in Resources.Where(resource => resource.NumResources > 0))
            {
                for(int i = 0; i < resource.NumResources; i++)
                {
                   PhysicsComponent item =  EntityFactory.GenerateComponent(resource.ResourceType.ResourceName, MathFunctions.RandVector3Box(GetBoundingBox()), Manager, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics, PlayState.ChunkManager, Manager.Factions, PlayState.Camera) as PhysicsComponent;

                    if(item != null)
                    {
                        item.Velocity = MathFunctions.RandVector3Cube() * 5.0f;
                    }
                   
                }
            }
            base.Die();
        }
    }
}
