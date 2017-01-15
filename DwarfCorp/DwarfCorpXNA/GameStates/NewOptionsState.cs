using System.Collections.Generic;
using System.Linq;
using DwarfCorpCore;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum;
using Gum.Widgets;

namespace DwarfCorp.GameStates
{
    public class NewOptionsState : GameState
    {
        private Gum.Root GuiRoot;
        private bool HasChanges = false;

        private Dictionary<string, int> AntialiasingOptions;
        
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

        private Gem.Input Input; // Todo: This needs to be shared with the play state somehow so the key
        // bindings actually work.

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInput.GetInputQueue();
            Input = new Gem.Input(DwarfGame.GumInput);

            // Dummy key binding for testing.
            Input.AddAction("TEST", Gem.Input.KeyBindingType.Pressed);

            // Setup antialiasing options.
            AntialiasingOptions = new Dictionary<string, int>();
            AntialiasingOptions.Add("NONE", 0);
            AntialiasingOptions.Add("FXAA", -1);
            AntialiasingOptions.Add("2x MSAA", 2);
            AntialiasingOptions.Add("4x MSAA", 4);
            AntialiasingOptions.Add("16x MSAA", 16);

            // Create and initialize GUI framework.
            GuiRoot = new Gum.Root(new Point(640, 480), DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

            // CONSTRUCT GUI HERE...
            MainPanel = GuiRoot.RootItem.AddChild(new Gum.Widget
                {
                    Rect = GuiRoot.VirtualScreen,
                    Background = new TileReference("basic", 0),
                    Padding = new Margin(4,4,4,4)
                });

            MainPanel.AddChild(new Widget
            {
                Text = "CLOSE",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                TextSize = 2,
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
                                        StateManager.PopState();
                                    }
                            };
                        GuiRoot.ShowPopup(confirm, false);
                    }
                    else
                        StateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            MainPanel.AddChild(new Widget
            {
                Text = "APPLY",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                TextSize = 2,
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
                TextSize = 4,
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

            // Must be true or Render will not be called.
            IsInitialized = true;

            base.OnEnter();
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
                TextSize = 2,
                AutoLayout = AutoLayout.DockLeft
            });

            Widget.AutoLayout = AutoLayout.DockFill;
            r.AddChild(Widget);

            return r;
        }

        private void BeforeTextChangeInteger(Widget Sender, Gum.Widgets.EditableTextField.BeforeTextChangeEventArgs args)
        {
            Sender.TextColor = new Vector4(0, 0, 0, 1);
            int value = 0;
            if (!int.TryParse(args.NewText, out value))
                Sender.TextColor = new Vector4(1, 0, 0, 1);
        }

        private void BeforeTextChangeFloat(Widget Sender, Gum.Widgets.EditableTextField.BeforeTextChangeEventArgs args)
        {
            Sender.TextColor = new Vector4(0, 0, 0, 1);
            float value = 0;
            if (!float.TryParse(args.NewText, out value))
                Sender.TextColor = new Vector4(1, 0, 0, 1);
        }

