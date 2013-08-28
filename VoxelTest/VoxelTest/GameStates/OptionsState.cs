using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace DwarfCorp
{
    public class OptionsState : GameState
    {
        public SillyGUI GUI { get; set; }
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
        public ListSelector TabSelector { get; set; }
        
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
            DefaultFont = Game.Content.Load<SpriteFont>("Default");
            GUI = new SillyGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>("Title"),  Game.Content.Load<SpriteFont>("Small"), Input);
            IsInitialized = true;
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            MainWindow = new Panel(GUI, GUI.RootComponent);
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Layout = new GridLayout(GUI, MainWindow, 10, 6);
            
            Label label = new Label(GUI, Layout, "Options", GUI.TitleFont);
            Layout.SetComponentPosition(label, 0, 0, 1, 1);

            TabSelector = new ListSelector(GUI, MainWindow);
            TabSelector.LocalBounds = new Rectangle(10, 50, 100, 100);
            TabSelector.DrawPanel = false;
            TabSelector.AddItem("Graphics");
            TabSelector.Label = "- Categories -";
            TabSelector.OnItemClicked += new ClickedDelegate(tabSelector_OnItemClicked);

            GroupBox GraphicsBox = new GroupBox(GUI, Layout, "Graphics");
            Categories["Graphics"] = GraphicsBox;
            CurrentBox = GraphicsBox;

            GridLayout GraphicsLayout = new GridLayout(GUI, GraphicsBox, 6, 5);

            Layout.SetComponentPosition(GraphicsBox, 1, 0, 5, 8);


            Label resolutionLabel = new Label(GUI, GraphicsLayout, "Resolution", GUI.DefaultFont);
            resolutionLabel.Alignment = Drawer2D.Alignment.Right;

            ComboBox resolutionBox = new ComboBox(GUI, GraphicsLayout);

            foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                string s = mode.Width + " x " + mode.Height;

                DisplayModes[s] = mode;
                if (mode.Width == GameSettings.Default.Resolution.Width && mode.Height == GameSettings.Default.Resolution.Height)
                {
                    resolutionBox.AddValue(s);
                    resolutionBox.CurrentValue = s;
                }
                else
                {
                    resolutionBox.AddValue(s);
                }

            }

            GraphicsLayout.SetComponentPosition(resolutionLabel, 0, 0, 1, 1);
            GraphicsLayout.SetComponentPosition(resolutionBox, 1, 0, 1, 1);


            resolutionBox.OnSelectionModified += new ComboBoxSelector.Modified(resolutionBox_OnSelectionModified);

            Checkbox fullscreenCheck = new Checkbox(GUI, GraphicsLayout, "Fullscreen", GUI.DefaultFont, GameSettings.Default.Fullscreen);
            GraphicsLayout.SetComponentPosition(fullscreenCheck, 0, 1, 1, 1);

            fullscreenCheck.OnCheckModified +=new Checkbox.CheckModified(fullscreenCheck_OnClicked);


            Label DrawDistance = new Label(GUI, GraphicsLayout, "Draw Distance", GUI.DefaultFont);
            Slider ChunkDrawSlider = new Slider(GUI, GraphicsLayout, "", GameSettings.Default.ChunkDrawDistance, 0, 1000, Slider.SliderMode.Integer);
  

            GraphicsLayout.SetComponentPosition(DrawDistance, 0, 2, 1, 1);
            GraphicsLayout.SetComponentPosition(ChunkDrawSlider, 1, 2, 1, 1);
            ChunkDrawSlider.OnValueModified += new Slider.ValueModified(ChunkDrawSlider_OnValueModified);

            Label CullDistance = new Label(GUI, GraphicsLayout, "Cull Distance", GUI.DefaultFont);
            Slider CullSlider = new Slider(GUI, GraphicsLayout, "", GameSettings.Default.VertexCullDistance, 0, 1000, Slider.SliderMode.Integer);

            CullSlider.OnValueModified += new Slider.ValueModified(CullSlider_OnValueModified);

            GraphicsLayout.SetComponentPosition(CullDistance, 0, 3, 1, 1);
            GraphicsLayout.SetComponentPosition(CullSlider, 1, 3, 1, 1);

            Label GenerateDistance = new Label(GUI, GraphicsLayout, "Generate Distance", GUI.DefaultFont);
            Slider GenerateSlider = new Slider(GUI, GraphicsLayout, "", GameSettings.Default.ChunkGenerateDistance, 0, 1000, Slider.SliderMode.Integer);

            GenerateSlider.OnValueModified += new Slider.ValueModified(GenerateSlider_OnValueModified);

            GraphicsLayout.SetComponentPosition(GenerateDistance, 0, 4, 1, 1);
            GraphicsLayout.SetComponentPosition(GenerateSlider, 1, 4, 1, 1);

            Checkbox glowBox = new Checkbox(GUI, GraphicsLayout, "Enable Glow", GUI.DefaultFont, GameSettings.Default.EnableGlow);
            GraphicsLayout.SetComponentPosition(glowBox, 1, 1, 1, 1);
            glowBox.OnCheckModified += new Checkbox.CheckModified(glowBox_OnCheckModified);

            Label AALabel = new Label(GUI, GraphicsLayout, "Antialiasing", GUI.DefaultFont);
            AALabel.Alignment = Drawer2D.Alignment.Right;

            ComboBox AABox = new ComboBox(GUI, GraphicsLayout);
            AABox.AddValue("None");
            AABox.AddValue("2");
            AABox.AddValue("4");
            AABox.AddValue("16");

            foreach (string s in AAModes.Keys)
            {
                if (AAModes[s] == GameSettings.Default.AntiAliasing)
                {
                    AABox.CurrentValue = s;
                }
            }


            AABox.OnSelectionModified += new ComboBoxSelector.Modified(AABox_OnSelectionModified);

            GraphicsLayout.SetComponentPosition(AALabel, 2, 0, 1, 1);
            GraphicsLayout.SetComponentPosition(AABox, 3, 0, 1, 1);


            Checkbox reflectTerrainBox = new Checkbox(GUI, GraphicsLayout, "Reflect Chunks", GUI.DefaultFont, GameSettings.Default.DrawChunksReflected);
            reflectTerrainBox.OnCheckModified += new Checkbox.CheckModified(reflectTerrainBox_OnCheckModified);
            GraphicsLayout.SetComponentPosition(reflectTerrainBox, 2, 1, 1, 1);

            Checkbox refractTerrainBox = new Checkbox(GUI, GraphicsLayout, "Refract Chunks", GUI.DefaultFont, GameSettings.Default.DrawChunksRefracted);
            refractTerrainBox.OnCheckModified += new Checkbox.CheckModified(refractTerrainBox_OnCheckModified);
            GraphicsLayout.SetComponentPosition(refractTerrainBox, 2, 2, 1, 1);

            Checkbox reflectEntities = new Checkbox(GUI, GraphicsLayout, "Reflect Entities", GUI.DefaultFont, GameSettings.Default.DrawEntityReflected);
            reflectEntities.OnCheckModified += new Checkbox.CheckModified(reflectEntities_OnCheckModified);
            GraphicsLayout.SetComponentPosition(reflectEntities, 3, 1, 1, 1);

            Checkbox refractEntities = new Checkbox(GUI, GraphicsLayout, "Refract Entities", GUI.DefaultFont, GameSettings.Default.DrawEntityReflected);
            refractEntities.OnCheckModified += new Checkbox.CheckModified(refractEntities_OnCheckModified);
            GraphicsLayout.SetComponentPosition(refractEntities, 3, 2, 1, 1);

            Checkbox sunlight = new Checkbox(GUI, GraphicsLayout, "Sunlight", GUI.DefaultFont, GameSettings.Default.CalculateSunlight);
            sunlight.OnCheckModified += new Checkbox.CheckModified(sunlight_OnCheckModified);
            GraphicsLayout.SetComponentPosition(sunlight, 2, 3, 1, 1);

            Checkbox AO = new Checkbox(GUI, GraphicsLayout, "Ambient Occlusion", GUI.DefaultFont, GameSettings.Default.AmbientOcclusion);
            AO.OnCheckModified += new Checkbox.CheckModified(AO_OnCheckModified);
            GraphicsLayout.SetComponentPosition(AO, 3, 3, 1, 1);

            Checkbox ramps = new Checkbox(GUI, GraphicsLayout, "Ramps", GUI.DefaultFont, GameSettings.Default.CalculateRamps);
            ramps.OnCheckModified += new Checkbox.CheckModified(ramps_OnCheckModified);
            GraphicsLayout.SetComponentPosition(ramps, 2, 4, 1, 1);

            Checkbox cursorLight = new Checkbox(GUI, GraphicsLayout, "Cursor Light", GUI.DefaultFont, GameSettings.Default.CursorLightEnabled);
            cursorLight.OnCheckModified += new Checkbox.CheckModified(cursorLight_OnCheckModified);
            GraphicsLayout.SetComponentPosition(cursorLight, 2, 5, 1, 1);

            Checkbox entityLight = new Checkbox(GUI, GraphicsLayout, "Entity Lighting", GUI.DefaultFont, GameSettings.Default.EntityLighting);
            entityLight.OnCheckModified += new Checkbox.CheckModified(entityLight_OnCheckModified);
            GraphicsLayout.SetComponentPosition(entityLight, 3, 4, 1, 1);

            Checkbox selfIllum = new Checkbox(GUI, GraphicsLayout, "Ore Glow", GUI.DefaultFont, GameSettings.Default.SelfIlluminationEnabled);
            selfIllum.OnCheckModified += new Checkbox.CheckModified(selfIllum_OnCheckModified);
            GraphicsLayout.SetComponentPosition(selfIllum, 3, 5, 1, 1);

            Checkbox particlePhysics = new Checkbox(GUI, GraphicsLayout, "Particle Physics", GUI.DefaultFont, GameSettings.Default.ParticlePhysics);
            particlePhysics.OnCheckModified += new Checkbox.CheckModified(particlePhysics_OnCheckModified);
            GraphicsLayout.SetComponentPosition(particlePhysics, 0, 5, 1, 1);


            GroupBox GraphicsBox2 = new GroupBox(GUI, Layout, "Graphics II");
            GraphicsBox2.IsVisible = false;
            Categories["Graphics II"] = GraphicsBox2;
            TabSelector.AddItem("Graphics II");

            GridLayout GraphicsLayout2 = new GridLayout(GUI, GraphicsBox2, 6, 5);

            Checkbox MoteBox = new Checkbox(GUI, GraphicsLayout2, "Generate Motes", GUI.DefaultFont, GameSettings.Default.GrassMotes);
            MoteBox.OnCheckModified += new Checkbox.CheckModified(MoteBox_OnCheckModified);
            GraphicsLayout2.SetComponentPosition(MoteBox, 1, 2, 1, 1);

            Label NumMotes = new Label(GUI, GraphicsLayout2, "Num Motes", GUI.DefaultFont);
            Slider MotesSlider = new Slider(GUI, GraphicsLayout2, "", (int)(GameSettings.Default.NumMotes * 100), 0, 1000, Slider.SliderMode.Integer);


            GraphicsLayout2.SetComponentPosition(NumMotes,    0, 1, 1, 1);
            GraphicsLayout2.SetComponentPosition(MotesSlider, 1, 1, 1, 1);
            MotesSlider.OnValueModified += new Slider.ValueModified(MotesSlider_OnValueModified);

            GroupBox GameplayBox = new GroupBox(GUI, Layout, "Gameplay");
            GameplayBox.IsVisible = false;
            Categories["Gameplay"] = GameplayBox;
            TabSelector.AddItem("Gameplay");
            GridLayout GameplayLayout = new GridLayout(GUI, GameplayBox, 6, 5);

            Label MoveSpeedLabel = new Label(GUI, GameplayLayout, "Camera Move Speed", GUI.DefaultFont);
            Slider MoveSlider = new Slider(GUI, GameplayLayout, "", GameSettings.Default.CameraScrollSpeed, 0.0f, 20.0f, Slider.SliderMode.Float);
            GameplayLayout.SetComponentPosition(MoveSpeedLabel, 0, 0, 1, 1);
            GameplayLayout.SetComponentPosition(MoveSlider, 1, 0, 1, 1);
            MoveSlider.OnValueModified += new Slider.ValueModified(MoveSlider_OnValueModified);

            Label ZoomSpeedLabel = new Label(GUI, GameplayLayout, "Zoom Speed", GUI.DefaultFont);
            Slider ZoomSlider = new Slider(GUI, GameplayLayout, "", GameSettings.Default.CameraZoomSpeed, 0.0f, 2.0f, Slider.SliderMode.Float);
            GameplayLayout.SetComponentPosition(ZoomSpeedLabel, 0, 1, 1, 1);
            GameplayLayout.SetComponentPosition(ZoomSlider, 1, 1, 1, 1);
            ZoomSlider.OnValueModified += new Slider.ValueModified(ZoomSlider_OnValueModified);

            Checkbox EdgeScrollBox = new Checkbox(GUI, GameplayLayout, "Edge Scrolling", GUI.DefaultFont, GameSettings.Default.EnableEdgeScroll);
            GameplayLayout.SetComponentPosition(EdgeScrollBox, 0, 2, 1, 1);
            EdgeScrollBox.OnCheckModified += new Checkbox.CheckModified(EdgeScrollBox_OnCheckModified);

            Checkbox IntroBox = new Checkbox(GUI, GameplayLayout, "Play Intro", GUI.DefaultFont, GameSettings.Default.DisplayIntro);
            GameplayLayout.SetComponentPosition(IntroBox, 1, 2, 1, 1);
            IntroBox.OnCheckModified += new Checkbox.CheckModified(IntroBox_OnCheckModified);

            Label ChunkWidthLabel = new Label(GUI, GameplayLayout, "Chunk Width", GUI.DefaultFont);
            Slider ChunkWidthSlider = new Slider(GUI, GameplayLayout, "", GameSettings.Default.ChunkWidth, 4, 256, Slider.SliderMode.Integer);
            GameplayLayout.SetComponentPosition(ChunkWidthLabel, 0, 3, 1, 1);
            GameplayLayout.SetComponentPosition(ChunkWidthSlider, 1, 3, 1, 1);
            ChunkWidthSlider.OnValueModified += new Slider.ValueModified(ChunkWidthSlider_OnValueModified);

            Label ChunkHeightLabel = new Label(GUI, GameplayLayout, "Chunk Height", GUI.DefaultFont);
            Slider ChunkHeightSlider = new Slider(GUI, GameplayLayout, "", GameSettings.Default.ChunkHeight, 4, 256, Slider.SliderMode.Integer);
            GameplayLayout.SetComponentPosition(ChunkHeightLabel, 2, 3, 1, 1);
            GameplayLayout.SetComponentPosition(ChunkHeightSlider, 3, 3, 1, 1);
            ChunkHeightSlider.OnValueModified += new Slider.ValueModified(ChunkHeightSlider_OnValueModified);

            Label WorldWidthLabel = new Label(GUI, GameplayLayout, "World Width", GUI.DefaultFont);
            Slider WorldWidthSlider = new Slider(GUI, GameplayLayout, "", GameSettings.Default.WorldWidth, 4, 2048, Slider.SliderMode.Integer);
            GameplayLayout.SetComponentPosition(WorldWidthLabel, 0, 4, 1, 1);
            GameplayLayout.SetComponentPosition(WorldWidthSlider, 1, 4, 1, 1);
            WorldWidthSlider.OnValueModified += new Slider.ValueModified(WorldWidthSlider_OnValueModified);

            Label WorldHeightLabel = new Label(GUI, GameplayLayout, "World Height", GUI.DefaultFont);
            Slider WorldHeightSlider = new Slider(GUI, GameplayLayout, "", GameSettings.Default.WorldHeight, 4, 2048, Slider.SliderMode.Integer);
            GameplayLayout.SetComponentPosition(WorldHeightLabel, 2, 4, 1, 1);
            GameplayLayout.SetComponentPosition(WorldHeightSlider, 3, 4, 1, 1);
            WorldHeightSlider.OnValueModified += new Slider.ValueModified(WorldHeightSlider_OnValueModified);


            Label WorldScaleLabel = new Label(GUI, GameplayLayout, "World Scale", GUI.DefaultFont);
            Slider WorldScaleSlider = new Slider(GUI, GameplayLayout, "", GameSettings.Default.WorldScale, 2, 128, Slider.SliderMode.Integer);
            GameplayLayout.SetComponentPosition(WorldScaleLabel, 0, 5, 1, 1);
            GameplayLayout.SetComponentPosition(WorldScaleSlider, 1, 5, 1, 1);
            WorldScaleSlider.OnValueModified += new Slider.ValueModified(WorldScaleSlider_OnValueModified);

            GroupBox AudioBox = new GroupBox(GUI, Layout, "Audio");
            AudioBox.IsVisible = false;
            Categories["Audio"] = AudioBox;
            TabSelector.AddItem("Audio");
            GridLayout AudioLayout = new GridLayout(GUI, AudioBox, 6, 5);

            Label MasterLabel = new Label(GUI, AudioLayout, "Master Volume", GUI.DefaultFont);
            Slider MasterSlider = new Slider(GUI, AudioLayout, "", GameSettings.Default.MasterVolume, 0.0f, 1.0f, Slider.SliderMode.Float);
            AudioLayout.SetComponentPosition(MasterLabel, 0, 0, 1, 1);
            AudioLayout.SetComponentPosition(MasterSlider, 1, 0, 1, 1);
            MasterSlider.OnValueModified += new Slider.ValueModified(MasterSlider_OnValueModified);

            Label SFXLabel = new Label(GUI, AudioLayout, "SFX Volume", GUI.DefaultFont);
            Slider SFXSlider = new Slider(GUI, AudioLayout, "", GameSettings.Default.SoundEffectVolume, 0.0f, 1.0f, Slider.SliderMode.Float);
            AudioLayout.SetComponentPosition(SFXLabel, 0, 1, 1, 1);
            AudioLayout.SetComponentPosition(SFXSlider, 1, 1, 1, 1);
            SFXSlider.OnValueModified += new Slider.ValueModified(SFXSlider_OnValueModified);

            Label MusicLabel = new Label(GUI, AudioLayout, "Music Volume", GUI.DefaultFont);
            Slider MusicSlider = new Slider(GUI, AudioLayout, "", GameSettings.Default.MusicVolume, 0.0f, 1.0f, Slider.SliderMode.Float);
            AudioLayout.SetComponentPosition(MusicLabel, 0, 2, 1, 1);
            AudioLayout.SetComponentPosition(MusicSlider, 1, 2, 1, 1);
            MusicSlider.OnValueModified += new Slider.ValueModified(MusicSlider_OnValueModified);

            GroupBox keysBox = new GroupBox(GUI, Layout, "Keys");
            keysBox.IsVisible = false;
            Categories["Keys"] = keysBox;
            TabSelector.AddItem("Keys");
            KeyEditor keyeditor = new KeyEditor(GUI, keysBox, new KeyManager(), 8, 4);
            GridLayout keyLayout = new GridLayout(GUI, keysBox, 1, 1);
            keyLayout.SetComponentPosition(keyeditor, 0, 0, 1, 1);
            keyLayout.UpdateSizes();
            keyeditor.UpdateLayout();


            GroupBox CustomBox = new GroupBox(GUI, Layout, "Customization");
            CustomBox.IsVisible = false;
            Categories["Customization"] = CustomBox;
            TabSelector.AddItem("Customization");
            
            GridLayout CustomBoxLayout = new GridLayout(GUI, CustomBox, 6, 5);

            List<string> assets = new List<string>();
            foreach (string s in TextureManager.DefaultContent.Keys)
            {
                assets.Add(s);
            }

            AssetManager assetManager = new AssetManager(GUI, CustomBoxLayout, assets);
            CustomBoxLayout.SetComponentPosition(assetManager, 0, 0, 5, 6);

            /*


            Label TilesheetLabel = new Label(GUI, CustomBoxLayout, "Tile Sheet " + AssetSettings.Default.TileSet, GUI.DefaultFont);
            CustomBoxLayout.SetComponentPosition(TilesheetLabel, 0, 0, 1, 1);

            Texture2D tilesheet = TextureManager.GetTexture("TileSet");
            TextureLoadDialog loadDialog = new TextureLoadDialog(GUI, CustomBoxLayout, "Tiles", tilesheet);
            CustomBoxLayout.SetComponentPosition(loadDialog, 0, 1, 4, 6);

            loadDialog.OnTextureSelected += new TextureLoadDialog.TextureSelected(loadDialog_OnTextureSelected);
             */

            Button apply = new Button(GUI, Layout, "Apply", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(apply, 4, 9, 1, 1);

            apply.OnClicked += new ClickedDelegate(apply_OnClicked);

            Button back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(back, 5, 9, 1, 1);

            back.OnClicked += new ClickedDelegate(back_OnClicked);


            base.OnEnter();
        }

        void MotesSlider_OnValueModified(float arg)
        {
            GameSettings.Default.NumMotes = arg / 100;
        }

        void MoteBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.GrassMotes = arg;
        }

        void particlePhysics_OnCheckModified(bool arg)
        {
            GameSettings.Default.ParticlePhysics = arg;
        }

        void selfIllum_OnCheckModified(bool arg)
        {
            GameSettings.Default.SelfIlluminationEnabled = arg;
        }

        void entityLight_OnCheckModified(bool arg)
        {
            GameSettings.Default.EntityLighting = arg;
        }

        void cursorLight_OnCheckModified(bool arg)
        {
            GameSettings.Default.CursorLightEnabled = arg;
        }

        void loadDialog_OnTextureSelected(TextureLoader.TextureFile arg)
        {
            AssetSettings.Default.TileSet = arg.File;
        }

        void MusicSlider_OnValueModified(float arg)
        {
            GameSettings.Default.MusicVolume = arg;
        }

        void SFXSlider_OnValueModified(float arg)
        {
            GameSettings.Default.SoundEffectVolume = arg;
        }

        void MasterSlider_OnValueModified(float arg)
        {
            GameSettings.Default.MasterVolume = arg;
        }

        void IntroBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.DisplayIntro = arg;
        }

        void WorldScaleSlider_OnValueModified(float arg)
        {
            GameSettings.Default.WorldScale = (int)arg;
        }

        void WorldHeightSlider_OnValueModified(float arg)
        {
            GameSettings.Default.WorldHeight = (int)arg;
        }

        void WorldWidthSlider_OnValueModified(float arg)
        {
            GameSettings.Default.WorldWidth = (int)arg;
        }

        void ChunkHeightSlider_OnValueModified(float arg)
        {
            GameSettings.Default.ChunkHeight = (int)arg;
        }

        void ChunkWidthSlider_OnValueModified(float arg)
        {
            GameSettings.Default.ChunkWidth = (int)arg;
        }

        void EdgeScrollBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.EnableEdgeScroll = arg;
        }

        void ZoomSlider_OnValueModified(float arg)
        {
            GameSettings.Default.CameraZoomSpeed = arg;
        }

        void MoveSlider_OnValueModified(float arg)
        {
            GameSettings.Default.CameraScrollSpeed = arg;
        }

        void tabSelector_OnItemClicked()
        {
            CurrentBox.IsVisible = false;
            CurrentBox = Categories[TabSelector.SelectedItem.Label];
            CurrentBox.IsVisible = true;
            Layout.SetComponentPosition(CurrentBox, 1, 0, 5, 8);
        }

        void ramps_OnCheckModified(bool arg)
        {
            GameSettings.Default.CalculateRamps = arg;
        }

        void AO_OnCheckModified(bool arg)
        {
            GameSettings.Default.AmbientOcclusion = arg;
        }

        void sunlight_OnCheckModified(bool arg)
        {
            GameSettings.Default.CalculateSunlight = arg;
        }


        void refractEntities_OnCheckModified(bool arg)
        {
            GameSettings.Default.DrawEntityRefracted = arg;
        }


        void reflectEntities_OnCheckModified(bool arg)
        {
            GameSettings.Default.DrawEntityReflected = arg;
        }

        void refractTerrainBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.DrawChunksRefracted = arg;
        }

        void reflectTerrainBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.DrawChunksReflected = arg;
        }

        void AABox_OnSelectionModified(string arg)
        {
            GameSettings.Default.AntiAliasing = AAModes[arg];
        }

        void glowBox_OnCheckModified(bool arg)
        {
            GameSettings.Default.EnableGlow = arg;
        }

        void GenerateSlider_OnValueModified(float arg)
        {
            GameSettings.Default.ChunkGenerateDistance = arg;
        }

        void CullSlider_OnValueModified(float arg)
        {
            GameSettings.Default.VertexCullDistance = arg;
        }

        void ChunkDrawSlider_OnValueModified(float arg)
        {
            GameSettings.Default.ChunkDrawDistance = arg;
        }

        void apply_OnClicked()
        {
            GameSettings.Default.Save();

            StateManager.Game.graphics.PreferredBackBufferWidth = GameSettings.Default.Resolution.Width;
            StateManager.Game.graphics.PreferredBackBufferHeight = GameSettings.Default.Resolution.Height;
            StateManager.Game.graphics.IsFullScreen = GameSettings.Default.Fullscreen;

            try
            {
                StateManager.Game.graphics.ApplyChanges();
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
        }

        void fullscreenCheck_OnClicked(bool c)
        {
            GameSettings.Default.Fullscreen = c;
        }

        void resolutionBox_OnSelectionModified(string arg)
        {
            DisplayMode mode = DisplayModes[arg];
           
            GameSettings.Default.Resolution = new System.Drawing.Size(mode.Width, mode.Height); 
        }

        void back_OnClicked()
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
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));

            DwarfGame.SpriteBatch.End();

        }

        public override void Render(GameTime gameTime)
        {

            if (Transitioning == TransitionMode.Running)
            {
                Game.GraphicsDevice.Clear(Color.Black);
                Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                DrawGUI(gameTime, 0);
            }
            else if (Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }
            else if (Transitioning == TransitionMode.Exiting)
            {
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }



            base.Render(gameTime);
        }



    }
}
