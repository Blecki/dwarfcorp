using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class CompositeCell
    {
        public SpriteSheet Sheet;
        public Color Tint;
        public Point Tile;

        //Todo: Kill this. These should NOT get compared - but it is necessary until the creatures are fixed
        //  to share resources.

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                int tintHash = 19 * 31 + Tint.GetHashCode();
                int layerHash = 19 * 31 + Sheet.GetHashCode();
                hashCode = (hashCode * 397) ^ (layerHash);
                hashCode = (hashCode * 397) ^ Tile.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(CompositeCell a, CompositeCell b)
        {
            // If both are null, or both are same instance, return true.
            if (global::System.Object.ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(CompositeCell a, CompositeCell b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CompositeCell)obj);
        }

        protected bool Equals(CompositeCell otherFrame)
        {
            if (Sheet != otherFrame.Sheet) return false;
            if (Tint != otherFrame.Tint) return false;
            if (Tile != otherFrame.Tile) return false;
            return true;
        }
    }
}
