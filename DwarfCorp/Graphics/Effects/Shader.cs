
using System;
using System.Runtime.Serialization;
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
            get { return Parameters["xLightProj"].GetValueMatrix(); }
            set {  Parameters["xLightProj"].SetValue(value);}
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

        public float CaveView
        {
            get { return Parameters["xCaveView"].GetValueSingle(); }
            set { Parameters["xCaveView"].SetValue(value); }
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
            set {  Parameters["xTexture"].SetValue(value);
                if (value != null)
                {
                    TextureWidth = value.Width;
                    TextureHeight = value.Height;
                }
            }
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
            get { return Parameters["xLightmap"].GetValueTexture2D(); }
            set {  Parameters["xLightmap"].SetValue(value);}
        }

        public Color LightRamp
        {
            get {  return new Color(Parameters["xLightRamp"].GetValueVector4());}
            set {  Parameters["xLightRamp"].SetValue(value.ToVector4());}
        }

        public Color VertexColorTint
        {
            get {  return new Color(Parameters["xVertexColorMultiplier"].GetValueVector4());}
            set {  Parameters["xVertexColorMultiplier"].SetValue(value.ToVector4());}
        }

        public Texture2D ShadowMap
        {
            get { return Parameters["xShadowMap"].GetValueTexture2D(); }
            set { Parameters["xShadowMap"].SetValue(value);}
        }

        public bool EnableWind
        {
            get { return Parameters["xEnableWind"].GetValueInt32() > 0; }
            set { Parameters["xEnableWind"].SetValue(value ? 1 : 0); }
        }

        public int TextureWidth
        {
            get { return Parameters["xTextureWidth"].GetValueInt32(); }
            set { Parameters["xTextureWidth"].SetValue(value); }
        }

        public int TextureHeight
        {
            get { return Parameters["xTextureHeight"].GetValueInt32(); }
            set { Parameters["xTextureHeight"].SetValue(value); }
        }

        public int ScreenWidth
        {
            get { return Parameters["xScreenWidth"].GetValueInt32(); }
            set { Parameters["xScreenWidth"].SetValue(value); }
        }

        public int ScreenHeight
        {
            get { return Parameters["xScreenHeight"].GetValueInt32(); }
            set { Parameters["xScreenHeight"].SetValue(value); }
        }


        public int CurrentNumLights { get; set; }

        public class Technique
        {
            public static string Icon = "Icon";
            public static string Water = "Water";
            public static string WaterFlat = "WaterFlat";
            public static string WaterTextured = "WaterTextured";
            public static string Untextured = "Untextured";
            public static string Untextured_Pulse = "Untextured_Pulse";
            public static string ShadowMap = "Shadow";
            public static string ShadowMapInstanced = "ShadowInstanced";
            public static string SelectionBuffer = "Selection";
            public static string Textured_ = "Textured";
            public static string TexturedFlag = "Textured_Flag";
            public static string TexturedWithLightmap = "Textured_From_Lightmap";
            public static string Lightmap = "Lightmap";
            public static string TexturedWithColorScale = "Textured_colorscale";
            public static string Instanced_ = "Instanced";
            public static string TiledInstanced_ = "TiledInstanced";
            public static string SelectionBufferInstanced = "Instanced_SelectionBuffer";
            public static string SelectionBufferTiledInstanced = "TiledInstanced_SelectionBuffer";
            public static string Silhouette = "Silhouette";
            public static string Stipple = "Textured_Stipple";
        }


        public static string[] TexturedTechniques =
        {
            Shader.Technique.Textured_ + "_1_Light",
            Shader.Technique.Textured_ + "_2_Lights",
            Shader.Technique.Textured_ + "_3_Lights",
            Shader.Technique.Textured_ + "_4_Lights",
            Shader.Technique.Textured_ + "_5_Lights",
            Shader.Technique.Textured_ + "_6_Lights",
            Shader.Technique.Textured_ + "_7_Lights",
            Shader.Technique.Textured_ + "_8_Lights",
            Shader.Technique.Textured_ + "_9_Lights",
            Shader.Technique.Textured_ + "_10_Lights",
            Shader.Technique.Textured_ + "_11_Lights",
            Shader.Technique.Textured_ + "_12_Lights",
            Shader.Technique.Textured_ + "_13_Lights",
            Shader.Technique.Textured_ + "_14_Lights",
            Shader.Technique.Textured_ + "_15_Lights",
            Shader.Technique.Textured_ + "_16_Lights",
        };

        public static string[] InstancedTechniques =
        {
            Shader.Technique.Instanced_ + "_1_Light",
            Shader.Technique.Instanced_ + "_2_Lights",
            Shader.Technique.Instanced_ + "_3_Lights",
            Shader.Technique.Instanced_ + "_4_Lights",
            Shader.Technique.Instanced_ + "_5_Lights",
            Shader.Technique.Instanced_ + "_6_Lights",
            Shader.Technique.Instanced_ + "_7_Lights",
            Shader.Technique.Instanced_ + "_8_Lights",
            Shader.Technique.Instanced_ + "_9_Lights",
            Shader.Technique.Instanced_ + "_10_Lights",
            Shader.Technique.Instanced_ + "_11_Lights",
            Shader.Technique.Instanced_ + "_12_Lights",
            Shader.Technique.Instanced_ + "_13_Lights",
            Shader.Technique.Instanced_ + "_14_Lights",
            Shader.Technique.Instanced_ + "_15_Lights",
            Shader.Technique.Instanced_ + "_16_Lights",
        };

        public static string[] TiledInstancedTechniques =
        {
            Shader.Technique.TiledInstanced_ + "_1_Light",
            Shader.Technique.TiledInstanced_ + "_2_Lights",
            Shader.Technique.TiledInstanced_ + "_3_Lights",
            Shader.Technique.TiledInstanced_ + "_4_Lights",
            Shader.Technique.TiledInstanced_ + "_5_Lights",
            Shader.Technique.TiledInstanced_ + "_6_Lights",
            Shader.Technique.TiledInstanced_ + "_7_Lights",
            Shader.Technique.TiledInstanced_ + "_8_Lights",
            Shader.Technique.TiledInstanced_ + "_9_Lights",
            Shader.Technique.TiledInstanced_ + "_10_Lights",
            Shader.Technique.TiledInstanced_ + "_11_Lights",
            Shader.Technique.TiledInstanced_ + "_12_Lights",
            Shader.Technique.TiledInstanced_ + "_13_Lights",
            Shader.Technique.TiledInstanced_ + "_14_Lights",
            Shader.Technique.TiledInstanced_ + "_15_Lights",
            Shader.Technique.TiledInstanced_ + "_16_Lights",
        };


        public void SetTexturedTechnique()
        {
            CurrentTechnique = Techniques[TexturedTechniques[CurrentNumLights]];
        }

        public void SetIconTechnique()
        {
            CurrentTechnique = Techniques[Shader.Technique.Icon];
        }

        public void SetInstancedTechnique()
        {
            CurrentTechnique = Techniques[InstancedTechniques[CurrentNumLights]];
        }

        public void SetTiledInstancedTechnique()
        {
            CurrentTechnique = Techniques[TiledInstancedTechniques[CurrentNumLights]];
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
            CaveView = 0.0f;
            LightView = Matrix.Identity;
            LightProjection = Matrix.Identity;
            EnableWind = false;
        }
    }
}
