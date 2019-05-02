using System;
using System.Diagnostics;

namespace DwarfCorp
{
    // Todo: I hate this class.
    public class Quantitiy<T> : ICloneable
    {
        protected bool Equals(Quantitiy<T> other)
        {
            return Equals(Type, other.Type) && Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((Quantitiy<T>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Type.GetHashCode() * 397) ^ Count;
            }
        }

        public virtual object Clone()
        {
            return new Quantitiy<T>(Type, Count);
        }

        public virtual Quantitiy<T> CloneQuantity()
        {
            return Clone() as Quantitiy<T>;
        }

        public T Type { get; set; }
        public int Count { get; set; }


        public Quantitiy()
        {

        }

        public Quantitiy(T type)
        {
            Type = type;
            Count = 1;
        }

        public Quantitiy(Quantitiy<T> other)
        {
            Type = other.Type;
            Count = other.Count;
        }

        public Quantitiy(T resourceType, int numResources)
        {
            Type = resourceType;
            Count = numResources;
        }


        public static Quantitiy<T> operator +(int a, Quantitiy<T> b)
        {
            return new Quantitiy<T>()
            {
                Type = b.Type,
                Count = b.Count + a
            };
        }

        public static Quantitiy<T> operator -(int b, Quantitiy<T> a)
        {
            return new Quantitiy<T>()
            {
                Type = a.Type,
                Count = a.Count - b
            };
        }

        public static Quantitiy<T> operator +(Quantitiy<T> a, int b)
        {
            return new Quantitiy<T>()
            {
                Type = a.Type,
                Count = a.Count + b
            };
        }

        public static Quantitiy<T> operator -(Quantitiy<T> a, int b)
        {
            return new Quantitiy<T>()
            {
                Type = a.Type,
                Count = a.Count - b
            };
        }

        public static bool operator ==(Quantitiy<T> a, Quantitiy<T> b)
        {
            if (ReferenceEquals(a, null) && !ReferenceEquals(b, null))
            {
                return true;
            }

            if (!ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return false;
            }

            if (ReferenceEquals(a, null))
            {
                return true;
            }

            return a.Type.Equals(b.Type) && (a.Count == b.Count);
        }

        public static bool operator !=(Quantitiy<T> a, Quantitiy<T> b)
        {
            return !(a == b);
        }

        public static bool operator <(Quantitiy<T> a, Quantitiy<T> b)
        {
            return (a.Type.Equals(b.Type)) && (a.Count < b.Count);
        }

        public static bool operator >(Quantitiy<T> a, Quantitiy<T> b)
        {
            return (a.Type.Equals(b.Type)) && (a.Count > b.Count);
        }
    }
}