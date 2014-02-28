using System.Diagnostics;

namespace DwarfCorp
{

    /// <summary>
    /// This is just a struct of two things: a resource, and a number of that resource.
    /// This is used instead of a list, since there is nothing distinguishing resources from each other.
    /// </summary>
    public class ResourceAmount
    {
        protected bool Equals(ResourceAmount other)
        {
            return Equals(ResourceType, other.ResourceType) && NumResources == other.NumResources;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj))
            {
                return false;
            }
            if(ReferenceEquals(this, obj))
            {
                return true;
            }
            if(obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((ResourceAmount) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ResourceType != null ? ResourceType.GetHashCode() : 0) * 397) ^ NumResources;
            }
        }

        public Resource ResourceType { get; set; }
        public int NumResources { get; set; }


        public ResourceAmount()
        {
            
        }

        public ResourceAmount(Resource resource)
        {
            ResourceType = resource;
            NumResources = 1;
        }

        public ResourceAmount(string resource)
        {
            ResourceType = ResourceLibrary.Resources[resource];
            NumResources = 1;
        }

        public ResourceAmount(LocatableComponent component)
        {
            ResourceType = ResourceLibrary.Resources[component.Tags[0]];
            NumResources = 1;
        }

        public ResourceAmount(Resource resourceType, int numResources)
        {
            ResourceType = resourceType;
            NumResources = numResources;
        }


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
            if(ReferenceEquals(a , null) && !ReferenceEquals(b ,null))
            {
                return true;
            }

            if(!ReferenceEquals(a ,null) && ReferenceEquals(b, null))
            {
                return false;
            }

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