
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Shader : Effect
    {
        public const int MaxLights = 16;

        public Vector3[] LightPositions
        {
            set { Parameters["xLightPositions"].SetValue(value);}
        }

        public Matrix View
        {
            get { return Parameters["xView"].GetValueMatrix(); }
            set { Parameters["xView"].SetValue(value);}
        }

        public Matrix Projection
        {
            get { return Parameters["xProjection"].GetValueMatrix(); }
            set {  Parameters["xProjection"].SetValue(value);}
        }

        public Matrix World
        {
            get { return Parameters["xWorld"].GetValueMatrix(); }
            set {  Parameters["xWorld"].SetValue(value);}
        }

        public Matrix LightView
        {
            get { return Parameters["xLightView"].GetValueMatrix(); }
            set {  Parameters["xLightView"].SetValue(value);}
        }

        public Matrix LightProjection
        {
            get { return Parameters["xLightProtection"].GetValueMatrix(); }
            set {  Parameters["xLightProjection"].SetValue(value);}
        }

        public Matrix ReflectionView 
        {
            get { return Parameters["xReflectionView"].GetValueMatrix(); }
            set {  Parameters["xReflectionView"].SetValue(value);}
        }

        public float WaterOpacity
        {
            get { return Parameters["xWaterOpacity"].GetValueSingle(); }
            set {  Parameters["xWaterOpacity"].SetValue(value);}
        }

        public float MinWaterOpacity
        {
            get { return Parameters["xWaterMinOpacity"].GetValueSingle(); }
            set { Parameters["xWaterMinOpacity"].SetValue(value);}
        }

        public bool EnableLighting
        {
            get { return Parameters["xEnableLighting"].GetValueInt32() > 0; }
            set {  Parameters["xEnableLighting"].SetValue(value ? 1 : 0);}
        }

        public bool EnableShadows
        {
            get { return Parameters["xEnableShadows"].GetValueBoolean(); }
            set {  Parameters["xEnableShadows"].SetValue(value);}
        }

        public Texture2D WaterBumpMap
        {
            get { return Parameters["xWaterBumpMap"].GetValueTexture2D(); }
            set {  Parameters["xWaterBumpMap"].SetValue(value);}
        }

        public float WaveLength
        {
            get { return Parameters["xWaveLength"].GetValueSingle(); }
            set { Parameters["xWaveLength"].SetValue(value);}
        }

        public float WaveHeight
        {
            get { return Parameters["xWaveHeight"].GetValueSingle(); }
            set { Parameters["xWaveHeight"].SetValue(value);}
        }

        public Vector3 CameraPosition
        {
            get { return Parameters["xCamPos"].GetValueVector3(); }
            set {  Parameters["xCamPos"].SetValue(value);}
        }

        public float Time
        {
            get { return Parameters["xTime"].GetValueSingle(); }
            set {  Parameters["xTime"].SetValue(value);}
        }

        public float TimeOfDay
        {
            get { return Parameters["xTimeOfDay"].GetValueSingle(); }
            set {  Parameters["xTimeOfDay"].SetValue(value);}
        }

        public float WindForce
        {
            get { return Parameters["xWindForce"].GetValueSingle(); }
            set {  Parameters["xWindForce"].SetValue(value);}
        }

        public Vector3 WindDirection
        {
            get { return Parameters["xWindDirection"].GetValueVector3(); }
            set {  Parameters["xWindDirection"].SetValue(value); }
        }

        public bool EnbleFog
        {
            get { return Parameters["xEnableFog"].GetValueInt32() > 0; }
            set {  Parameters["xEnableFog"].SetValue(value ? 1 : 0);}
        }

        public float FogStart
        {
            get { return Parameters["xFogStart"].GetValueSingle(); }
            set {  Parameters["xFogStart"].SetValue(value);}
        }

        public float FogEnd
        {
            get { return Parameters["xFogEnd"].GetValueSingle(); }
            set {  Parameters["xFogEnd"].SetValue(value);}
        }

        public Color FogColor
        {
            get {  return new Color(Parameters["xFogColor"].GetValueVector3());}
            set {  Parameters["xFogColor"].SetValue(value.ToVector3());}
        }

        public Color RippleColor
        {
            get {  return new Color(Parameters["xRippleColor"].GetValueVector4());}
            set {  Parameters["xRippleColor"].SetValue(value.ToVector4());}
        }

        public Color FlatWaterColor
        {
            get {  return new Color(Parameters["xFlatColor"].GetValueVector4());}
            set {  Parameters["xFlatColor"].SetValue(value.ToVector4());}
        }

        public Vector2 PixelSize
        {
            get { return Parameters["pixelSize"].GetValueVector2(); }
            set {  Parameters["pixelSize"].SetValue(value);}
        }

        public Vector4 SelectionBufferColor
        {
            get { return Parameters["xID"].GetValueVector4(); }
            set {  Parameters["xID"].SetValue(value);}
        }

        public bool ClippingEnabled
        {
            get { return Parameters["Clipping"].GetValueInt32() > 0; }
            set { Parameters["Clipping"].SetValue(value ? 1 : 0);}
        }

        public bool GhostClippingEnabled
        {
            get { return Parameters["GhostMode"].GetValueInt32() > 0; }
            set {  Parameters["GhostMode"].SetValue(value ? 1 : 0);}
        }

        public bool SelfIlluminationEnabled
        {
            get { return Parameters["SelfIllumination"].GetValueInt32() > 0; }
            set {  Parameters["SelfIllumination"].SetValue(value ? 1 : 0);}
        }

        public Vector4 ClipPlane
        {
            get { return Parameters["ClipPlane0"].GetValueVector4(); }
            set { Parameters["ClipPlane0"].SetValue(value); }
        }

        public Texture2D MainTexture
        {
            get { return Parameters["xTexture"].GetValueTexture2D(); }
            set {  Parameters["xTexture"].SetValue(value);}
        }

        public Texture2D SelfIlluminationTexture
        {
            get { return Parameters["xIllumination"].GetValueTexture2D(); }
            set {  Parameters["xIllumination"].SetValue(value);}
        }


        public Texture2D WaterReflectionMap
        {
            get { return Parameters["xReflectionMap"].GetValueTexture2D(); }
            set {  Parameters["xReflectionMap"].SetValue(value);}
        }

        public float WaterReflectance
        {
            get { return Parameters["xWaterReflective"].GetValueSingle(); }
            set {  Parameters["xWaterReflective"].SetValue(value);}
        }

        public Texture2D SunlightGradient
        {
            get { return Parameters["xSunGradient"].GetValueTexture2D(); }
            set {  Parameters["xSunGradient"].SetValue(value);}
        }

        public Texture2D AmbientOcclusionGradient
        {
            get { return Parameters["xAmbientGradient"].GetValueTexture2D(); }
            set {  Parameters["xAmbientGradient"].SetValue(value);}
        }

        public Texture2D TorchlightGradient
        {
            get { return Parameters["xTorchGradient"].GetValueTexture2D(); }
            set {  Parameters["xTorchGradient"].SetValue(value);}
        }

        public Texture2D WaterShoreGradient
        {
            get { return Parameters["xShoreGradient"].GetValueTexture2D(); }
            set {  Parameters["xShoreGradient"].SetValue(value);}
        }

        public Texture2D LightMap
        {
            get { return Parameters["xLightMap"].GetValueTexture2D(); }
            set {  Parameters["xLightMap"].SetValue(value);}
        }

        public Color LightRampTint
        {
            get {  return new Color(Parameters["xTint"].GetValueVector4());}
            set {  Parameters["xTint"].SetValue(value.ToVector4());}
        }

        public Color VertexColorTint
        {
            get {  return new Color(Parameters["xColorTint"].GetValueVector4());}
            set {  Parameters["xColorTint"].SetValue(value.ToVector4());}
        }

        public Texture2D ShadowMap
        {
            get { return Parameters["xShadowMap"].GetValueTexture2D(); }
            set { Parameters["xShadowMap"].SetValue(value);}
        }

        public class Technique
        {
            public static string Water = "Water";
            public static string WaterFlat = "WaterFlat";
            public static string WaterTextured = "WaterTextured";
            public static string Untextured = "Untextured";
            public static string ShadowMap = "Shadow";
            public static string ShadowMapInstanced = "ShadowInstanced";
            public static string SelectionBuffer = "Selection";
            public static string Textured = "Textured";
            public static string TexturedWithLightmap = "Textured_From_Lightmap";
            public static string Lightmap = "Lightmap";
            public static string TexturedWithColorScale = "Textured_colorscale";
            public static string Instanced = "Instanced";
            public static string SelectionBufferInstanced = "Instanced_SelectionBuffer";
        }

        public Shader(GraphicsDevice graphicsDevice, byte[] effectCode) 
            : base(graphicsDevice, effectCode)
        {
            SetDefaults();
        }


        public Shader(Effect cloneSource, bool defaults) :
            this(cloneSource)
        {
            if (defaults)
            {
                SetDefaults();
            }
        }

        protected Shader(Effect cloneSource) 
            : base(cloneSource)
        {
        }

        public void SetDefaults()
        {
            FogStart = 40.0f;
            FogEnd = 80.0f;
        }
    }
}
