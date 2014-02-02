using System.Diagnostics;

namespace DwarfCorp
{

    /// <summary>
    /// This is just a struct of two things: a resource, and a number of that resource.
    /// This is used instead of a list, since there is nothing distinguishing resources from each other.
    /// </summary>
    public struct ResourceAmount
    {
        public Resource ResourceType { get; set; }
        public int NumResources { get; set; }




        public static ResourceAmount operator +(int a, ResourceAmount b)
        {
            return new ResourceAmount()
            {
                ResourceType = b.ResourceType,
                NumResources = b.NumResources + a
            };
        }

        public static ResourceAmount operator -(int b, ResourceAmount a)
        {
            return new ResourceAmount()
            {
                ResourceType = a.ResourceType,
                NumResources = a.NumResources - b
            };
        }

        public static ResourceAmount operator +(ResourceAmount a, int b)
        {
            return new ResourceAmount()
            {
                ResourceType = a.ResourceType,
                NumResources = a.NumResources + b
            };
        }

        public static ResourceAmount operator -(ResourceAmount a, int b)
        {
            return new ResourceAmount()
            {
                ResourceType = a.ResourceType,
                NumResources = a.NumResources - b
            };
        }

        public static ResourceAmount operator +(ResourceAmount a, ResourceAmount b)
        {
            if(a.ResourceType != b.ResourceType)
            {
                return default(ResourceAmount);
            }

            return new ResourceAmount()
            {
                ResourceType = a.ResourceType,
                NumResources = a.NumResources + b.NumResources
            };
        }

        public static ResourceAmount operator -(ResourceAmount a, ResourceAmount b)
        {
            if (a.ResourceType != b.ResourceType)
            {
                return default(ResourceAmount);
            }

            return new ResourceAmount()
            {
                ResourceType = a.ResourceType,
                NumResources = a.NumResources - b.NumResources
            };
        }

        public static bool operator ==(ResourceAmount a, ResourceAmount b)
        {
            return a.ResourceType == b.ResourceType && a.NumResources == b.NumResources;
        }

        public static bool operator !=(ResourceAmount a, ResourceAmount b)
        {
            return !(a == b);
        }

        public static bool operator <(ResourceAmount a, ResourceAmount b)
        {
            return (a.ResourceType == b.ResourceType) && (a.NumResources < b.NumResources);
        }

        public static bool operator >(ResourceAmount a, ResourceAmount b)
        {
            return (a.ResourceType == b.ResourceType) && (a.NumResources > b.NumResources);
        }
    }

}