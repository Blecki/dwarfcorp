using Microsoft.Xna.Framework;

namespace DwarfCorp.Voxels.Geo
{
    public class TemplateVertex
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public VoxelVertex LogicalVertex;
        public bool ApplySlope = false;

        public TemplateVertex WithTextCoordinate(Vector2 Coord)
        {
            return new TemplateVertex
            {
                Position = this.Position,
                TextureCoordinate = Coord,
                LogicalVertex = this.LogicalVertex,
                ApplySlope = this.ApplySlope
            };
        }
    }
}