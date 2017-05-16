using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum;
using Gum.Widgets;
using System;

namespace DwarfCorp.GameStates
{
    public class NewOptionsState : GameState
    {
        private Gum.Root GuiRoot;
        private bool HasChanges = false;
        public Action OnClosed = null;
        private bool BuildingGUI = false;

        private Dictionary<string, int> AntialiasingOptions;
        private Dictionary<string, DisplayMode> DisplayModes;
        
        private Gum.Widget MainPanel;
        private Gum.Widgets.TabPanel TabPanel;

        private HorizontalFloatSlider MoveSpeed;
        private HorizontalFloatSlider ZoomSpeed;
        private CheckBox InvertZoom;
        private CheckBox EdgeScrolling;
        private CheckBox FogOfWar;
        private CheckBox PlayIntro;
        private ComboBox GuiScale;
        private HorizontalFloatSlider MasterVolume;
        private HorizontalFloatSlider SFXVolume;
        private HorizontalFloatSlider MusicVolume;
        private Gum.Widgets.ComboBox Resolution;
        private CheckBox Fullscreen;
        private HorizontalFloatSlider ChunkDrawDistance;
        private HorizontalFloatSlider VertexCullDistance;
        private CheckBox Glow;
        private Gum.Widgets.ComboBox Antialiasing;
        private CheckBox ReflectTerrain;
        private CheckBox ReflectEntities;
        private CheckBox Sunlight;
        private CheckBox AmbientOcclusion;
        private CheckBox Ramps;
        private CheckBox CursorLight;
        private CheckBox EntityLight;
        private CheckBox SelfIllumination;
        private CheckBox ParticlePhysics;
        private CheckBox Motes;
        private HorizontalFloatSlider NumMotes;
        private CheckBox LightMap;
        private CheckBox DynamicShadows;
        private Gum.Widgets.ComboBox EasyGraphicsSetting;

        public NewOptionsState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, "NewOptionsState", StateManager)
        { }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            // Setup antialiasing options.
            AntialiasingOptions = new Dictionary<string, int>
            {
                {"NONE", 0},
                {"FXAA", -1},
                {"2x MSAA", 2},
                {"4x MSAA", 4},
                {"16x MSAA", 16}
            };

            DisplayModes = new Dictionary<string, DisplayMode>();
            foreach (var displayMode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes.Where(dm =>
                dm.Format == SurfaceFormat.Color && dm.Width >= 640))
                DisplayModes.Add(string.Format("{0} x {1}", displayMode.Width, displayMode.Height), displayMode);

            RebuildGui();

            // Must be true or Render will not be called.
            IsInitialized = true;

