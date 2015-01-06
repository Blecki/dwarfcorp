using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A bounded object is just anything which has 
    /// a bounding box.
    /// </summary>
    public interface IBoundedObject
    {
        BoundingBox GetBoundingBox();
        uint GetID();
    }

}