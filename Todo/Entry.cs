using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.IO;

namespace TodoList
{
    public struct ParentChildPair
    {
        public Entry Parent;
        public Entry Child;
    }

    public class Entry
    {
        public UInt32 ID = 0;
        public String Status = "-";
        public UInt32 Priority = 0;
        public String Description = "ROOT";
        public List<String> Tags = new List<String>();
        public List<Entry> Children = new List<Entry>();
        public DateTime CreationTime = DateTime.Now;
        public DateTime CompletionTime = DateTime.Now;
        public String Notes = "";

        public IEnumerable<ParentChildPair> EnumerateParentChildPairs(Entry Parent = null)
        {
            yield return new ParentChildPair
            {
                Parent = Parent,
                Child = this
            };

            foreach (var child in Children)
                foreach (var entry in child.EnumerateParentChildPairs(this))
                    yield return entry;
        }

        public IEnumerable<Entry> EnumerateTree()
        {
            yield return this;

            foreach (var child in Children)
                foreach (var entry in child.EnumerateTree())
                    yield return entry;
        }

        public Entry FindChildWithID(UInt32 ID)
        {
            return this.EnumerateTree().FirstOrDefault(e => e.ID == ID);
        }

        public List<Entry> FindParentChain(UInt32 ID)
        {
            if (this.ID == ID)
                return new List<Entry> { this };
            foreach (var child in Children)
            {
                var clist = child.FindParentChain(ID);
                if (clist != null)
                {
                    clist.Insert(0, this);
                    return clist;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return String.Format(" {0:X4} {1,1} {2:X2} {3}", ID, Status, Priority, Description);
        }
    }
}