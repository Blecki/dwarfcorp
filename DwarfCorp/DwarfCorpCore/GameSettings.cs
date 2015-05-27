using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace DwarfCorp
{
    public class GameSettings
    {
        public class Settings
        {
            public int ResolutionX = 1280;
            public int ResolutionY = 720;
            public float ChunkDrawDistance = 100;
            public float VertexCullDistance = 80;
            public int MaxChunks = 1000;
            public int AntiAliasing = 16;
            public bool Fullscreen = false;
            public bool EnableGlow = true;
            public bool DrawSkyReflected = true;
            public bool DrawChunksReflected = true;
            public bool DrawEntityReflected = true;
            public bool DrawSkyRefracted = false;
            public bool DrawChunksRefracted = true;
            public bool DrawEntityRefracted = true;
            public bool CalculateSunlight = true;
            public bool AmbientOcclusion = true;
            public bool CalculateRamps = true;
            public float CameraScrollSpeed = 10.0f;
            public float CameraZoomSpeed = 0.5f;
            public bool EnableEdgeScroll = false;
            public int ChunkWidth = 24;
            public int ChunkHeight = 48;
            public float WorldScale = 2.0f;
            public bool DisplayIntro = true;
            public float MasterVolume = 1.0f;
            public float SoundEffectVolume = 1.0f;
            public float MusicVolume = 0.2f;
            public bool CursorLightEnabled = true;
            public bool EntityLighting = true;
            public bool SelfIlluminationEnabled = true;
            public bool ParticlePhysics = true;
            public bool GrassMotes = true;
            public int NumMotes = 1;
            public bool InvertZoom = false;
            public bool EnableAIDebugger = false;
            public float ChunkGenerateDistance = 80.0f;
            public float ChunkRebuildTime = 0.5f;
            public float ChunkUnloadDistance = 250.0f;
            public bool DrawDebugData = false;
            public float VisibilityUpdateTime = 0.1f;
            public float ChunkGenerateTime = 0.5f;
        }

        public static Settings Default { get; set; }

        public static void Reset()
        {
            Default = new Settings();
        }

        public static void Save()
        {
            Save(ContentPaths.settings);
        }

        public static void Load()
        {
            Load(ContentPaths.settings);
        }

        public static void Save(string file)
        {
            FileUtils.SaveBasicJson(Default, file);
        }

        public static void Load(string file)
        {
            try
            {
                Default = FileUtils.LoadJson<Settings>(file, false);
            }
            catch (FileNotFoundException fileLoad)
            {
                Default = new Settings();
                Save();
            }
        }
    }
}