        private System.Action<Widget, EditableTextField.BeforeTextChangeEventArgs> 
            RangedBeforeTextChangeFloat(float Min, float Max)
        {
            return (sender, args) =>
                {
                    sender.TextColor = new Vector4(0, 0, 0, 1);
                    float value = 0;
                    if (!float.TryParse(args.NewText, out value))
                        sender.TextColor = new Vector4(1, 0, 0, 1);
                    else
                    {
                        if (value < Min) sender.TextColor = new Vector4(1, 0, 0, 1);
                        if (value > Max) sender.TextColor = new Vector4(1, 0, 0, 1);
                    }
                };
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
                    TextSize = 2,
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop
                }) as CheckBox;

            EdgeScrolling = panel.AddChild(new CheckBox
            {
                Text = "Edge Scrolling",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            PlayIntro = panel.AddChild(new CheckBox
            {
                Text = "Play Intro",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            FogOfWar = panel.AddChild(new CheckBox
            {
                Text = "Fog Of War",
                TextSize = 2,
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
                    TextSize = 2,
                    AutoLayout = AutoLayout.DockTop
                });

            foreach (var binding in Input.EnumerateBindableActions())
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
                        TextSize = 2,
                        AutoLayout = AutoLayout.DockLeft
                    });

                // Todo: Editable key field.

            }
        }

        private void CreateGraphicsTab()
        {
            var panel = TabPanel.AddTab("KEYS", new Widget
            {
                Border = "border-thin",
                Padding = new Margin(4, 4, 0, 0)
            });

            Resolution = panel.AddChild(LabelAndDockWidget("Resolution", new Gum.Widgets.ComboBox
                {
                    Items = GraphicsAdapter.DefaultAdapter.SupportedDisplayModes.Select(mode =>
                    {
                        return mode.ToString();
                    }).ToList(),
                    TextSize = 2,
                    OnSelectedIndexChanged = OnItemChanged
                })).GetChild(1) as Gum.Widgets.ComboBox;

            Fullscreen = panel.AddChild(new CheckBox
                {
                    Text = "Fullscreen",
                    TextSize = 2,
                    OnCheckStateChange = OnItemChanged,
                    AutoLayout = AutoLayout.DockTop
                }) as CheckBox;

            ChunkDrawDistance = panel.AddChild(LabelAndDockWidget("Chunk Draw Distance", new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnScroll = OnItemChanged
            })).GetChild(1) as HorizontalFloatSlider;

            VertexCullDistance = panel.AddChild(LabelAndDockWidget("Vertex Cull Distance",
                new HorizontalFloatSlider
            {
                ScrollArea = 1000f,
                OnScroll = OnItemChanged
            })).GetChild(1) as HorizontalFloatSlider;

            GenerateDistance = panel.AddChild(LabelAndDockWidget("Generate Distance",
                new HorizontalFloatSlider
                {
                    ScrollArea = 1000f,
                    OnScroll = OnItemChanged
                })).GetChild(1) as HorizontalFloatSlider;

            Glow = panel.AddChild(new CheckBox
            {
                Text = "Glow",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            Antialiasing = panel.AddChild(LabelAndDockWidget("Antialiasing", new Gum.Widgets.ComboBox
            {
                Items = AntialiasingOptions.Select(o => o.Key).ToList(),
                TextSize = 2,
                OnSelectedIndexChanged = OnItemChanged
            })).GetChild(1) as Gum.Widgets.ComboBox;

            ReflectTerrain = panel.AddChild(new CheckBox
            {
                Text = "Reflect Terrain",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            ReflectEntities = panel.AddChild(new CheckBox
            {
                Text = "Reflect Entities",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            Sunlight = panel.AddChild(new CheckBox
            {
                Text = "Sunlight",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            AmbientOcclusion = panel.AddChild(new CheckBox
            {
                Text = "Ambient Occlusion",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            Ramps = panel.AddChild(new CheckBox
            {
                Text = "Ramps",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            CursorLight = panel.AddChild(new CheckBox
            {
                Text = "Cursor Light",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            EntityLight = panel.AddChild(new CheckBox
            {
                Text = "Entity Light",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            SelfIllumination = panel.AddChild(new CheckBox
            {
                Text = "Ore Glow",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            ParticlePhysics = panel.AddChild(new CheckBox
            {
                Text = "Particle Physics",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            Motes = panel.AddChild(new CheckBox
            {
                Text = "Motes",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            NumMotes = panel.AddChild(LabelAndDockWidget("Number of Motes",
                 new HorizontalFloatSlider
                 {
                     ScrollArea = 2048 - 100,
                     OnScroll = OnItemChanged
                 })).GetChild(1) as HorizontalFloatSlider;

            LightMap = panel.AddChild(new CheckBox
            {
                Text = "Light Maps",
                TextSize = 2,
                OnCheckStateChange = OnItemChanged,
                AutoLayout = AutoLayout.DockTop
            }) as CheckBox;

            DynamicShadows = panel.AddChild(new CheckBox
            {
                Text = "Dynamic Shadows",
                TextSize = 2,
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



            HasChanges = false;
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

            HasChanges = false;
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInput.GetInputQueue())
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