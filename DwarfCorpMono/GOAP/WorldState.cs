using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class WorldState : IEquatable<WorldState>
    {
        public Dictionary<string, object> Specification { get; set; }

        public WorldState Diff(WorldState other)
        {
            Dictionary<string, object> Spec = new Dictionary<string, object>();

            foreach (string s in other.Specification.Keys)
            {
                if (!Specification.ContainsKey(s) || !Specification[s].Equals(other.Specification[s]))
                {
                    Spec[s] = other[s];
                }
            }

            foreach (string s in Specification.Keys)
            {
                if (!other.Specification.ContainsKey(s))
                {
                    Spec[s] = Specification[s];
                }
            }
            return new WorldState(Spec);
        }

        public bool Conflicts(WorldState requirements)
        {
            foreach (string s in requirements.Specification.Keys)
            {
                if (!Specification.ContainsKey(s))
                {
                    continue;
                }
                else if (Specification[s] == null)
                {
                    if (requirements[s] != null)
                    {
                        return true;
                    }
                }
                else if (!Specification[s].Equals(requirements[s]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool MeetsRequirements(WorldState requirements)
        {
            foreach (string s in requirements.Specification.Keys)
            {
                if (!Specification.ContainsKey(s))
                {
                    return false;
                }
                else if (Specification[s] == null)
                {
                    if (requirements[s] != null)
                    {
                        return false;
                    }
                }
                else if (!Specification[s].Equals(requirements[s]))
                {
                    return false;
                }
            }

            return true;
        }



        public int Distance(WorldState other)
        {
            int toReturn = 0;
            foreach (string s in other.Specification.Keys)
            {
                if (!Specification.ContainsKey(s))
                {
                    toReturn++;
                }
                else if (Specification[s] == null)
                {
                    if (Specification[s] != other.Specification[s])
                    {
                        toReturn++;
                    }
                }
                else if (!Specification[s].Equals(other[s]))
                {
                    toReturn++;
                }
            }

            foreach (string s in Specification.Keys)
            {
                if (!other.Specification.ContainsKey(s))
                {
                    toReturn++;
                }
            }

            return toReturn;
        }

        public WorldState()
        {
            Specification = new Dictionary<string, object>();
        }

        public WorldState(Dictionary<string, object> spec)
        {
            Specification = spec;
        }

        public WorldState(WorldState other)
        {
            Specification = new Dictionary<string, object>();

            string[] keys = new string[other.Specification.Keys.Count];
            other.Specification.Keys.CopyTo(keys, 0);

            foreach (string s in keys)
            {
                Specification[s] = other.Specification[s];
            }
        }

        public object this[string index]
        {

            get
            {
                if (Specification.ContainsKey(index))
                {
                    return Specification[index];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                Specification[index] = value;
            }
        }

        public bool Equals(WorldState other)
        {
            return Distance(other) == 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is WorldState)
            {
                return Equals((WorldState)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            int toReturn = 0;
            int v = 0;
            foreach (KeyValuePair<string, object> pair in Specification)
            {
                toReturn = v * 7 + toReturn ^ pair.GetHashCode();
                v++;
            }

            return toReturn;
        }

    }

}
