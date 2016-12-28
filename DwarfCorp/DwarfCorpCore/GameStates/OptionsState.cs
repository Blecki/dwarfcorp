// OptionsState.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{
    /// <summary>
    ///     This game state allows the player to modify game options (stored in the GameSettings file).
    /// </summary>
    public class OptionsState : GameState
    {
        public OptionsState(DwarfGame game, GameStateManager stateManager) :
            base(game, "OptionsState", stateManager)
        {
            EdgePadding = 32;
            Input = new InputManager();
            Categories = new Dictionary<string, GroupBox>();
            DisplayModes = new Dictionary<string, DisplayMode>();
            AAModes = new Dictionary<string, int>();
            AAModes["None"] = 0;
            AAModes["FXAA"] = -1;
            AAModes["2x MSAA"] = 2;
            AAModes["4x MSAA"] = 4;
            AAModes["16x MSAA"] = 16;
        }

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

        public override void OnEnter()
        {
            DefaultFont = Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            GUI = new DwarfGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title),
                Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input);
            IsInitialized = true;
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            MainWindow = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds =
                    new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding*2,
                        Game.GraphicsDevice.Viewport.Height - EdgePadding*2)
            };
            Layout = new GridLayout(GUI, MainWindow, 12, 6);

            var label = new Label(GUI, Layout, "Options", GUI.TitleFont);
            Layout.SetComponentPosition(label, 0, 0, 1, 1);

            TabSelector = new TabSelector(GUI, Layout, 6);
            Layout.SetComponentPosition(TabSelector, 0, 1, 6, 10);

            TabSelector.Tab simpleGraphicsTab = TabSelector.AddTab("Graphics");
            var simpleGraphicsBox = new GroupBox(GUI, simpleGraphicsTab, "Graphics")
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit
            };
            var simpleGraphicsLayout = new FormLayout(GUI, simpleGraphicsBox);
            var simpleGraphicsSelector = new ComboBox(GUI, simpleGraphicsLayout);
            simpleGraphicsSelector.AddValue("Custom");
            simpleGraphicsSelector.AddValue("Lowest");
            simpleGraphicsSelector.AddValue("Low");
            simpleGraphicsSelector.AddValue("Medium");
            simpleGraphicsSelector.AddValue("High");
            simpleGraphicsSelector.AddValue("Highest");

            simpleGraphicsLayout.AddItem("Graphics Quality", simpleGraphicsSelector);

            simpleGraphicsSelector.OnSelectionModified += simpleGraphicsSelector_OnSelectionModified;


            CreateGraphicsTab();

            TabSelector.Tab gameplayTab = TabSelector.AddTab("Gameplay");
            var gameplayBox = new GroupBox(GUI, gameplayTab, "Gameplay")
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit
            };
            Categories["Gameplay"] = gameplayBox;

            var gameplayLayout = new GridLayout(GUI, gameplayBox, 6, 5);

            var moveSpeedLabel = new Label(GUI, gameplayLayout, "Camera Move Speed", GUI.DefaultFont);
            var moveSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.CameraScrollSpeed, 0.0f, 20.0f,
                Slider.SliderMode.Float)
            {
                ToolTip = "Determines how fast the camera will move when keys are pressed."
            };

            gameplayLayout.SetComponentPosition(moveSpeedLabel, 0, 0, 1, 1);
            gameplayLayout.SetComponentPosition(moveSlider, 1, 0, 1, 1);
            moveSlider.OnValueModified += check => { GameSettings.Default.CameraScrollSpeed = check; };

            var zoomSpeedLabel = new Label(GUI, gameplayLayout, "Zoom Speed", GUI.DefaultFont);
            var zoomSlider = new Slider(GUI, gameplayLayout, "", GameSettings.Default.CameraZoomSpeed, 0.0f, 2.0f,
                Slider.SliderMode.Float)
            {
                ToolTip = "Determines how fast the camera will go\nup and down with the scroll wheel."
            };

            gameplayLayout.SetComponentPosition(zoomSpeedLabel, 0, 1, 1, 1);
            gameplayLayout.SetComponentPosition(zoomSlider, 1, 1, 1, 1);
            zoomSlider.OnValueModified += check => { GameSettings.Default.CameraZoomSpeed = check; };

            var invertZoomBox = new Checkbox(GUI, gameplayLayout, "Invert Zoom", GUI.DefaultFont,
                GameSettings.Default.InvertZoom)
            {
                ToolTip = "When checked, the scroll wheel is reversed\nfor zooming."
            };

            gameplayLayout.SetComponentPosition(invertZoomBox, 2, 1, 1, 1);
            invertZoomBox.OnCheckModified += check => { GameSettings.Default.InvertZoom = check; };


            var edgeScrollBox = new Checkbox(GUI, gameplayLayout, "Edge Scrolling", GUI.DefaultFont,
                GameSettings.Default.EnableEdgeScroll)
            {
                ToolTip = "When checked, the camera will scroll\nwhen the cursor is at the edge of the screen."
            };

            gameplayLayout.SetComponentPosition(edgeScrollBox, 0, 2, 1, 1);
            edgeScrollBox.OnCheckModified += check => { GameSettings.Default.EnableEdgeScroll = check; };

            var introBox = new Checkbox(GUI, gameplayLayout, "Play Intro", GUI.DefaultFont,
                GameSettings.Default.DisplayIntro)
            {
                ToolTip = "When checked, the intro will be played when the game starts"
            };

            gameplayLayout.SetComponentPosition(introBox, 1, 2, 1, 1);
            introBox.OnCheckModified += check => { GameSettings.Default.DisplayIntro = check; };

            var fogOfWarBox = new Checkbox(GUI, gameplayLayout, "Fog of War", GUI.DefaultFont,
                GameSettings.Default.FogofWar)
            {
                ToolTip = "When checked, unexplored blocks will be blacked out"
            };

            gameplayLayout.SetComponentPosition(fogOfWarBox, 2, 2, 1, 1);

            fogOfWarBox.OnCheckModified += check => { GameSettings.Default.FogofWar = check; };


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

            var audioBox = new GroupBox(GUI, audioTab, "Audio")
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit
            };
            Categories["Audio"] = audioBox;

            var audioLayout = new GridLayout(GUI, audioBox, 6, 5);

            var masterLabel = new Label(GUI, audioLayout, "Master Volume", GUI.DefaultFont);
            var masterSlider = new Slider(GUI, audioLayout, "", GameSettings.Default.MasterVolume, 0.0f, 1.0f,
                Slider.SliderMode.Float);
            audioLayout.SetComponentPosition(masterLabel, 0, 0, 1, 1);
            audioLayout.SetComponentPosition(masterSlider, 1, 0, 1, 1);
            masterSlider.OnValueModified += value => { GameSettings.Default.MasterVolume = value; };

            var sfxLabel = new Label(GUI, audioLayout, "SFX Volume", GUI.DefaultFont);
            var sfxSlider = new Slider(GUI, audioLayout, "", GameSettings.Default.SoundEffectVolume, 0.0f, 1.0f,
                Slider.SliderMode.Float);
            audioLayout.SetComponentPosition(sfxLabel, 0, 1, 1, 1);
            audioLayout.SetComponentPosition(sfxSlider, 1, 1, 1, 1);
            sfxSlider.OnValueModified += check => { GameSettings.Default.SoundEffectVolume = check; };

            var musicLabel = new Label(GUI, audioLayout, "Music Volume", GUI.DefaultFont);
            var musicSlider = new Slider(GUI, audioLayout, "", GameSettings.Default.MusicVolume, 0.0f, 1.0f,
                Slider.SliderMode.Float);
            audioLayout.SetComponentPosition(musicLabel, 0, 2, 1, 1);
            audioLayout.SetComponentPosition(musicSlider, 1, 2, 1, 1);
            musicSlider.OnValueModified += check => { GameSettings.Default.MusicVolume = check; };

            TabSelector.Tab keysTab = TabSelector.AddTab("Keys");
            var keysBox = new GroupBox(GUI, keysTab, "Keys")
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit
            };
            Categories["Keys"] = keysBox;

            var keyeditor = new KeyEditor(GUI, keysBox, new KeyManager(), 8, 4);
            var keyLayout = new GridLayout(GUI, keysBox, 1, 1);
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

            var apply = new Button(GUI, Layout, "Apply", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                GUI.Skin.GetSpecialFrame(GUISkin.Tile.Check));
            Layout.SetComponentPosition(apply, 5, 11, 1, 1);
            apply.OnClicked += apply_OnClicked;

            var back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton,
                GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow));
            Layout.SetComponentPosition(back, 4, 11, 1, 1);
            back.OnClicked += back_OnClicked;

            Layout.UpdateSizes();
            TabSelector.UpdateSize();
            TabSelector.SetTab("Graphics");
            base.OnEnter();
        }

        private void simpleGraphicsSelector_OnSelectionModified(string arg)
        {
            switch (arg)
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
                    CreateGraphicsTab();
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
                    CreateGraphicsTab();
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
                    CreateGraphicsTab();
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
                    CreateGraphicsTab();
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
                    CreateGraphicsTab();
                    break;
            }
        }

        private void CreateGraphicsTab()
        {
            TabSelector.Tab graphicsTab = null;

            if (!TabSelector.Tabs.ContainsKey("Advanced Graphics"))
            {
                graphicsTab = TabSelector.AddTab("Advanced Graphics");
            }
            else
            {
                graphicsTab = TabSelector.Tabs["Advanced Graphics"];
                graphicsTab.Reset();
            }
            var tabLayout = new GridLayout(GUI, graphicsTab, 1, 1);

            var graphicsView = new ScrollView(GUI, tabLayout);
            tabLayout.SetComponentPosition(graphicsView, 0, 0, 1, 1);

            var graphicsBox = new GroupBox(GUI, graphicsView, "Advanced Graphics")
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fixed,
                MinHeight = 640,
                MaxHeight = 640
            };

            Categories["Advanced Graphics"] = graphicsBox;
            CurrentBox = graphicsBox;

            var graphicsLayout = new GridLayout(GUI, graphicsBox, 10, 5);


            var resolutionLabel = new Label(GUI, graphicsLayout, "Resolution", GUI.DefaultFont)
            {
                Alignment = Drawer2D.Alignment.Right
            };

            var resolutionBox = new ComboBox(GUI, graphicsLayout)
            {
                ToolTip = "Sets the size of the screen.\nSmaller for higher framerates."
            };

            foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (mode.Format != SurfaceFormat.Color)
                {
                    continue;
                }

                if (mode.Width <= 640) continue;

                string s = mode.Width + " x " + mode.Height;
                DisplayModes[s] = mode;
                if (mode.Width == GameSettings.Default.ResolutionX && mode.Height == GameSettings.Default.ResolutionY)
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


            resolutionBox.OnSelectionModified += s =>
            {
                GameSettings.Default.ResolutionX = DisplayModes[s].Width;
                GameSettings.Default.ResolutionY = DisplayModes[s].Height;
            };

            var fullscreenCheck = new Checkbox(GUI, graphicsLayout, "Fullscreen", GUI.DefaultFont,
                GameSettings.Default.Fullscreen)
            {
                ToolTip = "If this is checked, the game takes up the whole screen."
            };

            graphicsLayout.SetComponentPosition(fullscreenCheck, 0, 1, 1, 1);

            fullscreenCheck.OnCheckModified += b => { GameSettings.Default.Fullscreen = b; };


            var drawDistance = new Label(GUI, graphicsLayout, "Draw Distance", GUI.DefaultFont);
            var chunkDrawSlider = new Slider(GUI, graphicsLayout, "", GameSettings.Default.ChunkDrawDistance, 1, 1000,
                Slider.SliderMode.Integer)
            {
                ToolTip = "Maximum distance at which terrain will be drawn\nSmaller for faster."
            };


            graphicsLayout.SetComponentPosition(drawDistance, 0, 2, 1, 1);
            graphicsLayout.SetComponentPosition(chunkDrawSlider, 1, 2, 1, 1);
            chunkDrawSlider.OnValueModified += b => { GameSettings.Default.ChunkDrawDistance = b; };

            var cullDistance = new Label(GUI, graphicsLayout, "Cull Distance", GUI.DefaultFont);
            var cullSlider = new Slider(GUI, graphicsLayout, "", GameSettings.Default.VertexCullDistance, 0.1f, 1000,
                Slider.SliderMode.Integer)
            {
                ToolTip = "Maximum distance at which anything will be drawn\n Smaller for faster."
            };

            cullSlider.OnValueModified += v => { GameSettings.Default.VertexCullDistance = v; };

            graphicsLayout.SetComponentPosition(cullDistance, 0, 3, 1, 1);
            graphicsLayout.SetComponentPosition(cullSlider, 1, 3, 1, 1);

            var generateDistance = new Label(GUI, graphicsLayout, "Generate Distance", GUI.DefaultFont);
            var generateSlider = new Slider(GUI, graphicsLayout, "", GameSettings.Default.ChunkGenerateDistance, 1, 1000,
                Slider.SliderMode.Integer)
            {
                ToolTip = "Maximum distance at which terrain will be generated."
            };

            generateSlider.OnValueModified += v => { GameSettings.Default.ChunkGenerateDistance = v; };

            graphicsLayout.SetComponentPosition(generateDistance, 0, 4, 1, 1);
            graphicsLayout.SetComponentPosition(generateSlider, 1, 4, 1, 1);

            var glowBox = new Checkbox(GUI, graphicsLayout, "Enable Glow", GUI.DefaultFont,
                GameSettings.Default.EnableGlow)
            {
                ToolTip = "When checked, there will be a fullscreen glow effect."
            };

            graphicsLayout.SetComponentPosition(glowBox, 1, 1, 1, 1);
            glowBox.OnCheckModified += b => { GameSettings.Default.EnableGlow = b; };

            var aaLabel = new Label(GUI, graphicsLayout, "Antialiasing", GUI.DefaultFont)
            {
                Alignment = Drawer2D.Alignment.Right
            };

            var aaBox = new ComboBox(GUI, graphicsLayout)
            {
                ToolTip =
                    "Determines how much antialiasing (smoothing) there is.\nHigher means more smooth, but is slower."
            };
            aaBox.AddValue("None");
            aaBox.AddValue("FXAA");
            aaBox.AddValue("2x MSAA");
            aaBox.AddValue("4x MSAA");
            aaBox.AddValue("16x MSAA");

            foreach (string s in AAModes.Keys.Where(s => AAModes[s] == GameSettings.Default.AntiAliasing))
            {
                aaBox.CurrentValue = s;
            }


            aaBox.OnSelectionModified += s => { GameSettings.Default.AntiAliasing = AAModes[s]; };

            graphicsLayout.SetComponentPosition(aaLabel, 2, 0, 1, 1);
            graphicsLayout.SetComponentPosition(aaBox, 3, 0, 1, 1);


            var reflectTerrainBox = new Checkbox(GUI, graphicsLayout, "Reflect Chunks", GUI.DefaultFont,
                GameSettings.Default.DrawChunksReflected)
            {
                ToolTip = "When checked, water will reflect terrain."
            };
            reflectTerrainBox.OnCheckModified += b => { GameSettings.Default.DrawChunksReflected = b; };
            graphicsLayout.SetComponentPosition(reflectTerrainBox, 2, 1, 1, 1);

            var reflectEntities = new Checkbox(GUI, graphicsLayout, "Reflect Entities", GUI.DefaultFont,
                GameSettings.Default.DrawEntityReflected)
            {
                ToolTip = "When checked, water will reflect trees, dwarves, etc."
            };
            reflectEntities.OnCheckModified += b => { GameSettings.Default.DrawEntityReflected = b; };
            graphicsLayout.SetComponentPosition(reflectEntities, 3, 1, 1, 1);

            var sunlight = new Checkbox(GUI, graphicsLayout, "Sunlight", GUI.DefaultFont,
                GameSettings.Default.CalculateSunlight)
            {
                ToolTip = "When checked, terrain will be lit/shadowed by the sun."
            };
            sunlight.OnCheckModified += b => { GameSettings.Default.CalculateSunlight = b; };
            graphicsLayout.SetComponentPosition(sunlight, 2, 3, 1, 1);

            var ao = new Checkbox(GUI, graphicsLayout, "Ambient Occlusion", GUI.DefaultFont,
                GameSettings.Default.AmbientOcclusion)
            {
                ToolTip = "When checked, terrain will smooth shading effects."
            };
            ao.OnCheckModified += b => { GameSettings.Default.AmbientOcclusion = b; };
            graphicsLayout.SetComponentPosition(ao, 3, 3, 1, 1);

            var ramps = new Checkbox(GUI, graphicsLayout, "Ramps", GUI.DefaultFont, GameSettings.Default.CalculateRamps)
            {
                ToolTip = "When checked, some terrain will have smooth ramps."
            };

            ramps.OnCheckModified += b => { GameSettings.Default.CalculateRamps = b; };
            graphicsLayout.SetComponentPosition(ramps, 2, 4, 1, 1);

            var cursorLight = new Checkbox(GUI, graphicsLayout, "Cursor Light", GUI.DefaultFont,
                GameSettings.Default.CursorLightEnabled)
            {
                ToolTip = "When checked, a light will follow the player cursor."
            };

            cursorLight.OnCheckModified += b => { GameSettings.Default.CursorLightEnabled = b; };
            graphicsLayout.SetComponentPosition(cursorLight, 2, 5, 1, 1);

            var entityLight = new Checkbox(GUI, graphicsLayout, "Entity Lighting", GUI.DefaultFont,
                GameSettings.Default.EntityLighting)
            {
                ToolTip = "When checked, dwarves, objects, etc. will be lit\nby the sun, lamps, etc."
            };

            entityLight.OnCheckModified += b => { GameSettings.Default.EntityLighting = b; };
            graphicsLayout.SetComponentPosition(entityLight, 3, 4, 1, 1);

            var selfIllum = new Checkbox(GUI, graphicsLayout, "Ore Glow", GUI.DefaultFont,
                GameSettings.Default.SelfIlluminationEnabled)
            {
                ToolTip = "When checked, some terrain elements will glow."
            };

            selfIllum.OnCheckModified += b => { GameSettings.Default.SelfIlluminationEnabled = b; };
            graphicsLayout.SetComponentPosition(selfIllum, 3, 5, 1, 1);

            var particlePhysics = new Checkbox(GUI, graphicsLayout, "Particle Body", GUI.DefaultFont,
                GameSettings.Default.ParticlePhysics)
            {
                ToolTip = "When checked, some particles will bounce off terrain."
            };

            particlePhysics.OnCheckModified += b => { GameSettings.Default.ParticlePhysics = b; };
            graphicsLayout.SetComponentPosition(particlePhysics, 0, 5, 1, 1);

            var moteBox = new Checkbox(GUI, graphicsLayout, "Generate Motes", GUI.DefaultFont,
                GameSettings.Default.GrassMotes)
            {
                ToolTip = "When checked, small detail vegetation will be visible."
            };

            moteBox.OnCheckModified += check => { GameSettings.Default.GrassMotes = check; };
            graphicsLayout.SetComponentPosition(moteBox, 0, 6, 1, 1);

            var numMotes = new Label(GUI, graphicsLayout, "Num Motes", GUI.DefaultFont);
            var motesSlider = new Slider(GUI, graphicsLayout, "", GameSettings.Default.NumMotes, 100, 2048,
                Slider.SliderMode.Integer)
            {
                ToolTip = "Determines the maximum amount of trees/grass that will be visible."
            };
            graphicsLayout.SetComponentPosition(numMotes, 0, 7, 1, 1);
            graphicsLayout.SetComponentPosition(motesSlider, 1, 7, 1, 1);


            var lightMapBox = new Checkbox(GUI, graphicsLayout, "Light Maps", GUI.DefaultFont,
                GameSettings.Default.UseLightmaps)
            {
                ToolTip = "When checked, terrain will be rendered using light maps."
            };
            lightMapBox.OnCheckModified += check => { GameSettings.Default.UseLightmaps = check; };

            graphicsLayout.SetComponentPosition(lightMapBox, 0, 8, 1, 1);


            var shadowBox = new Checkbox(GUI, graphicsLayout, "Dynamic Shadows", GUI.DefaultFont,
                GameSettings.Default.UseLightmaps)
            {
                ToolTip = "When checked, terrain casts dynamic shadows"
            };
            shadowBox.OnCheckModified += check => { GameSettings.Default.UseDynamicShadows = check; };

            graphicsLayout.SetComponentPosition(shadowBox, 0, 9, 1, 1);

            motesSlider.OnValueModified += MotesSlider_OnValueModified;
        }

        private void MotesSlider_OnValueModified(float arg)
        {
            GameSettings.Default.NumMotes = Math.ClampValue((int) (arg), 100, 2048);
        }

        private void tabSelector_OnItemClicked()
        {
            CurrentBox.IsVisible = false;
            //CurrentBox = Categories[TabSelector.SelectedItem.Label];
            CurrentBox.IsVisible = true;
            Layout.SetComponentPosition(CurrentBox, 1, 0, 5, 8);
        }

        private void apply_OnClicked()
        {
            GameSettings.Save();

            StateManager.Game.Graphics.PreferredBackBufferWidth = GameSettings.Default.ResolutionX;
            StateManager.Game.Graphics.PreferredBackBufferHeight = GameSettings.Default.ResolutionY;
            StateManager.Game.Graphics.IsFullScreen = GameSettings.Default.Fullscreen;

            try
            {
                StateManager.Game.Graphics.ApplyChanges();
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                Dialog.Popup(GUI, "Failure!", exception.Message, Dialog.ButtonType.OK);
            }

            Dialog.Popup(GUI, "Info", "Some settings will not take effect until the game is restarted.",
                Dialog.ButtonType.OK);
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

        public override void Update(DwarfTime gameTime)
        {
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding,
                Game.GraphicsDevice.Viewport.Width - EdgePadding*2, Game.GraphicsDevice.Viewport.Height - EdgePadding*2);
            Input.Update();
            GUI.Update(gameTime);
            base.Update(gameTime);
        }


        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            var rasterizerState = new RasterizerState
            {
                ScissorTestEnable = true
            };
            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp,
                null, rasterizerState);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));

            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();

            DwarfGame.SpriteBatch.GraphicsDevice.ScissorRectangle = DwarfGame.SpriteBatch.GraphicsDevice.Viewport.Bounds;
        }

        public override void Render(DwarfTime gameTime)
        {
            if (Transitioning == TransitionMode.Running)
            {
                Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                DrawGUI(gameTime, 0);
            }
            else if (Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width,
                    Game.GraphicsDevice.Viewport.Width, 1.0f);
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