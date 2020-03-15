using Microsoft.Xna.Framework;

namespace DwarfCorp.Voxels.Geo
{
    public class TemplateVertex
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;

        public TemplateVertex(Vector3 position, Vector2 textureCoordinate)
        {
            Position = position;
            TextureCoordinate = textureCoordinate;
        }
    }
}