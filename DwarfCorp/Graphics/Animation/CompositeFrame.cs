using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mime;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CompositeFrame
    {
        public override int GetHashCode()
        {
            unchecked
            {
                return Cells.Aggregate(19, (current, cell) => current * 31 + cell.GetHashCode());
            }
        }

        public List<CompositeCell> Cells = new List<CompositeCell>();

        public List<NamedImageFrame> GetFrames()
        {
            return Cells.Select(c => c.Sheet.GenerateFrame(c.Tile)).ToList();
        }

        public static bool operator ==(CompositeFrame a, CompositeFrame b)
        {
            // If both are null, or both are same instance, return true.
            if (global::System.Object.ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
                return false;

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(CompositeFrame a, CompositeFrame b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompositeFrame)obj);
        }

        protected bool Equals(CompositeFrame otherFrame)
        {
            if (Cells.Count != otherFrame.Cells.Count) return false;
            for (var i = 0; i < Cells.Count; ++i)
                if (Cells[i] != otherFrame.Cells[i]) return false;
            return true;
        }
    }
}
