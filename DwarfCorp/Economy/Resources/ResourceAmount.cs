using System;
using System.Diagnostics;

namespace DwarfCorp
{
    /// <summary>
    /// This is just a struct of two things: a resource, and a number of that resource.
    /// This is used instead of a list, since there is nothing distinguishing resources from each other.
    /// </summary>
    public class ResourceAmount : Quantitiy<String>
    {
        public ResourceAmount(ResourceAmount amount)
        {
            Type = amount.Type;
            Count = amount.Count;
        }

        public ResourceAmount(String type)
        {
            Type = type;
            Count = 1;
        }

        public ResourceAmount(Resource resource)
        {
            Type = resource.Name;
            Count = 1;
        }
        
        public ResourceAmount(GameComponent component)
        {
            // Assume that the first tag of the body is
            // the name of the resource.
            Type = component.Tags[0];
            Count = 1;
        }

        public ResourceAmount(Resource resourceType, int numResources)
        {
            Type = resourceType.Name;
            Count = numResources;
        }

        public ResourceAmount(String type, int num) :
            this(ResourceLibrary.Resources[type], num)
        {
            
        }

        public ResourceAmount()
        {
            
        }

        public ResourceAmount CloneResource()
        {
            return new ResourceAmount(Type, Count);
        }
    }

}