            base.OnEnter();
        }

        private void RebuildGui()
        {
            BuildingGUI = true;

            // Create and initialize GUI framework.
            GuiRoot = new Gum.Root(DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);
            GuiRoot.SetMouseOverlay(null, 0);
            // CONSTRUCT GUI HERE...
            MainPanel = GuiRoot.RootItem.AddChild(new Gum.Widget
                {
                    Rect = GuiRoot.RenderData.VirtualScreen,
                    Padding = new Margin(4,4,4,4),
                    Transparent = true
                });

            MainPanel.AddChild(new Gum.Widgets.Button
            {
                Text = "Close",
                Font = "font-hires",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    // If changes, prompt before closing.
                    if (HasChanges)
                    {
                        var confirm = new NewGui.Confirm
                            {
                                Text = "Apply changes?",
                                OkayText = "Yes",
                                CancelText = "No",
                                OnClose = (s2) =>
                                    {
                                        if ((s2 as NewGui.Confirm).DialogResult == NewGui.Confirm.Result.OKAY)
                                            ApplySettings();
                                        if (OnClosed != null) OnClosed();
                                        StateManager.PopState();
                                    }
                            };
                        GuiRoot.ShowPopup(confirm);
                    }
                    else
                    {
                        if (OnClosed != null) OnClosed();
                        StateManager.PopState();
                    }
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            MainPanel.AddChild(new Gum.Widgets.Button
            {
                Text = "Apply",
                Font = "font-hires",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    ApplySettings();
                },
                AutoLayout = AutoLayout.FloatBottomRight,
                OnLayout = s => s.Rect.X -= 128 // Hack to keep it from floating over the other button.
            });

            TabPanel = MainPanel.AddChild(new Gum.Widgets.TabPanel
            {
                AutoLayout = AutoLayout.DockFill,
                TextSize = 1,
                SelectedTabColor = new Vector4(1,0,0,1),
                OnLayout = (sender) => sender.Rect.Height -= 36 // Keep it from overlapping bottom buttons.
            }) as Gum.Widgets.TabPanel;

            CreateGameplayTab();
            CreateAudioTab();
            CreateKeysTab();
            CreateGraphicsTab();

            TabPanel.SelectedTab = 0;

            GuiRoot.RootItem.Layout();

            LoadSettings();

            BuildingGUI = false;
        }

        private Widget LabelAndDockWidget(string Label, Widget Widget)
        {
            var r = GuiRoot.ConstructWidget(new Widget
            {
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0,0,4,4),
                ChangeColorOnHover = true,
                HoverTextColor = new Vector4(0.5f, 0, 0, 1.0f)
            });

            var label = new Widget
            {
                Text = Label,
                AutoLayout = AutoLayout.DockLeft,
            };

            r.AddChild(label);

            Widget.AutoLayout = AutoLayout.DockFill;
            r.AddChild(Widget);
            Widget.OnMouseEnter += (sender, args) =>
            {
                label.TextColor = Color.DarkRed.ToVector4();
                label.Invalidate();
            };

            Widget.OnMouseLeave += (sender, args) =>
            {
                label.TextColor = Color.Black.ToVector4();
                label.Invalidate();
            };
            return r;
        }

        private void CreateGameplayTab()
        {
            var panel = TabPanel.AddTab("GAMEPLAY", new Widget
                {
                    Border = "border-thin",
                    Padding = new Margin(4,4,0,0)
                });

            panel.AddChild(new Widget
            {
                Text = "Restore default settings",
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0, 0, 4, 4),
                Border = "border-button",
                ChangeColorOnHover = true,
                OnClick = (sender, args) =>
                {
                    var prompt = GuiRoot.ConstructWidget(new NewGui.Confirm
                    {
                        Text = "Set all settings to their default?",
                        OnClose = (confirm) =>
                        {
                            if ((confirm as NewGui.Confirm).DialogResult == NewGui.Confirm.Result.OKAY)
                            {
                                GameSettings.Default = new GameSettings.Settings();
                                RebuildGui();
                                ApplySettings();
                            }
                        }
                    });

                    GuiRoot.ShowDialog(prompt);
                }
            });

            // Todo: Display actual value beside slider.
            MoveSpeed = panel.AddChild(LabelAndDockWidget("Camera Move Speed", new HorizontalFloatSlider
                {
                    ScrollArea = 20,
                    OnScroll = OnItemChanged,
                    Tooltip = "Sensitivity of the camera to the movement keys"
                })).GetChild(1) as HorizontalFloatSlider;

            ZoomSpeed = panel.AddChild(LabelAndDockWidget("Camera Zoom Speed", new HorizontalFloatSlider
            {
                ScrollArea = 2,
                OnScroll = OnItemChanged,
                Tooltip = "Sensitivity of the camera to zooming"
            })).GetChild(1) as HorizontalFloatSlider;

            InvertZoom = panel.AddChild(new CheckBox
                {
                    Text = "Invert Zoom",
                    Tooltip = "When checked, zooming in/out with the scroll wheel will be inverted",
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop
                }) as CheckBox;

            EdgeScrolling = panel.AddChild(new CheckBox
            {
                Text = "Edge Scrolling",
                Tooltip = "When checked, moving the cursor to the edge of the screen will move the camera.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            PlayIntro = panel.AddChild(new CheckBox
            {
                Text = "Play Intro",
                Tooltip = "When checked, the intro animation will play at the beginning of the game.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            FogOfWar = panel.AddChild(new CheckBox
            {
                Text = "Fog Of War",
                Tooltip = "When checked, unexplored tiles underground will be invisible.",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            GuiScale = panel.AddChild(LabelAndDockWidget("Gui Scale", new ComboBox
            {
                Items = new List<string>(new String[] { "1", "2", "3", "4", "5" }),
            })).GetChild(1) as ComboBox;
        }

        private void CreateAudioTab()
        {
            var panel = TabPanel.AddTab("AUDIO", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0)
            });

            MasterVolume = panel.AddChild(LabelAndDockWidget("Master Volume", new HorizontalFloatSlider
            {
                ScrollArea = 1.0f,
                OnScroll = OnItemChanged,
                Tooltip = "Volume of all sounds in the game.",
            })).GetChild(1) as HorizontalFloatSlider;

            SFXVolume = panel.AddChild(LabelAndDockWidget("SFX Volume", new HorizontalFloatSlider
            {
                ScrollArea = 1.0f,
                OnScroll = OnItemChanged,
                Tooltip = "Volume of sound effects."
            })).GetChild(1) as HorizontalFloatSlider;

            MusicVolume = panel.AddChild(LabelAndDockWidget("Music Volume", new HorizontalFloatSlider
            {
                ScrollArea = 1.0f,
                OnScroll = OnItemChanged,
                Tooltip = "Volume of background music."
            })).GetChild(1) as HorizontalFloatSlider;
        }

        private void CreateKeysTab()
        {
            // Todo: Scroll when list is too long.
            var panel = TabPanel.AddTab("KEYS", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0)
            });

            panel.AddChild(new NewGui.KeyEditor
            {
                KeyManager = new KeyManager(),
                AutoLayout = AutoLayout.DockFill
            });
        }

        private void CreateGraphicsTab()
        {
            var panel = TabPanel.AddTab("GRAPHICS", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4,4,4,4)
            });

            var split = panel.AddChild(new NewGui.TwoColumns
            {
                AutoLayout = AutoLayout.DockFill
            }) as NewGui.TwoColumns;

            var leftPanel = split.AddChild(new Widget
            {
                Padding = new Margin(2,2,2,2)
            });

            var rightPanel = split.AddChild(new Widget
            {
                Padding = new Margin(2,2,2,2)
            });

            EasyGraphicsSetting = leftPanel.AddChild(LabelAndDockWidget("Easy Settings", new Gum.Widgets.ComboBox
            {
                Items = new String[] { "Lowest", "Low", "Medium", "High", "Highest", "Custom" }.ToList(),
                OnSelectedIndexChanged = OnEasySettingChanged,
                Border = "border-thin"
            })).GetChild(1) as Gum.Widgets.ComboBox;

            Resolution = leftPanel.AddChild(LabelAndDockWidget("Resolution", new Gum.Widgets.ComboBox
                {
                    Items = DisplayModes.Select(dm => dm.Key).ToList(),
                    OnSelectedIndexChanged = OnItemChanged,
                    Border = "border-thin",
                    Tooltip = "Game screen size",
                })).GetChild(1) as Gum.Widgets.ComboBox;

            Fullscreen = leftPanel.AddChild(new CheckBox
                {
                    Text = "Fullscreen",
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop,
                    Tooltip = "When checked, game will take up the whole screen."
                }) as CheckBox;

            ChunkDrawDistance = leftPanel.AddChild(LabelAndDockWidget("Terrain Draw Distance", new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnScroll = OnItemChanged,
                Tooltip = "Higher values allow you to see more terrain. Lower values will make the game run faster."
            })).GetChild(1) as HorizontalFloatSlider;

            VertexCullDistance = leftPanel.AddChild(LabelAndDockWidget("Geometry Draw Distance",
                new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnScroll = OnItemChanged,
                Tooltip = "Higher values allow you to see more terrain. Lower values will make the game run faster."
            })).GetChild(1) as HorizontalFloatSlider;

            /*
            GenerateDistance = leftPanel.AddChild(LabelAndDockWidget("Generate Distance",
                new HorizontalFloatSlider
                {
                    ScrollArea = 1000f,
                    OnScroll = OnItemChanged
                })).GetChild(1) as HorizontalFloatSlider;
             */

            Glow = leftPanel.AddChild(new CheckBox
            {
                Text = "Glow",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, bright parts of the screen will have a glow effect. Turn off to make the game run faster."
            }) as CheckBox;

            Antialiasing = rightPanel.AddChild(LabelAndDockWidget("Antialiasing", new Gum.Widgets.ComboBox
            {
                Items = AntialiasingOptions.Select(o => o.Key).ToList(),
                OnSelectedIndexChanged = OnItemChanged,
                Border = "border-thin",
                Tooltip = "Higher values mean fewer jagged pixels. For best quality use FXAA. Fastest is no antialiasing."
            })).GetChild(1) as Gum.Widgets.ComboBox;

            ReflectTerrain = rightPanel.AddChild(new CheckBox
            {
                Text = "Water Reflects Terrain",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, water will reflect the terrain. Turn off to increase game performance."
            }) as CheckBox;

            ReflectEntities = rightPanel.AddChild(new CheckBox
            {
                Text = "Water Reflects Entities",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, water will reflect entities. Turn off to increase game performance."
            }) as CheckBox;

            Sunlight = rightPanel.AddChild(new CheckBox
            {
                Text = "Sunlight",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, caves will be darker than sunlit areas. Turn off to increase game performance."
            }) as CheckBox;

            AmbientOcclusion = rightPanel.AddChild(new CheckBox
            {
                Text = "Ambient Occlusion",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "Enables smooth lighting effects on terrain. Turn off to increase game performance."
            }) as CheckBox;

            Ramps = leftPanel.AddChild(new CheckBox
            {
                Text = "Terrain Slopes",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "Causes dirt/sand to have slopes. Turn off to increase game performance."
            }) as CheckBox;

            CursorLight = rightPanel.AddChild(new CheckBox
            {
                Text = "Cursor Light",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, the cursor casts light. Turn off to increase game performance."
            }) as CheckBox;

            EntityLight = rightPanel.AddChild(new CheckBox
            {
                Text = "Dynamic Entity Lighting",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, creatures underground will be in shadow. Turn off to increase game performance."
            }) as CheckBox;

            SelfIllumination = rightPanel.AddChild(new CheckBox
            {
                Text = "Ore Glow",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked certain ores will glow underground. Turn off to increase game performance."
            }) as CheckBox;

            ParticlePhysics = leftPanel.AddChild(new CheckBox
            {
                Text = "Particle Physics",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, certain particles will bounce off of terrain. Turn off to increase game performance."
            }) as CheckBox;

            Motes = leftPanel.AddChild(new CheckBox
            {
                Text = "Motes",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, detail grass motes will spawn. Turn off to increase game performance."
            }) as CheckBox;

            NumMotes = leftPanel.AddChild(LabelAndDockWidget("Max Number of Entities",
                 new HorizontalFloatSlider
                 {
                     ScrollArea = 2048 - 100,
                     OnScroll = OnItemChanged,
                     Tooltip = "Controls how many of each type of entity will get drawn to the screen. Higher values mean more detail. Lower values mean better performance."

                 })).GetChild(1) as HorizontalFloatSlider;

            LightMap = leftPanel.AddChild(new CheckBox
            {
                Text = "Light Maps",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, light maps will be used for pixelated terrain lighting. Turning this off increases performance."
            }) as CheckBox;

            DynamicShadows = leftPanel.AddChild(new CheckBox
            {
                Text = "Dynamic Shadows",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop,
                Tooltip = "When checked, the sun will cast shadows on terrain and entities. Turning this off increases performance."
            }) as CheckBox;

        }

        private void OnItemChanged(Gum.Widget Sender)
        {
            HasChanges = true;
        }

        private void OnEasySettingChanged(Gum.Widget Sender)
        {
            // This handler can be called while building the GUI, resulting in an infinite loop.
            if (BuildingGUI) return;

            GuiRoot.ShowDialog(GuiRoot.ConstructWidget(new NewGui.Confirm
            {
                Text = "This will automatically apply changes",
                OnClose = (confirm) =>
                {
                    if ((confirm as NewGui.Confirm).DialogResult == NewGui.Confirm.Result.OKAY)
                    {
                        var comboBox = Sender as Gum.Widgets.ComboBox;
                        var selection = comboBox.SelectedItem;

                        switch (selection)
                        {
                            case "Custom":
                                break;
                            case "Lowest":
                                GameSettings.Default.AmbientOcclusion = false;
                                GameSettings.Default.AntiAliasing = 0;
                                GameSettings.Default.CalculateRamps = false;
                                GameSettings.Default.CalculateSunlight = false;
                                GameSettings.Default.CursorLightEnabled = false;
                                GameSettings.Default.DrawChunksReflected = false;
                                GameSettings.Default.DrawEntityReflected = false;
                                GameSettings.Default.DrawSkyReflected = false;
                                GameSettings.Default.UseLightmaps = false;
                                GameSettings.Default.UseDynamicShadows = false;
                                GameSettings.Default.EntityLighting = false;
                                GameSettings.Default.EnableGlow = false;
                                GameSettings.Default.SelfIlluminationEnabled = false;
                                GameSettings.Default.NumMotes = 100;
                                GameSettings.Default.GrassMotes = false;
                                GameSettings.Default.ParticlePhysics = false;
                                break;
                            case "Low":
                                GameSettings.Default.AmbientOcclusion = false;
                                GameSettings.Default.AntiAliasing = 0;
                                GameSettings.Default.CalculateRamps = true;
                                GameSettings.Default.CalculateSunlight = true;
                                GameSettings.Default.CursorLightEnabled = false;
                                GameSettings.Default.DrawChunksReflected = false;
                                GameSettings.Default.DrawEntityReflected = false;
                                GameSettings.Default.DrawSkyReflected = true;
                                GameSettings.Default.UseLightmaps = false;
                                GameSettings.Default.UseDynamicShadows = false;
                                GameSettings.Default.EntityLighting = true;
                                GameSettings.Default.EnableGlow = false;
                                GameSettings.Default.SelfIlluminationEnabled = false;
                                GameSettings.Default.NumMotes = 300;
                                GameSettings.Default.GrassMotes = false;
                                GameSettings.Default.ParticlePhysics = false;
                                break;
                            case "Medium":
                                GameSettings.Default.AmbientOcclusion = true;
                                GameSettings.Default.AntiAliasing = 4;
                                GameSettings.Default.CalculateRamps = true;
                                GameSettings.Default.CalculateSunlight = true;
                                GameSettings.Default.CursorLightEnabled = true;
                                GameSettings.Default.DrawChunksReflected = true;
                                GameSettings.Default.DrawEntityReflected = false;
                                GameSettings.Default.DrawSkyReflected = true;
                                GameSettings.Default.UseLightmaps = false;
                                GameSettings.Default.UseDynamicShadows = false;
                                GameSettings.Default.EntityLighting = true;
                                GameSettings.Default.EnableGlow = false;
                                GameSettings.Default.SelfIlluminationEnabled = true;
                                GameSettings.Default.NumMotes = 500;
                                GameSettings.Default.GrassMotes = true;
                                GameSettings.Default.ParticlePhysics = true;
                                break;
                            case "High":
                                GameSettings.Default.AmbientOcclusion = true;
                                GameSettings.Default.AntiAliasing = 16;
                                GameSettings.Default.CalculateRamps = true;
                                GameSettings.Default.CalculateSunlight = true;
                                GameSettings.Default.CursorLightEnabled = true;
                                GameSettings.Default.DrawChunksReflected = true;
                                GameSettings.Default.DrawEntityReflected = true;
                                GameSettings.Default.DrawSkyReflected = true;
                                GameSettings.Default.UseLightmaps = true;
                                GameSettings.Default.UseDynamicShadows = false;
                                GameSettings.Default.EntityLighting = true;
                                GameSettings.Default.EnableGlow = true;
                                GameSettings.Default.SelfIlluminationEnabled = true;
                                GameSettings.Default.NumMotes = 1500;
                                GameSettings.Default.GrassMotes = true;
                                GameSettings.Default.ParticlePhysics = true;
                                break;
                            case "Highest":
                                GameSettings.Default.AmbientOcclusion = true;
                                GameSettings.Default.AntiAliasing = -1;
                                GameSettings.Default.CalculateRamps = true;
                                GameSettings.Default.CalculateSunlight = true;
                                GameSettings.Default.CursorLightEnabled = true;
                                GameSettings.Default.DrawChunksReflected = true;
                                GameSettings.Default.DrawEntityReflected = false;
                                GameSettings.Default.DrawSkyReflected = true;
                                GameSettings.Default.UseLightmaps = true;
                                GameSettings.Default.UseDynamicShadows = true;
                                GameSettings.Default.EntityLighting = true;
                                GameSettings.Default.EnableGlow = true;
                                GameSettings.Default.SelfIlluminationEnabled = true;
                                GameSettings.Default.NumMotes = 2048;
                                GameSettings.Default.GrassMotes = true;
                                GameSettings.Default.ParticlePhysics = true;
                                break;
                        }

                        HasChanges = true;
                        RebuildGui();
                        ApplySettings();
                        TabPanel.SelectedTab = 3;
                    }
                }
            }));
        }

        public void SetBestResolution()
        {
            this.Resolution.SelectedIndex = this.Resolution.Items.IndexOf(string.Format("{0} x {1}",
                GameSettings.Default.ResolutionX, GameSettings.Default.ResolutionY));

            if (this.Resolution.SelectedIndex != -1) return;

            string bestMode = null;
            int bestScore = int.MaxValue;
            foreach (var mode in DisplayModes)
            {
                int score = System.Math.Abs(mode.Value.Width - GameSettings.Default.ResolutionX) +
                            System.Math.Abs(mode.Value.Height - GameSettings.Default.ResolutionY);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMode = mode.Key;
                }
            }

            this.Resolution.SelectedIndex = this.Resolution.Items.IndexOf(bestMode);
        }

        private void ApplySettings()
        {
            // Copy all the states from widgets to game settings.

            // Gameplay settings
            GameSettings.Default.CameraScrollSpeed = this.MoveSpeed.ScrollPosition;
            GameSettings.Default.CameraZoomSpeed = this.ZoomSpeed.ScrollPosition;
            GameSettings.Default.EnableEdgeScroll = this.EdgeScrolling.CheckState;
            GameSettings.Default.FogofWar = this.FogOfWar.CheckState;
            GameSettings.Default.InvertZoom = this.InvertZoom.CheckState;
            GameSettings.Default.DisplayIntro = this.PlayIntro.CheckState;
          
            // Audio settings
            GameSettings.Default.MasterVolume = this.MasterVolume.ScrollPosition;
            GameSettings.Default.SoundEffectVolume = this.SFXVolume.ScrollPosition;
            GameSettings.Default.MusicVolume = this.MusicVolume.ScrollPosition;

            // Graphics settings
            var preResolutionX = GameSettings.Default.ResolutionX;
            var preResolutionY = GameSettings.Default.ResolutionY;
            var preFullscreen = GameSettings.Default.Fullscreen;
            var preGuiScale = GameSettings.Default.GuiScale;

            var newDisplayMode = DisplayModes[this.Resolution.SelectedItem];
            GameSettings.Default.ResolutionX = newDisplayMode.Width;
            GameSettings.Default.ResolutionY = newDisplayMode.Height;

            GameSettings.Default.Fullscreen = this.Fullscreen.CheckState;
            GameSettings.Default.ChunkDrawDistance = this.ChunkDrawDistance.ScrollPosition + 1.0f;
            GameSettings.Default.VertexCullDistance = this.VertexCullDistance.ScrollPosition + 0.1f;
            //GameSettings.Default.ChunkGenerateDistance = this.GenerateDistance.ScrollPosition + 1.0f;
            GameSettings.Default.EnableGlow = this.Glow.CheckState;
            GameSettings.Default.AntiAliasing = AntialiasingOptions[this.Antialiasing.SelectedItem];
            GameSettings.Default.DrawChunksReflected = this.ReflectTerrain.CheckState;
            GameSettings.Default.DrawEntityReflected = this.ReflectEntities.CheckState;
            GameSettings.Default.CalculateSunlight = this.Sunlight.CheckState;
            GameSettings.Default.AmbientOcclusion = this.AmbientOcclusion.CheckState;
            GameSettings.Default.CalculateRamps = this.Ramps.CheckState;
            GameSettings.Default.CursorLightEnabled = this.CursorLight.CheckState;
            GameSettings.Default.EntityLighting = this.EntityLight.CheckState;
            GameSettings.Default.SelfIlluminationEnabled = this.SelfIllumination.CheckState;
            GameSettings.Default.ParticlePhysics = this.ParticlePhysics.CheckState;
            GameSettings.Default.GrassMotes = this.Motes.CheckState;
            GameSettings.Default.NumMotes = (int)this.NumMotes.ScrollPosition + 100;
            GameSettings.Default.UseLightmaps = this.LightMap.CheckState;
            GameSettings.Default.UseDynamicShadows = this.DynamicShadows.CheckState;

            GameSettings.Default.GuiScale = GuiScale.SelectedIndex + 1;
            
            if (preResolutionX != GameSettings.Default.ResolutionX || 
                preResolutionY != GameSettings.Default.ResolutionY ||
                preFullscreen != GameSettings.Default.Fullscreen)
            {
                StateManager.Game.Graphics.PreferredBackBufferWidth = GameSettings.Default.ResolutionX;
                StateManager.Game.Graphics.PreferredBackBufferHeight = GameSettings.Default.ResolutionY;
                StateManager.Game.Graphics.IsFullScreen = GameSettings.Default.Fullscreen;

                try
                {
                    StateManager.Game.Graphics.ApplyChanges();
                }
                catch (NoSuitableGraphicsDeviceException)
                {
                    GameSettings.Default.ResolutionX = preResolutionX;
                    GameSettings.Default.ResolutionY = preResolutionY;
                    GameSettings.Default.Fullscreen = preFullscreen;
                    SetBestResolution();
                    this.Fullscreen.CheckState = GameSettings.Default.Fullscreen;
                    GuiRoot.ShowPopup(new NewGui.Popup
                        {
                            Text = "Could not change display mode. Previous settings restored.",
                            TextSize = 1,
                            PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                        });
                }               
            }

            if (preResolutionX != GameSettings.Default.ResolutionX ||
                preResolutionY != GameSettings.Default.ResolutionY ||
                preGuiScale != GameSettings.Default.GuiScale)
            {
                GuiRoot.RenderData.CalculateScreenSize();
                RebuildGui();
            }

            HasChanges = false;

            GameSettings.Save();
        }

        private void LoadSettings()
        {
            // Set all the widget states based on game settings.

            // Gameplay settings
            this.MoveSpeed.ScrollPosition = GameSettings.Default.CameraScrollSpeed;
            this.ZoomSpeed.ScrollPosition = GameSettings.Default.CameraZoomSpeed;
            this.EdgeScrolling.CheckState = GameSettings.Default.EnableEdgeScroll;
            this.FogOfWar.CheckState = GameSettings.Default.FogofWar;
            this.InvertZoom.CheckState = GameSettings.Default.InvertZoom;
            this.PlayIntro.CheckState = GameSettings.Default.DisplayIntro;

            // Audio settings
            this.MasterVolume.ScrollPosition = GameSettings.Default.MasterVolume;
            this.SFXVolume.ScrollPosition = GameSettings.Default.SoundEffectVolume;
            this.MusicVolume.ScrollPosition = GameSettings.Default.MusicVolume;

            // Graphics settings
            this.EasyGraphicsSetting.SelectedIndex = 5;
            SetBestResolution();
            this.Fullscreen.CheckState = GameSettings.Default.Fullscreen;
            this.ChunkDrawDistance.ScrollPosition = GameSettings.Default.ChunkDrawDistance - 1.0f;
            this.VertexCullDistance.ScrollPosition = GameSettings.Default.VertexCullDistance - 0.1f;
            //this.GenerateDistance.ScrollPosition = GameSettings.Default.ChunkGenerateDistance - 1.0f;
            this.Glow.CheckState = GameSettings.Default.EnableGlow;
            
            var antialiasingIndex = 0;
            foreach (var option in AntialiasingOptions)
            {
                if (option.Value == GameSettings.Default.AntiAliasing)
                    this.Antialiasing.SelectedIndex = antialiasingIndex;
                antialiasingIndex += 1;
            }

            this.ReflectTerrain.CheckState = GameSettings.Default.DrawChunksReflected;
            this.ReflectEntities.CheckState = GameSettings.Default.DrawEntityReflected;
            this.Sunlight.CheckState = GameSettings.Default.CalculateSunlight;
            this.AmbientOcclusion.CheckState = GameSettings.Default.AmbientOcclusion;
            this.Ramps.CheckState = GameSettings.Default.CalculateRamps;
            this.CursorLight.CheckState = GameSettings.Default.CursorLightEnabled;
            this.EntityLight.CheckState = GameSettings.Default.EntityLighting;
            this.SelfIllumination.CheckState = GameSettings.Default.SelfIlluminationEnabled;
            this.ParticlePhysics.CheckState = GameSettings.Default.ParticlePhysics;
            this.Motes.CheckState = GameSettings.Default.GrassMotes;
            this.NumMotes.ScrollPosition = GameSettings.Default.NumMotes - 100;
            this.LightMap.CheckState = GameSettings.Default.UseLightmaps;
            this.DynamicShadows.CheckState = GameSettings.Default.UseDynamicShadows;

            GuiScale.SelectedIndex = GameSettings.Default.GuiScale - 1;

            HasChanges = false;
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToGameTime());
            SoundManager.Update(gameTime, null, null);
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}