using System;
using System.Diagnostics;

namespace DwarfCorp
{

    /// <summary>
    /// This is just a struct of two things: a resource, and a number of that resource.
    /// This is used instead of a list, since there is nothing distinguishing resources from each other.
    /// </summary>
    public class ResourceAmount : ICloneable
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

        public object Clone()
        {
            return new ResourceAmount(ResourceType, NumResources);
        }

        public ResourceAmount CloneResource()
        {
            return Clone() as ResourceAmount;
        }

        public Resource ResourceType { get; set; }
        public int NumResources { get; set; }


        public ResourceAmount()
        {
            
        }

        public ResourceAmount(ResourceLibrary.ResourceType type)
        {
            ResourceType = ResourceLibrary.Resources[type];
            NumResources = 1;
        }

        public ResourceAmount(ResourceAmount other)
        {
            ResourceType = other.ResourceType;
            NumResources = other.NumResources;
        }

        public ResourceAmount(Resource resource)
        {
            ResourceType = resource;
            NumResources = 1;
        }

        public ResourceAmount(string resource)
        {
            ResourceType = ResourceLibrary.GetResourceByName(resource);
            NumResources = 1;
        }

        public ResourceAmount(Body component)
        {
            ResourceType = ResourceLibrary.GetResourceByName(component.Tags[0]);
            NumResources = 1;
        }

        public ResourceAmount(Resource resourceType, int numResources)
        {
            ResourceType = resourceType;
            NumResources = numResources;
        }

        public ResourceAmount(ResourceLibrary.ResourceType type, int num) :
            this(ResourceLibrary.Resources[type], num)
        {
            
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

            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return true;
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