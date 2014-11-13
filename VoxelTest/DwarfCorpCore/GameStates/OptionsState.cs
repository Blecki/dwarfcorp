using System.Collections.Generic;
using System.Linq;
using DwarfCorpCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This game state allows the player to modify game options (stored in the GameSettings file).
    /// </summary>
    public class OptionsState : GameState
    {
        public DwarfGUI GUI { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public Drawer2D Drawer { get; set; }
        public Panel MainWindow { get; set; }
        public int EdgePadding { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }
        public Dictionary<string, GroupBox> Categories { get; set; }
        public Dictionary<string, DisplayMode> DisplayModes { get; set; }
        public Dictionary<string, int> AAModes { get; set; }
        public GroupBox CurrentBox { get; set; }
        public TabSelector TabSelector { get; set; }

        public OptionsState(DwarfGame game, GameStateManager stateManager) :
            base(game, "OptionsState", stateManager)
        {
            EdgePadding = 32;
            Input = new InputManager();
            Categories = new Dictionary<string, GroupBox>();
            DisplayModes = new Dictionary<string, DisplayMode>();
            AAModes = new Dictionary<string, int>();
            AAModes["None"] = 0;
            AAModes["2"] = 2;
            AAModes["4"] = 4;
            AAModes["16"] = 16;
        }

        public override void OnEnter()
        {
            DefaultFont = Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            GUI = new DwarfGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);
            IsInitialized = true;
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            MainWindow = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2)
            };
            Layout = new GridLayout(GUI, MainWindow, 10, 6);

            Label label = new Label(GUI, Layout, "Options", GUI.TitleFont);
            Layout.SetComponentPosition(label, 0, 0, 1, 1);

            TabSelector = new TabSelector(GUI, Layout, 6);
            Layout.SetComponentPosition(TabSelector, 0, 1, 6, 8);

            DwarfCorp.TabSelector.Tab graphicsTab = TabSelector.AddTab("Graphics");

            GroupBox graphicsBox = new GroupBox(GUI, graphicsTab, "Graphics")
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit
            };

            Categories["Graphics"] = graphicsBox;
            CurrentBox = graphicsBox;

            GridLayout graphicsLayout = new GridLayout(GUI, graphicsBox, 6, 5);


            Label resolutionLabel = new Label(GUI, graphicsLayout, "Resolution", GUI.DefaultFont)
            {
                Alignment = Drawer2D.Alignment.Right
            };

            ComboBox resolutionBox = new ComboBox(GUI, graphicsLayout)
            {
                ToolTip = "Sets the size of the screen.\nSmaller for higher framerates."
            };

            foreach(DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if(mode.Format != SurfaceFormat.Color)
                {
                    continue;
                }

                string s = mode.Width + " x " + mode.Height;
                DisplayModes[s] = mode;
                if(mode.Width == GameSettings.Default.ResolutionX && mode.Height == GameSettings.Default.ResolutionY)
                {
                    resolutionBox.AddValue(s);
                    resolutionBox.CurrentValue = s;
                }
                else
                {
                    resolutionBox.AddValue(s);
                }
            }

            graphicsLayout.SetComponentPosition(resolutionLabel, 0, 0, 1, 1);
            graphicsLayout.SetComponentPosition(resolutionBox, 1, 0, 1, 1);


            resolutionBox.OnSelectionModified += resolutionBox_OnSelectionModified;

            Checkbox fullscreenCheck = new Checkbox(GUI, graphicsLayout, "Fullscreen", GUI.DefaultFont, GameSettings.Default.Fullscreen)
            {
                ToolTip = "If this is checked, the game takes up the whole screen."
            };

            graphicsLayout.SetComponentPosition(fullscreenCheck, 0, 1, 1, 1);

            fullscreenCheck.OnCheckModified += fullscreenCheck_OnClicked;


            Label drawDistance = new Label(GUI, graphicsLayout, "Draw Distance", GUI.DefaultFont);
            Slider chunkDrawSlider = new Slider(GUI, graphicsLayout, "", GameSettings.Default.ChunkDrawDistance, 1, 1000, Slider.SliderMode.Integer)
            {
                ToolTip = "Maximum distance at which terrain will be drawn\nSmaller for faster."
            };


            graphicsLayout.SetComponentPosition(drawDistance, 0, 2, 1, 1);
            graphicsLayout.SetComponentPosition(chunkDrawSlider, 1, 2, 1, 1);
            chunkDrawSlider.OnValueModified += ChunkDrawSlider_OnValueModified;

            Label cullDistance = new Label(GUI, graphicsLayout, "Cull Distance", GUI.DefaultFont);
            Slider cullSlider = new Slider(GUI, graphicsLayout, "", GameSettings.Default.VertexCullDistance, 0.1f, 1000, Slider.SliderMode.Integer)
            {
                ToolTip = "Maximum distance at which anything will be drawn\n Smaller for faster."
            };

            cullSlider.OnValueModified += CullSlider_OnValueModified;

            graphicsLayout.SetComponentPosition(cullDistance, 0, 3, 1, 1);
            graphicsLayout.SetComponentPosition(cullSlider, 1, 3, 1, 1);

            Label generateDistance = new Label(GUI, graphicsLayout, "Generate Distance", GUI.DefaultFont);
            Slider generateSlider = new Slider(GUI, graphicsLayout, "", GameSettings.Default.ChunkGenerateDistance, 1, 1000, Slider.SliderMode.Integer)
            {
                ToolTip = "Maximum distance at which terrain will be generated."
            };

            generateSlider.OnValueModified += GenerateSlider_OnValueModified;

            graphicsLayout.SetComponentPosition(generateDistance, 0, 4, 1, 1);
            graphicsLayout.SetComponentPosition(generateSlider, 1, 4, 1, 1);

            Checkbox glowBox = new Checkbox(GUI, graphicsLayout, "Enable Glow", GUI.DefaultFont, GameSettings.Default.EnableGlow)
            {
                ToolTip = "When checked, there will be a fullscreen glow effect."
            };

            graphicsLayout.SetComponentPosition(glowBox, 1, 1, 1, 1);
            glowBox.OnCheckModified += glowBox_OnCheckModified;

            Label aaLabel = new Label(GUI, graphicsLayout, "Antialiasing", GUI.DefaultFont)
            {
                Alignment = Drawer2D.Alignment.Right
            };

            ComboBox aaBox = new ComboBox(GUI, graphicsLayout)
            {
                ToolTip = "Determines how much antialiasing (smoothing) there is.\nHigher means more smooth, but is slower"
            };
            aaBox.AddValue("None");
            aaBox.AddValue("2");
            aaBox.AddValue("4");
            aaBox.AddValue("16");

            foreach(string s in AAModes.Keys.Where(s => AAModes[s] == GameSettings.Default.AntiAliasing))
            {
                aaBox.CurrentValue = s;
            }


            aaBox.OnSelectionModified += AABox_OnSelectionModified;

            graphicsLayout.SetComponentPosition(aaLabel, 2, 0, 1, 1);
            graphicsLayout.SetComponentPosition(aaBox, 3, 0, 1, 1);


            Checkbox reflectTerrainBox = new Checkbox(GUI, graphicsLayout, "Reflect Chunks", GUI.DefaultFont, GameSettings.Default.DrawChunksReflected)
            {
                ToolTip = "When checked, water will reflect terrain."
            };
            reflectTerrainBox.OnCheckModified += reflectTerrainBox_OnCheckModified;
            graphicsLayout.SetComponentPosition(reflectTerrainBox, 2, 1, 1, 1);

            Checkbox refractTerrainBox = new Checkbox(GUI, graphicsLayout, "Refract Chunks", GUI.DefaultFont, GameSettings.Default.DrawChunksRefracted)
            {
                ToolTip = "When checked, water will refract terrain."
            };
            refractTerrainBox.OnCheckModified += refractTerrainBox_OnCheckModified;
            graphicsLayout.SetComponentPosition(refractTerrainBox, 2, 2, 1, 1);

            Checkbox reflectEntities = new Checkbox(GUI, graphicsLayout, "Reflect Entities", GUI.DefaultFont, GameSettings.Default.DrawEntityReflected)
            {
                ToolTip = "When checked, water will reflect tress, dwarves, etc.."
            };
            reflectEntities.OnCheckModified += reflectEntities_OnCheckModified;
            graphicsLayout.SetComponentPosition(reflectEntities, 3, 1, 1, 1);

            Checkbox refractEntities = new Checkbox(GUI, graphicsLayout, "Refract Entities", GUI.DefaultFont, GameSettings.Default.DrawEntityReflected)
            {
                ToolTip = "When checked, water will reflect tress, dwarves, etc.."
            };
            refractEntities.OnCheckModified += refractEntities_OnCheckModified;
            graphicsLayout.SetComponentPosition(refractEntities, 3, 2, 1, 1);

            Checkbox sunlight = new Checkbox(GUI, graphicsLayout, "Sunlight", GUI.DefaultFont, GameSettings.Default.CalculateSunlight)
            {
                ToolTip = "When checked, terrain will be lit/shadowed by the sun."
            };
            sunlight.OnCheckModified += sunlight_OnCheckModified;
            graphicsLayout.SetComponentPosition(sunlight, 2, 3, 1, 1);

            Checkbox ao = new Checkbox(GUI, graphicsLayout, "Ambient Occlusion", GUI.DefaultFont, GameSettings.Default.AmbientOcclusion)
            {
                ToolTip = "When checked, terrain will smooth shading effects."
            };
            ao.OnCheckModified += AO_OnCheckModified;
            graphicsLayout.SetComponentPosition(ao, 3, 3, 1, 1);

            Checkbox ramps = new Checkbox(GUI, graphicsLayout, "Ramps", GUI.DefaultFont, GameSettings.Default.CalculateRamps)
            {
                ToolTip = "When checked, some terrain will have smooth ramps."
            };

            ramps.OnCheckModified += ramps_OnCheckModified;
            graphicsLayout.SetComponentPosition(ramps, 2, 4, 1, 1);

            Checkbox cursorLight = new Checkbox(GUI, graphicsLayout, "Cursor Light", GUI.DefaultFont, GameSettings.Default.CursorLightEnabled)
            {
                ToolTip = "When checked, a light will follow the player cursor."
            };

            cursorLight.OnCheckModified += cursorLight_OnCheckModified;
            graphicsLayout.SetComponentPosition(cursorLight, 2, 5, 1, 1);

            Checkbox entityLight = new Checkbox(GUI, graphicsLayout, "Entity Lighting", GUI.DefaultFont, GameSettings.Default.EntityLighting)
            {
                ToolTip = "When checked, dwarves, objects, etc. will be lit\nby the sun, lamps, etc."
            };

            entityLight.OnCheckModified += entityLight_OnCheckModified;
            graphicsLayout.SetComponentPosition(entityLight, 3, 4, 1, 1);

            Checkbox selfIllum = new Checkbox(GUI, graphicsLayout, "Ore Glow", GUI.DefaultFont, GameSettings.Default.SelfIlluminationEnabled)
            {
                ToolTip = "When checked, some terrain elements will glow."
            };

            selfIllum.OnCheckModified += selfIllum_OnCheckModified;
            graphicsLayout.SetComponentPosition(selfIllum, 3, 5, 1, 1);

            Checkbox particlePhysics = new Checkbox(GUI, graphicsLayout, "Particle Body", GUI.DefaultFont, GameSettings.Default.ParticlePhysics)
            {
                ToolTip = "When checked, some particles will bounce off terrain"
            };

            particlePhysics.OnCheckModified += particlePhysics_OnCheckModified;
            graphicsLayout.SetComponentPosition(particlePhysics, 0, 5, 1, 1);



            TabSelector.Tab graphics2Tab = TabSelector.AddTab("Graphics II");
            GroupBox graphicsBox2 = new GroupBox(GUI, graphics2Tab, "Graphics II")
            {
                HeightSizeMode = GUIComponent.SizeMode.Fit,
                WidthSizeMode = GUIComponent.SizeMode.Fit
            };
            Categories["Graphics II"] = graphicsBox2;
            GridLayout graphicsLayout2 = new GridLayout(GUI, graphicsBox2, 6, 5);

            Checkbox moteBox = new Checkbox(GUI, graphicsLayout2, "Generate Motes", GUI.DefaultFont, GameSettings.Default.GrassMotes)
            {
                ToolTip = "When checked, trees, grass, etc. will be visible."
            };

            moteBox.OnCheckModified += MoteBox_OnCheckModified;
            graphicsLayout2.SetComponentPosition(moteBox, 1, 2, 1, 1);

            Label numMotes = new Label(GUI, graphicsLayout2, "Num Motes", GUI.DefaultFont);
            Slider motesSlider = new Slider(GUI, graphicsLayout2, "", (int) (GameSettings.Default.NumMotes * 100), 0, 1000, Slider.SliderMode.Integer)
            {
                ToolTip = "Determines the maximum amount of trees/grass will be visible."
            };



            graphicsLayout2.SetComponentPosition(numMotes, 0, 1, 1, 1);
            graphicsLayout2.SetComponentPosition(motesSlider, 1, 1, 1, 1);
            motesSlider.OnValueModified += MotesSlider_OnValueModified;
            TabSelector.Tab gameplayTab = TabSelector.AddTab("Gameplay");
            GroupBox gameplayBox = new GroupBox(GUI, gameplayTab, "Gameplay")
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit
            };
            Categories["Gameplay"] = gameplayBox;

            GridLayout gameplayLayout = new GridLayout(GUI, gameplayBox, 6, 5);

            Label moveSpeedLabel = new Label(GUI, gameplayLayout, "Camera Move Speed", GUI.DefaultFont);
            Slider moveSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.CameraScrollSpeed, 0.0f, 20.0f, Slider.SliderMode.Float)
            {
                ToolTip = "Determines how fast the camera will move when keys are pressed."
            };

            gameplayLayout.SetComponentPosition(moveSpeedLabel, 0, 0, 1, 1);
            gameplayLayout.SetComponentPosition(moveSlider, 1, 0, 1, 1);
            moveSlider.OnValueModified += MoveSlider_OnValueModified;

            Label zoomSpeedLabel = new Label(GUI, gameplayLayout, "Zoom Speed", GUI.DefaultFont);
            Slider zoomSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.CameraZoomSpeed, 0.0f, 2.0f, Slider.SliderMode.Float)
            {
                ToolTip = "Determines how fast the camera will go\nup and down with the scroll wheel."
            };

            gameplayLayout.SetComponentPosition(zoomSpeedLabel, 0, 1, 1, 1);
            gameplayLayout.SetComponentPosition(zoomSlider, 1, 1, 1, 1);
            zoomSlider.OnValueModified += ZoomSlider_OnValueModified;

            Checkbox invertZoomBox = new Checkbox(GUI, gameplayLayout, "Invert Zoom", GUI.DefaultFont, GameSettings.Default.InvertZoom)
            {
                ToolTip = "When checked, the scroll wheel is reveresed\nfor zooming."
            };

            gameplayLayout.SetComponentPosition(invertZoomBox, 2, 1, 1, 1);
            invertZoomBox.OnCheckModified += InvertZoomBox_OnCheckModified;


            Checkbox edgeScrollBox = new Checkbox(GUI, gameplayLayout, "Edge Scrolling", GUI.DefaultFont, GameSettings.Default.EnableEdgeScroll)
            {
                ToolTip = "When checked, the camera will scroll\nwhen the cursor is at the edge of the screen."
            };

            gameplayLayout.SetComponentPosition(edgeScrollBox, 0, 2, 1, 1);
            edgeScrollBox.OnCheckModified += EdgeScrollBox_OnCheckModified;

            Checkbox introBox = new Checkbox(GUI, gameplayLayout, "Play Intro", GUI.DefaultFont, GameSettings.Default.DisplayIntro)
            {
                ToolTip = "When checked, the intro will be played when the game starts"
            };

            gameplayLayout.SetComponentPosition(introBox, 1, 2, 1, 1);
            introBox.OnCheckModified += IntroBox_OnCheckModified;

            /*
            Label chunkWidthLabel = new Label(GUI, gameplayLayout, "Chunk Width", GUI.DefaultFont);
            Slider chunkWidthSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.ChunkWidth, 4, 256, Slider.SliderMode.Integer)
            {
                ToolTip = "Determines the number of blocks in a chunk of terrain."
            };

            gameplayLayout.SetComponentPosition(chunkWidthLabel, 0, 3, 1, 1);
            gameplayLayout.SetComponentPosition(chunkWidthSlider, 1, 3, 1, 1);
            chunkWidthSlider.OnValueModified += ChunkWidthSlider_OnValueModified;

            Label chunkHeightLabel = new Label(GUI, gameplayLayout, "Chunk Height", GUI.DefaultFont);
            Slider chunkHeightSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.ChunkHeight, 4, 256, Slider.SliderMode.Integer)
            {
                ToolTip = "Determines the maximum depth,\nin blocks, of a chunk of terrain."
            };

            gameplayLayout.SetComponentPosition(chunkHeightLabel, 2, 3, 1, 1);
            gameplayLayout.SetComponentPosition(chunkHeightSlider, 3, 3, 1, 1);


            chunkHeightSlider.OnValueModified += ChunkHeightSlider_OnValueModified;

            Label worldWidthLabel = new Label(GUI, gameplayLayout, "World Width", GUI.DefaultFont);
            Slider worldWidthSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.WorldWidth, 4, 2048, Slider.SliderMode.Integer)
            {
                ToolTip = "Determines the size of the overworld."
            };

            gameplayLayout.SetComponentPosition(worldWidthLabel, 0, 4, 1, 1);
            gameplayLayout.SetComponentPosition(worldWidthSlider, 1, 4, 1, 1);
            worldWidthSlider.OnValueModified += WorldWidthSlider_OnValueModified;

            Label worldHeightLabel = new Label(GUI, gameplayLayout, "World Height", GUI.DefaultFont);
            Slider worldHeightSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.WorldHeight, 4, 2048, Slider.SliderMode.Integer)
            {
                ToolTip = "Determines the size of the overworld."
            };

            gameplayLayout.SetComponentPosition(worldHeightLabel, 2, 4, 1, 1);
            gameplayLayout.SetComponentPosition(worldHeightSlider, 3, 4, 1, 1);
            worldHeightSlider.OnValueModified += WorldHeightSlider_OnValueModified;


            Label worldScaleLabel = new Label(GUI, gameplayLayout, "World Scale", GUI.DefaultFont);
            Slider worldScaleSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.WorldScale, 2, 128, Slider.SliderMode.Integer)
            {
                ToolTip = "Determines the number of voxel\nper pixel of the overworld"
            };

            gameplayLayout.SetComponentPosition(worldScaleLabel, 0, 5, 1, 1);
            gameplayLayout.SetComponentPosition(worldScaleSlider, 1, 5, 1, 1);
            worldScaleSlider.OnValueModified += WorldScaleSlider_OnValueModified;
            */

            TabSelector.Tab audioTab = TabSelector.AddTab("Audio");

            GroupBox audioBox = new GroupBox(GUI, audioTab, "Audio")
            {
              WidthSizeMode = GUIComponent.SizeMode.Fit,
              HeightSizeMode = GUIComponent.SizeMode.Fit
            };
            Categories["Audio"] = audioBox;

            GridLayout audioLayout = new GridLayout(GUI, audioBox, 6, 5);

            Label masterLabel = new Label(GUI, audioLayout, "Master Volume", GUI.DefaultFont);
            Slider masterSlider = new Slider(GUI, audioLayout, "", GameSettings.Default.MasterVolume, 0.0f, 1.0f, Slider.SliderMode.Float);
            audioLayout.SetComponentPosition(masterLabel, 0, 0, 1, 1);
            audioLayout.SetComponentPosition(masterSlider, 1, 0, 1, 1);
            masterSlider.OnValueModified += MasterSlider_OnValueModified;

            Label sfxLabel = new Label(GUI, audioLayout, "SFX Volume", GUI.DefaultFont);
            Slider sfxSlider = new Slider(GUI, audioLayout, "", GameSettings.Default.SoundEffectVolume, 0.0f, 1.0f, Slider.SliderMode.Float);
            audioLayout.SetComponentPosition(sfxLabel, 0, 1, 1, 1);
            audioLayout.SetComponentPosition(sfxSlider, 1, 1, 1, 1);
            sfxSlider.OnValueModified += SFXSlider_OnValueModified;

            Label musicLabel = new Label(GUI, audioLayout, "Music Volume", GUI.DefaultFont);
            Slider musicSlider = new Slider(GUI, audioLayout, "", GameSettings.Default.MusicVolume, 0.0f, 1.0f, Slider.SliderMode.Float);
            audioLayout.SetComponentPosition(musicLabel, 0, 2, 1, 1);
            audioLayout.SetComponentPosition(musicSlider, 1, 2, 1, 1);
            musicSlider.OnValueModified += MusicSlider_OnValueModified;

            DwarfCorp.TabSelector.Tab keysTab = TabSelector.AddTab("Keys");
            GroupBox keysBox = new GroupBox(GUI, keysTab, "Keys")
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit
            };
            Categories["Keys"] = keysBox;

            KeyEditor keyeditor = new KeyEditor(GUI, keysBox, new KeyManager(), 8, 4);
            GridLayout keyLayout = new GridLayout(GUI, keysBox, 1, 1);
            keyLayout.SetComponentPosition(keyeditor, 0, 0, 1, 1);
            keyLayout.UpdateSizes();
            keyeditor.UpdateLayout();

            /*
            GroupBox customBox = new GroupBox(GUI, Layout, "Customization")
            {
                IsVisible = false
            };
            Categories["Customization"] = customBox;
            TabSelector.AddItem("Customization");

            GridLayout customBoxLayout = new GridLayout(GUI, customBox, 6, 5);

            List<string> assets = TextureManager.DefaultContent.Keys.ToList();

            AssetManager assetManager = new AssetManager(GUI, customBoxLayout, assets);
            customBoxLayout.SetComponentPosition(assetManager, 0, 0, 5, 6);
            */

            Button apply = new Button(GUI, Layout, "Apply", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Check));
            Layout.SetComponentPosition(apply, 5, 9, 1, 1);
            apply.OnClicked += apply_OnClicked;

            Button back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow));
            Layout.SetComponentPosition(back, 4, 9, 1, 1);
            back.OnClicked += back_OnClicked;

            Layout.UpdateSizes();
            TabSelector.UpdateSize();
            TabSelector.SetTab("Graphics");
            base.OnEnter();
        }

        private void InvertZoomBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.InvertZoom = arg;
        }

        private void MotesSlider_OnValueModified(float arg)
        {
            GameSettings.Default.NumMotes = arg / 100;
        }

        private void MoteBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.GrassMotes = arg;
        }

        private void particlePhysics_OnCheckModified(bool arg)
        {
            GameSettings.Default.ParticlePhysics = arg;
        }

        private void selfIllum_OnCheckModified(bool arg)
        {
            GameSettings.Default.SelfIlluminationEnabled = arg;
        }

        private void entityLight_OnCheckModified(bool arg)
        {
            GameSettings.Default.EntityLighting = arg;
        }

        private void cursorLight_OnCheckModified(bool arg)
        {
            GameSettings.Default.CursorLightEnabled = arg;
        }

        private void loadDialog_OnTextureSelected(TextureLoader.TextureFile arg)
        {
            AssetSettings.Default.TileSet = arg.File;
        }

        private void MusicSlider_OnValueModified(float arg)
        {
            GameSettings.Default.MusicVolume = arg;
        }

        private void SFXSlider_OnValueModified(float arg)
        {
            GameSettings.Default.SoundEffectVolume = arg;
        }

        private void MasterSlider_OnValueModified(float arg)
        {
            GameSettings.Default.MasterVolume = arg;
        }

        private void IntroBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.DisplayIntro = arg;
        }

        private void WorldScaleSlider_OnValueModified(float arg)
        {
            GameSettings.Default.WorldScale = (int) arg;
        }


        private void EdgeScrollBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.EnableEdgeScroll = arg;
        }

        private void ZoomSlider_OnValueModified(float arg)
        {
            GameSettings.Default.CameraZoomSpeed = arg;
        }

        private void MoveSlider_OnValueModified(float arg)
        {
            GameSettings.Default.CameraScrollSpeed = arg;
        }

        private void tabSelector_OnItemClicked()
        {
            CurrentBox.IsVisible = false;
            //CurrentBox = Categories[TabSelector.SelectedItem.Label];
            CurrentBox.IsVisible = true;
            Layout.SetComponentPosition(CurrentBox, 1, 0, 5, 8);
        }

        private void ramps_OnCheckModified(bool arg)
        {
            GameSettings.Default.CalculateRamps = arg;
        }

        private void AO_OnCheckModified(bool arg)
        {
            GameSettings.Default.AmbientOcclusion = arg;
        }

        private void sunlight_OnCheckModified(bool arg)
        {
            GameSettings.Default.CalculateSunlight = arg;
        }


        private void refractEntities_OnCheckModified(bool arg)
        {
            GameSettings.Default.DrawEntityRefracted = arg;
        }


        private void reflectEntities_OnCheckModified(bool arg)
        {
            GameSettings.Default.DrawEntityReflected = arg;
        }

        private void refractTerrainBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.DrawChunksRefracted = arg;
        }

        private void reflectTerrainBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.DrawChunksReflected = arg;
        }

        private void AABox_OnSelectionModified(string arg)
        {
            GameSettings.Default.AntiAliasing = AAModes[arg];
        }

        private void glowBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.EnableGlow = arg;
        }

        private void GenerateSlider_OnValueModified(float arg)
        {
            GameSettings.Default.ChunkGenerateDistance = arg;
        }

        private void CullSlider_OnValueModified(float arg)
        {
            GameSettings.Default.VertexCullDistance = arg;
        }

        private void ChunkDrawSlider_OnValueModified(float arg)
        {
            GameSettings.Default.ChunkDrawDistance = arg;
        }

        private void apply_OnClicked()
        {
            GameSettings.Default.Save();

            StateManager.Game.Graphics.PreferredBackBufferWidth = GameSettings.Default.ResolutionX;
            StateManager.Game.Graphics.PreferredBackBufferHeight = GameSettings.Default.ResolutionY;
            StateManager.Game.Graphics.IsFullScreen = GameSettings.Default.Fullscreen;

            try
            {
                StateManager.Game.Graphics.ApplyChanges();
            }
            catch(NoSuitableGraphicsDeviceException exception)
            {
                Dialog.Popup(GUI, "Failure!", exception.Message, Dialog.ButtonType.OK);
            }

            Dialog.Popup(GUI, "Info", "Some settings will not take effect until the game is restarted.", Dialog.ButtonType.OK);
        }

        private void fullscreenCheck_OnClicked(bool c)
        {
            GameSettings.Default.Fullscreen = c;
        }

        private void resolutionBox_OnSelectionModified(string arg)
        {
            DisplayMode mode = DisplayModes[arg];

            GameSettings.Default.ResolutionX = mode.Width;
            GameSettings.Default.ResolutionY = mode.Height;
        }

        private void back_OnClicked()
        {
            StateManager.PopState();
        }

        public override void Update(GameTime gameTime)
        {
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Input.Update();
            GUI.Update(gameTime);
            base.Update(gameTime);
        }


        private void DrawGUI(GameTime gameTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };
            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));

            DwarfGame.SpriteBatch.End();

            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
            GUI.PostRender(gameTime);
        }

        public override void Render(GameTime gameTime)
        {
            if(Transitioning == TransitionMode.Running)
            {
                Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                DrawGUI(gameTime, 0);
            }
            else if(Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }
            else if(Transitioning == TransitionMode.Exiting)
            {
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }


            base.Render(gameTime);
        }
    }

}