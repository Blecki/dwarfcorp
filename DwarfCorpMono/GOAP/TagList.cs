using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class TagList : IEquatable<TagList>
    {
        public List<string> Tags { get; set; }

        public TagList(params string[] args)
        {
            Tags = new List<string>();
            Tags.AddRange(args);
        }

        public TagList(IEnumerable<string> args)
        {
            Tags = new List<string>();
            Tags.AddRange(args);
        }

        public override int GetHashCode()
        {
            int maxCode = 0;

            foreach (string s in Tags)
            {
                if (Math.Abs(s.GetHashCode()) > Math.Abs(maxCode))
                {
                    maxCode = s.GetHashCode();
                }
            }

            return maxCode;
        }

        public override string ToString()
        {
            string toReturn = "{";

            foreach (string t in Tags)
            {
                toReturn += t;
                toReturn += " ";
            }

            toReturn += "}";

            return toReturn;
        }

        public bool Contains(IEnumerable<string> tags)
        {
            foreach (string s in tags)
            {
                if (Tags.Contains(s))
                {
                    return true;
                }
            }

            return false;
        }


        public  bool Equals(TagList obj)
        {
            return Equals((object)obj);
        }

        // Equal if they share ANY tag in common.
        public override bool Equals(object obj)
        {
            if(obj is TagList)
            {
                TagList other = (TagList)obj;

                foreach (string s in Tags)
                {
                    if (other.Tags.Contains(s))
                    {
                        return true;
                    }
                }

                foreach (string s in other.Tags)
                {
                    if (Tags.Contains(s))
                    {
                        return true;
                    }
                }

                return false;
            }
            else return false;
        }
    }
}
