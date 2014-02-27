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
    public class Inventory : LocatableComponent
    {
        public ResourceContainer Resources { get; set; }


        public Inventory()
        {
            
        }

        public Inventory(ComponentManager manager, string name, LocatableComponent parent) :
            base(manager, name, parent, Matrix.Identity, new Vector3(1, 1, 1), Vector3.Zero)
        {
            
        }

        public bool Pickup(ResourceAmount resourceAmount)
        {
            return Resources.AddResource(resourceAmount);
        }

        public bool Remove(ResourceAmount resourceAmount)
        {
            return Resources.RemoveResource(resourceAmount);
        }

        public bool Pickup(LocatableComponent component)
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
                item.Zone.RemoveItem(item.UserData);
                item.Zone = null;
            }
            item.UserData.GetRootComponent().Delete();

            return true;
        }

        public List<LocatableComponent> RemoveAndCreate(ResourceAmount resources)
        {
            List<LocatableComponent> toReturn = new List<LocatableComponent>();

            if(!Resources.RemoveResource(resources))
            {
                return toReturn;
            }

            for(int i = 0; i < resources.NumResources; i++)
            {
                LocatableComponent newEntity = EntityFactory.GenerateComponent(resources.ResourceType.ResourceName, GlobalTransform.Translation + MathFunctions.RandVector3Cube() * 0.5f,
                    Manager, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics, PlayState.ChunkManager, Manager.Factions, PlayState.Camera);
                toReturn.Add(newEntity);
            }

            return toReturn;
        }


 
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
