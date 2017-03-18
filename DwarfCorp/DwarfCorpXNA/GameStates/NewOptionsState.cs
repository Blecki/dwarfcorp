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
        private HorizontalFloatSlider MasterVolume;
        private HorizontalFloatSlider SFXVolume;
        private HorizontalFloatSlider MusicVolume;
        private Gum.Widgets.ComboBox Resolution;
        private CheckBox Fullscreen;
        private HorizontalFloatSlider ChunkDrawDistance;
        private HorizontalFloatSlider VertexCullDistance;
        private HorizontalFloatSlider GenerateDistance;
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

        public NewOptionsState(DwarfGame Game, GameStateManager StateManager) :
            base(Game, "NewOptionsState", StateManager)
        { }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            // Setup antialiasing options.
            AntialiasingOptions = new Dictionary<string, int>();
            AntialiasingOptions.Add("NONE", 0);
            AntialiasingOptions.Add("FXAA", -1);
            AntialiasingOptions.Add("2x MSAA", 2);
            AntialiasingOptions.Add("4x MSAA", 4);
            AntialiasingOptions.Add("16x MSAA", 16);

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
            // Create and initialize GUI framework.
            GuiRoot = new Gum.Root(Gum.Root.MinimumSize, DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

            // CONSTRUCT GUI HERE...
            MainPanel = GuiRoot.RootItem.AddChild(new Gum.Widget
                {
                    Rect = GuiRoot.VirtualScreen,
                    Padding = new Margin(4,4,4,4),
                    Transparent = true
                });

            MainPanel.AddChild(new Widget
            {
                Text = "CLOSE",
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
                                OkayText = "YES",
                                CancelText = "NO",
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

            MainPanel.AddChild(new Widget
            {
                Text = "APPLY",
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
                TextSize = 2,
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
        }

        private Widget LabelAndDockWidget(string Label, Widget Widget)
        {
            var r = GuiRoot.ConstructWidget(new Widget
            {
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(0,0,4,4)
            });

            r.AddChild(new Widget
            {
                Text = Label,
                AutoLayout = AutoLayout.DockLeft
            });

            Widget.AutoLayout = AutoLayout.DockFill;
            r.AddChild(Widget);

            return r;
        }

        private void CreateGameplayTab()
        {
            var panel = TabPanel.AddTab("GAMEPLAY", new Widget
                {
                    Border = "border-thin",
                    Padding = new Margin(4,4,0,0)
                });

            // Todo: Display actual value beside slider.
            MoveSpeed = panel.AddChild(LabelAndDockWidget("Camera Move Speed", new HorizontalFloatSlider
                {
                    ScrollArea = 20,
                    OnScroll = OnItemChanged
                })).GetChild(1) as HorizontalFloatSlider;

            ZoomSpeed = panel.AddChild(LabelAndDockWidget("Camera Zoom Speed", new HorizontalFloatSlider
            {
                ScrollArea = 2,
                OnScroll = OnItemChanged
            })).GetChild(1) as HorizontalFloatSlider;

            InvertZoom = panel.AddChild(new CheckBox
                {
                    Text = "Invert Zoom",
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop
                }) as CheckBox;

            EdgeScrolling = panel.AddChild(new CheckBox
            {
                Text = "Edge Scrolling",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            PlayIntro = panel.AddChild(new CheckBox
            {
                Text = "Play Intro",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            FogOfWar = panel.AddChild(new CheckBox
            {
                Text = "Fog Of War",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;
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
                OnScroll = OnItemChanged
            })).GetChild(1) as HorizontalFloatSlider;

            SFXVolume = panel.AddChild(LabelAndDockWidget("SFX Volume", new HorizontalFloatSlider
            {
                ScrollArea = 1.0f,
                OnScroll = OnItemChanged
            })).GetChild(1) as HorizontalFloatSlider;

            MusicVolume = panel.AddChild(LabelAndDockWidget("Music Volume", new HorizontalFloatSlider
            {
                ScrollArea = 1.0f,
                OnScroll = OnItemChanged
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

            panel.AddChild(new Widget
                {
                    Text = "NON-FUNCTIONAL UNTIL INPUT SYSTEM REDONE",
                    AutoLayout = AutoLayout.DockTop
                });

            foreach (var binding in DwarfGame.GumInput.EnumerateBindableActions())
            {
                // Todo: Columns?

                var entryPanel = panel.AddChild(new Widget
                    {
                        MinimumSize = new Point(0, 20),
                        AutoLayout = AutoLayout.DockTop
                    });

                entryPanel.AddChild(new Widget
                    {
                        Text = binding.Key,
                        AutoLayout = AutoLayout.DockLeft
                    });

                // Todo: Editable key field.

            }
        }

        private void CreateGraphicsTab()
        {
            var panel = TabPanel.AddTab("GRAPHICS", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4,4,4,4)
            });

            var leftPanel = panel.AddChild(new Widget
            {
                MinimumSize = new Point(GuiRoot.VirtualScreen.Width / 2 - 4, 0),
                AutoLayout = AutoLayout.DockLeft,
                Padding = new Margin(2,2,2,2)
            });

            var rightPanel = panel.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Padding = new Margin(2,2,2,2)
            });

            Resolution = leftPanel.AddChild(LabelAndDockWidget("Resolution", new Gum.Widgets.ComboBox
                {
                    Items = DisplayModes.Where(dm => dm.Value.Width >= 1024 && dm.Value.Height >= 768).Select(dm => dm.Key).ToList(),
                    OnSelectedIndexChanged = OnItemChanged,
                    Border = "border-thin"
                })).GetChild(1) as Gum.Widgets.ComboBox;

            Fullscreen = leftPanel.AddChild(new CheckBox
                {
                    Text = "Fullscreen",
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop
                }) as CheckBox;

            ChunkDrawDistance = leftPanel.AddChild(LabelAndDockWidget("Chunk Draw Distance", new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnScroll = OnItemChanged
            })).GetChild(1) as HorizontalFloatSlider;

            VertexCullDistance = leftPanel.AddChild(LabelAndDockWidget("Vertex Cull Distance",
                new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnScroll = OnItemChanged
            })).GetChild(1) as HorizontalFloatSlider;

            GenerateDistance = leftPanel.AddChild(LabelAndDockWidget("Generate Distance",
                new HorizontalFloatSlider
                {
                    ScrollArea = 1000f,
                    OnScroll = OnItemChanged
                })).GetChild(1) as HorizontalFloatSlider;

            Glow = leftPanel.AddChild(new CheckBox
            {
                Text = "Glow",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            Antialiasing = rightPanel.AddChild(LabelAndDockWidget("Antialiasing", new Gum.Widgets.ComboBox
            {
                Items = AntialiasingOptions.Select(o => o.Key).ToList(),
                OnSelectedIndexChanged = OnItemChanged,
                Border = "border-thin"
            })).GetChild(1) as Gum.Widgets.ComboBox;

            ReflectTerrain = rightPanel.AddChild(new CheckBox
            {
                Text = "Reflect Terrain",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            ReflectEntities = rightPanel.AddChild(new CheckBox
            {
                Text = "Reflect Entities",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            Sunlight = rightPanel.AddChild(new CheckBox
            {
                Text = "Sunlight",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            AmbientOcclusion = rightPanel.AddChild(new CheckBox
            {
                Text = "Ambient Occlusion",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            Ramps = leftPanel.AddChild(new CheckBox
            {
                Text = "Ramps",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            CursorLight = rightPanel.AddChild(new CheckBox
            {
                Text = "Cursor Light",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            EntityLight = rightPanel.AddChild(new CheckBox
            {
                Text = "Entity Light",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            SelfIllumination = rightPanel.AddChild(new CheckBox
            {
                Text = "Ore Glow",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            ParticlePhysics = leftPanel.AddChild(new CheckBox
            {
                Text = "Particle Physics",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            Motes = leftPanel.AddChild(new CheckBox
            {
                Text = "Motes",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            NumMotes = leftPanel.AddChild(LabelAndDockWidget("Number of Motes",
                 new HorizontalFloatSlider
                 {
                     ScrollArea = 2048 - 100,
                     OnScroll = OnItemChanged
                 })).GetChild(1) as HorizontalFloatSlider;

            LightMap = leftPanel.AddChild(new CheckBox
            {
                Text = "Light Maps",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            DynamicShadows = leftPanel.AddChild(new CheckBox
            {
                Text = "Dynamic Shadows",
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

        }

        private void OnItemChanged(Gum.Widget Sender)
        {
            HasChanges = true;
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
            GameSettings.Default.MusicVolume = this.SFXVolume.ScrollPosition;

            // Graphics settings
            var preResolutionX = GameSettings.Default.ResolutionX;
            var preResolutionY = GameSettings.Default.ResolutionY;
            var preFullscreen = GameSettings.Default.Fullscreen;

            var newDisplayMode = DisplayModes[this.Resolution.SelectedItem];
            GameSettings.Default.ResolutionX = newDisplayMode.Width;
            GameSettings.Default.ResolutionY = newDisplayMode.Height;

            GameSettings.Default.Fullscreen = this.Fullscreen.CheckState;
            GameSettings.Default.ChunkDrawDistance = this.ChunkDrawDistance.ScrollPosition + 1.0f;
            GameSettings.Default.VertexCullDistance = this.VertexCullDistance.ScrollPosition + 0.1f;
            GameSettings.Default.ChunkGenerateDistance = this.GenerateDistance.ScrollPosition + 1.0f;
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
                    RebuildGui();
                }
                catch (NoSuitableGraphicsDeviceException)
                {
                    GameSettings.Default.ResolutionX = preResolutionX;
                    GameSettings.Default.ResolutionY = preResolutionY;
                    GameSettings.Default.Fullscreen = preFullscreen;
                    this.Resolution.SelectedIndex = this.Resolution.Items.IndexOf(string.Format("{0} x {1}",
                        GameSettings.Default.ResolutionX, GameSettings.Default.ResolutionY));
                    this.Fullscreen.CheckState = GameSettings.Default.Fullscreen;
                    GuiRoot.ShowPopup(new NewGui.Popup
                        {
                            Text = "Could not change display mode. Previous settings restored.",
                            TextSize = 2,
                            PopupDestructionType = PopupDestructionType.DestroyOnOffClick
                        });
                }
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
            this.Resolution.SelectedIndex = this.Resolution.Items.IndexOf(string.Format("{0} x {1}",
                GameSettings.Default.ResolutionX, GameSettings.Default.ResolutionY));
            this.Fullscreen.CheckState = GameSettings.Default.Fullscreen;
            this.ChunkDrawDistance.ScrollPosition = GameSettings.Default.ChunkDrawDistance - 1.0f;
            this.VertexCullDistance.ScrollPosition = GameSettings.Default.VertexCullDistance - 0.1f;
            this.GenerateDistance.ScrollPosition = GameSettings.Default.ChunkGenerateDistance - 1.0f;
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
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}