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

            foreach(string s in Tags)
            {
                if(Math.Abs(s.GetHashCode()) > Math.Abs(maxCode))
                {
                    maxCode = s.GetHashCode();
                }
            }

            return maxCode;
        }

        public override string ToString()
        {
            string toReturn = "{";

            foreach(string t in Tags)
            {
                toReturn += t;
                toReturn += " ";
            }

            toReturn += "}";

            return toReturn;
        }

        public bool Contains(IEnumerable<string> tags)
        {
            return tags.Any(s => Tags.Contains(s));
        }


        public bool Equals(TagList obj)
        {
            return Equals((object) obj);
        }

        // Equal if they share ANY tag in common.
        public override bool Equals(object obj)
        {
            var list = obj as TagList;
            if(list != null)
            {
                TagList other = list;

                if(Tags.Any(s => other.Tags.Contains(s)))
                {
                    return true;
                }

                return other.Tags.Any(s => Tags.Contains(s));
            }
            else
            {
                return false;
            }
        }
    }

}