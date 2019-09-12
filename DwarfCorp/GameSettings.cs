using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class ColorSettings
    {
        public Dictionary<string, Microsoft.Xna.Framework.Color> Colors = new Dictionary<string, Microsoft.Xna.Framework.Color>()
        {
            {
                "Dig",
                Microsoft.Xna.Framework.Color.Red
            },
            {
                "Farm",
                Microsoft.Xna.Framework.Color.LimeGreen
            },
            {
                "Guard",
                Microsoft.Xna.Framework.Color.Blue
            },
            {
                "Attack",
                Microsoft.Xna.Framework.Color.Red
            },
            {
                "Harvest",
                Microsoft.Xna.Framework.Color.LightGreen
            },
            {
                "Highlight",
                Microsoft.Xna.Framework.Color.DarkRed
            },
            {
                "Positive",
                Microsoft.Xna.Framework.Color.Green
            },
            {
                "Negative",
                Microsoft.Xna.Framework.Color.Red
            },
            {
                "Low Health",
                Microsoft.Xna.Framework.Color.Red
            },
            {
                "Medium Health",
                Microsoft.Xna.Framework.Color.Orange
            },
            {
                "High Health",
                Microsoft.Xna.Framework.Color.LightGreen
            },
            {
                "Catch",
                Microsoft.Xna.Framework.Color.Tomato
            },
            {
                "Gather",
                Microsoft.Xna.Framework.Color.OrangeRed
            }

        };

        public static Dictionary<string, ColorSettings> Profiles = new Dictionary<string, ColorSettings>()
        {
            {
                "Default",
                new ColorSettings()
            },
            {
                "Colorblind",
                new ColorSettings()
                {
                    Colors = new Dictionary<string, Microsoft.Xna.Framework.Color>()
                    {
                        {
                            "Dig",
                            Microsoft.Xna.Framework.Color.White
                        },
                        {
                            "Farm",
                            Microsoft.Xna.Framework.Color.Aqua
                        },
                        {
                            "Guard",
                            Microsoft.Xna.Framework.Color.Orange
                        },
                        {
                            "Attack",
                            Microsoft.Xna.Framework.Color.Tomato
                        },
                        {
                            "Harvest",
                            Microsoft.Xna.Framework.Color.Aqua
                        },
                        {
                            "Highlight",
                            Microsoft.Xna.Framework.Color.Blue
                        },
                        {
                            "Positive",
                            Microsoft.Xna.Framework.Color.Aqua
                        },
                        {
                            "Negative",
                            Microsoft.Xna.Framework.Color.Yellow
                        },
                        {
                            "Low Health",
                            Microsoft.Xna.Framework.Color.Yellow
                        },
                        {
                            "Medium Health",
                            Microsoft.Xna.Framework.Color.Orange
                        },
                        {
                            "High Health",
                            Microsoft.Xna.Framework.Color.Aqua
                        },
                        {
                            "Catch",
                            Microsoft.Xna.Framework.Color.Purple
                        },
                        {
                            "Gather",
                            Microsoft.Xna.Framework.Color.Pink
                        }
                    }
                }
            }
        };

        public ColorSettings Clone()
        {
            var colorSettings = new ColorSettings();
            colorSettings.Colors.Clear();
            foreach(var setting in Colors)
            {
                colorSettings.Colors.Add(setting.Key, setting.Value);
            }
            return colorSettings;
        }

        public Microsoft.Xna.Framework.Color GetColor(string color, Microsoft.Xna.Framework.Color def)
        {
            Microsoft.Xna.Framework.Color outColor;
            if(Colors.TryGetValue(color, out outColor))
            {
                return outColor;
            }
            return def;
        }

        public void SetColor(string color, Microsoft.Xna.Framework.Color value)
        {
            Colors[color] = value;
        }
    }

    public class GameSettings
    {
        public class Settings
        {
            public bool TutorialDisabledGlobally = false;
            public int ResolutionX = 1280;
            public int ResolutionY = 720;
            public int GuiScale = 1;
            public bool GuiAutoScale = true;
            public int ChunkDrawDistance = 100;
            public float VertexCullDistance = 1000;
            public int EntityUpdateDistance = 128;
            public int AntiAliasing = 0;
            public bool Fullscreen = false;
            public bool EnableGlow = true;
            public bool DrawSkyReflected = true;
            public bool DrawChunksReflected = true;
            public bool DrawEntityReflected = true;
            public bool AmbientOcclusion = false;
            public bool CalculateRamps = true;
            public float CameraScrollSpeed = 10.0f;
            public float CameraZoomSpeed = 0.5f;
            public bool EnableEdgeScroll = false;
            public float WorldScale = 4.0f;
            public bool DisplayIntro = true;
            public float MasterVolume = 1.0f;
            public float SoundEffectVolume = 1.0f;
            public float MusicVolume = 0.2f;
            public bool CursorLightEnabled = true;
            public bool EntityLighting = true;
            public bool SelfIlluminationEnabled = true;
            public bool ParticlePhysics = true;
            public bool GrassMotes = true;
            public int NumMotes = 512;
            public bool InvertZoom = false;
            public float VisibilityUpdateTime = 0.1f;
            public bool FogofWar = true;
            public bool AutoSave = true;
            public int AutoSaveTimeMinutes = 20;
            public string SaveLocation = null;
            public bool VSync = true;
            public bool AllowReporting = true;
            public bool ZoomCameraTowardMouse = false;
            public bool CameraFollowSurface = true;
            public String LocalModDirectory = "Mods";
            public String SteamModDirectory = "C:/Program Files/Steam/steamapps/workshop/content/252390";
            public List<String> EnabledMods = new List<String>();
            public int MaxSaves = 15;
            public bool EnableSlowMotion = false;
            public int ConsoleTextSize = 2;
            public float HoursUnhappyBeforeQuitting = 4.0f;
            public ColorSettings Colors = new ColorSettings();
            public bool AllowAutoDigging = true;
            public bool AllowAutoFarming = true;
            public float FNAONLY_KeyRepeatRate = 0.1f;
            public int DwarfArrivalDelayHours = 4;
            public int MaxLiveChunks = 10; // How many chunks can have geometry saved
            public float SpeciesLimitAdjust = 1.0f;
            public int LandCost = 3;

            public bool DisableWeather = true;

            public bool AllowIdleCrafting = false;

            [AutoResetBool(false)] public bool FastGen = false;
            [AutoResetFloat(0.15f)] public float GenerationRuinsRate = 0.15f;
            [AutoResetBool(false)] public bool NoStone = false;

            [AutoResetFloat(-10.0f)] public float Boredom_Gamble = -10.0f;
            [AutoResetFloat(0.1f)] public float Boredom_NormalTask = -0.1f;
            [AutoResetFloat(-0.1f)] public float Boredom_Sleep = 0.1f;
            [AutoResetFloat(-0.1f)] public float Boredom_ExcitingTask = -0.5f;
            [AutoResetFloat(0.5f)] public float Boredom_BoringTask = 0.5f;
            [AutoResetFloat(-0.1f)] public float Boredom_Eat = -0.1f;
            [AutoResetFloat(-0.2f)] public float Boredom_Walk = -0.2f;

            [AutoResetFloat(-0.5f)] public float Energy_Arduous = -0.5f;
            [AutoResetFloat(-0.1f)] public float Energy_Tiring = -0.1f;
            [AutoResetFloat(0.1f)] public float Energy_Refreshing = 0.1f;
            [AutoResetFloat(1.0f)] public float Energy_Restful = 1.0f;

            [AutoResetFloat(0.75f)] public float CreatureMovementAdjust = 0.75f;
            [AutoResetFloat(0.01f)] public float DrownChance = 0.01f;

            [AutoResetFloat(0.03f)] public float IdleBehavior_Craft = 0.03f;
            [AutoResetFloat(0.02f)] public float IdleBehavior_Binge = 0.02f;
            [AutoResetFloat(0.2f)] public float IdleBehavior_Train = 0.2f;
            [AutoResetFloat(0.1f)] public float IdleBehavior_Walk = 0.1f;
            [AutoResetFloat(0.8f)] public float IdleBehavior_Relax = 0.8f;
            [AutoResetFloat(0.8f)] public float IdleBehavior_Gamble = 0.8f;
            [AutoResetFloat(2.0f)] public float IdleBehavior_Research = 2.0f;
            [AutoResetFloat(0.1f)] public float IdleBehavior_Mourn = 0.1f;
            [AutoResetFloat(0.2f)] public float IdleBehavior_Potions = 0.2f;
            [AutoResetFloat(4.0f)] public float IdleBehavior_Loiter = 4.0f;
            [AutoResetFloat(2.0f)] public float IdleBehavior_Heal = 2.0f;
            [AutoResetFloat(32.0f)] public float IdleBehavior_Restock = 32.0f;



            public int MaxVoxelDesignations = 1024;
            public int NumPathingThreads = 2;

            public Settings Clone()
            {
                return MemberwiseClone() as Settings;
            }

            public override string ToString()
            {
                return FileUtils.SerializeBasicJSON(this);
            }
        }

        public static Settings Default { get; set; }

        /// <summary>
        /// Use this attribute to flag a float setting that can be tweaked during execution, but 
        /// should never be saved. (It does save, but will be reset everytime it's loaded.)
        /// </summary>
        private class AutoResetFloatAttribute : Attribute
        {
            public float Value;

            public AutoResetFloatAttribute(float Value)
            {
                this.Value = Value;
            }
        }       
        
        private class AutoResetBoolAttribute : Attribute
        {
            public bool Value;

            public AutoResetBoolAttribute(bool Value)
            {
                this.Value = Value;
            }
        }

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
            try
            {
                FileUtils.SaveJSON(Default, file);
                Console.Out.WriteLine("Saving settings to {0} : {1}", file, GameSettings.Default.ToString());
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Failed to save settings: {0}", exception.ToString());
                if (exception.InnerException != null)
                {
                    Console.Error.WriteLine("Inner exception: {0}", exception.InnerException.ToString()); 
                }
            }
        }

        public static void Load(string file)
        {
            try
            {
                Default = FileUtils.LoadJsonFromAbsolutePath<Settings>(file);

                foreach (var member in Default.GetType().GetFields())
                    foreach (var attribute in member.GetCustomAttributes(false))
                    {
                        if (attribute is AutoResetFloatAttribute resetFloat)
                        {
                            member.SetValue(Default, resetFloat.Value);
                            Console.Out.WriteLine("Auto Reset Float Setting: {0} to {1}", member.Name, resetFloat.Value);
                        }

                        if (attribute is AutoResetBoolAttribute resetBool)
                        {
                            member.SetValue(Default, resetBool.Value);
                            Console.Out.WriteLine("Auto Reset Bool Setting: {0} to {1}", member.Name, resetBool.Value);
                        }
                    }

                Console.Out.WriteLine("Loaded settings {0}", file);
            }
            catch (FileNotFoundException)
            {
				Console.Error.WriteLine("Settings file {0} does not exist. Using default settings.", file);
                Default = new Settings();
                Save();
            }
            catch (Exception otherException)
            { 
                Console.Error.WriteLine("Failed to load settings file {0} : {1}", file, otherException.ToString());
                if (otherException.InnerException != null)
                {
                    Console.Error.WriteLine("Inner exception: {0}", otherException.InnerException.ToString());
                }
                Default = new Settings();
                Save();
            }
            // mklingen (I have made it impossible to disable fog of war for performance reasons).
            Default.FogofWar = true; // Todo: Check on this - autoreset?
        }

        [ConsoleCommandHandler("SHOW")]
        public static string ShowSetting(String Name)
        {
            var member = typeof(Settings).GetFields().FirstOrDefault(f => f.Name == Name);
            if (member == null)
                return "No such setting.";
            var value = member.GetValue(Default);
            if (value == null)
                return "NULL";
            return value.ToString();
        }

        [ConsoleCommandHandler("LIST")]
        public static string ListSettings(String Name)
        {
            var builder = new StringBuilder();
            foreach (var member in typeof(Settings).GetFields())
                builder.AppendLine(member.Name);
            return builder.ToString();
        }

        [ConsoleCommandHandler("SAVESETTINGS")]
        public static string SaveSettings(String Name)
        {
            GameSettings.Save();
            return "Saved.";
        }

        [ConsoleCommandHandler("SET")]
        public static string Set(String Name)
        {
            var setting = "";
            var value = "";
            var space = Name.IndexOf(' ');
            if (space != -1)
            {
                setting = Name.Substring(0, space);
                value = Name.Substring(space + 1);
            }
            else
                setting = Name;

            var member = typeof(Settings).GetFields().FirstOrDefault(f => f.Name == setting);
            if (member == null)
                return "No such setting.";

            try
            {
                member.SetValue(GameSettings.Default, Convert.ChangeType(value, member.FieldType));
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return "Set.";
        }
    }
}