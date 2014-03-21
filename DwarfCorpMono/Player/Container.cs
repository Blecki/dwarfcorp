using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Container : Zone
    {
        public int MaxResources { get; set; }
        public List<LocatableComponent> Resources { get; set; }
        public LocatableComponent UserData { get; set; }

        public Container(string id, int maxResources) :
            base(id)
        {
            MaxResources = maxResources;
            Resources = new List<LocatableComponent>();
        }

        public bool IsFull()
        {
            return Resources.Count >= MaxResources;
        }

        public bool HasResource(string id)
        {
            foreach (LocatableComponent r in Resources)
            {
                if (r.Tags[0] == id)
                {
                    return true;
                }
            }

            return false;
        }

        public bool PutResouce(LocatableComponent resource)
        {
            Item item = new Item(resource.Tags[0], this, resource);

            if (IsFull())
            {
                return false;
            }
            else
            {
                Items.Add(item);
                Resources.Add(resource);
                resource.IsVisible = false;
                resource.IsActive = false;
                Matrix m = Matrix.Identity;
                resource.LocalTransform = m;
                UserData.AddChild(resource);
                return true;
                
            }
        }

        public LocatableComponent TakeResource(string id)
        {
            LocatableComponent foundResource = null;
            foreach (LocatableComponent r in Resources)
            {
                if(r.Tags[0] == id)
                {
                    foundResource = r;
                    break;
                }
            }

            if (foundResource != null)
            {
                Resources.Remove(foundResource);
                RemoveFirstItem(id);
                foundResource.IsVisible = true;
                foundResource.IsActive = true;
                UserData.RemoveChild(foundResource);
                Matrix m = Matrix.Identity;
                m.Translation = UserData.GlobalTransform.Translation;
                foundResource.LocalTransform = m;
                UserData.AddChild(foundResource);
            }

            return foundResource;

        }

    }
}
