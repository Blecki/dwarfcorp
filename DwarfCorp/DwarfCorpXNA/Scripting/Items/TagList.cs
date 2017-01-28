// TagList.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A tag list is a list of strings. Arbitrary tag lists can be attached
    /// to items to modify how scripts interpret the items.
    /// </summary>
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

        public static implicit operator TagList(string tag)
        {
            return new TagList(tag);
        }

        public static implicit operator TagList(string[] tags)
        {
            return new TagList(tags);
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