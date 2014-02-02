using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// Specifies how a liquid is to be rendered.
    /// </summary>
    public struct LiquidAsset
    {
        public LiquidType Type;
        public Texture2D BaseTexture;
        public Texture2D FoamTexture;
        public Texture2D PuddleTexture;
        public Texture2D BumpTexture;
        public float Opactiy;
        public float SloshOpacity;
        public float WaveLength;
        public float WaveHeight;
        public float WindForce;
        public float MinOpacity;
        public Vector4 RippleColor;
    }

}