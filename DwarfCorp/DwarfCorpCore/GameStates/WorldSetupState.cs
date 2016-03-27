// WorldSetupState.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This game state allows the player to load generated worlds from files.
    /// </summary>
    public class WorldSetupState : GameState
    {
        public Panel MainPanel { get; set; }
        public GridLayout Layout { get; set; }
        public Label NameLabel { get; set; }
        public LineEdit NameEdit { get; set; }
        public Button NameRandomButton { get; set; }
        public ScrollView OptionsView { get; set; }
        public FormLayout OptionsLayout { get; set; }
        public Button BackButton { get; set; }
        public Button AcceptButton { get; set; }
        public WorldSettings Settings { get; set; }
        public DwarfGUI GUI { get; set; }
        public InputManager Input { get; set; }
        private const string VeryLow = "Very Low";
        private const string Low = "Low";
        private const string Medium = "Medium";
        private const string High = "High";
        private const string VeryHigh = "Very High"; 

        public WorldSetupState(DwarfGame game, GameStateManager stateManager) :
            base(game, "WorldSetupState", stateManager)
        {
            IsInitialized = false;
            Settings = new WorldSettings()
            {
                Width = 512,
                Height = 512,
                Name = GetRandomWorldName(),
                NumCivilizations = 5,
                NumFaults = 3,
                NumRains = 1000,
                NumVolcanoes = 3,
                RainfallScale = 1.0f,
                SeaLevel = 0.17f,
                TemperatureScale = 1.0f
            };
        }

        public void CreateGUI()
        {
            Input = new InputManager();
            GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default), 
                                      Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), 
                                      Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), 
                                      Input);
            MainPanel = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds =
                    new Rectangle(128, 64, Game.GraphicsDevice.Viewport.Width - 256,
                        Game.GraphicsDevice.Viewport.Height - 128)
            };
            
            Layout = new GridLayout(GUI, MainPanel, 8, 6)
            {
                HeightSizeMode = GUIComponent.SizeMode.Fit,
                WidthSizeMode = GUIComponent.SizeMode.Fit
            };

            NameLabel = new Label(GUI, Layout, "World Name: ", GUI.DefaultFont);
            Layout.SetComponentPosition(NameLabel, 0, 0, 1, 1);

            NameEdit = new LineEdit(GUI, Layout, Settings.Name);
            Layout.SetComponentPosition(NameEdit, 1, 0, 4, 1);
            NameEdit.OnTextModified += NameEdit_OnTextModified;

            NameRandomButton = new Button(GUI, Layout, "Random", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(NameRandomButton, 5, 0, 1, 1);
            NameRandomButton.OnClicked += NameRandomButton_OnClicked;

            OptionsView = new ScrollView(GUI, Layout)
            {
                DrawBorder = true
            };
            Layout.SetComponentPosition(OptionsView, 0, 1, 6, 6);

            OptionsLayout = new FormLayout(GUI, OptionsView)
            {
                WidthSizeMode = GUIComponent.SizeMode.Fit,
                HeightSizeMode = GUIComponent.SizeMode.Fit
            };

            ComboBox sizeBox = CreateOptionsBox("World Size", "Size of the world to generate", OptionsLayout);
            sizeBox.OnSelectionModified += sizeBox_OnSelectionModified;
            sizeBox.InvokeSelectionModified();

            ComboBox nativeBox = CreateOptionsBox("Natives", "Number of native civilizations", OptionsLayout);
            nativeBox.OnSelectionModified += nativeBox_OnSelectionModified;
            nativeBox.InvokeSelectionModified();

            ComboBox faultBox = CreateOptionsBox("Faults",  "Number of straights, seas, etc.", OptionsLayout);
            faultBox.OnSelectionModified += faultBox_OnSelectionModified;
            faultBox.InvokeSelectionModified();

            ComboBox rainBox = CreateOptionsBox("Rainfall", "Amount of moisture in the world.", OptionsLayout);
            rainBox.OnSelectionModified += rainBox_OnSelectionModified;
            rainBox.InvokeSelectionModified();

            ComboBox erosionBox = CreateOptionsBox("Erosion", "More or less eroded landscape.", OptionsLayout);
            erosionBox.OnSelectionModified += erosionBox_OnSelectionModified;
            erosionBox.InvokeSelectionModified();

            ComboBox seaBox = CreateOptionsBox("Sea Level", "Height of the sea.", OptionsLayout);
            seaBox.OnSelectionModified += seaBox_OnSelectionModified;
            seaBox.InvokeSelectionModified();

            ComboBox temp = CreateOptionsBox("Temperature", "Average temperature.", OptionsLayout);
            temp.OnSelectionModified += temp_OnSelectionModified;
            temp.InvokeSelectionModified();

            BackButton = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow))
            {
                ToolTip = "Back to the main menu."
            };
            Layout.SetComponentPosition(BackButton, 0, 7, 1, 1);
            BackButton.OnClicked += BackButton_OnClicked;

            AcceptButton = new Button(GUI, Layout, "Next", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.RightArrow))
            {
                ToolTip = "Generate a world with these settings"
            };
            AcceptButton.OnClicked += AcceptButton_OnClicked;
            Layout.SetComponentPosition(AcceptButton, 5, 7, 1, 1);
        }

        void erosionBox_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case VeryLow:
                    Settings.NumRains = 50;
                    break;
                case Low:
                    Settings.NumRains = 1000;
                    break;
                case Medium:
                    Settings.NumRains = 8000;
                    break;
                case High:
                    Settings.NumRains = 20000;
                    break;
                case VeryHigh:
                    Settings.NumRains = 50000;
                    break;
            }
        }

        void temp_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case VeryLow:
                    Settings.TemperatureScale = 0.0f;
                    break;
                case Low:
                    Settings.TemperatureScale = 0.5f;
                    break;
                case Medium:
                    Settings.TemperatureScale = 1.0f;
                    break;
                case High:
                    Settings.TemperatureScale = 1.5f;
                    break;
                case VeryHigh:
                    Settings.TemperatureScale = 2.0f;
                    break;
            }
        }

        void seaBox_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case VeryLow:
                    Settings.SeaLevel = 0.05f;
                    break;
                case Low:
                    Settings.SeaLevel = 0.1f;
                    break;
                case Medium:
                    Settings.SeaLevel = 0.17f;
                    break;
                case High:
                    Settings.SeaLevel = 0.25f;
                    break;
                case VeryHigh:
                    Settings.SeaLevel = 0.3f;
                    break;
            }
        }

        void rainBox_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case VeryLow:
                    Settings.RainfallScale = 0.0f;
                    break;
                case Low:
                    Settings.RainfallScale = 0.5f;
                    break;
                case Medium:
                    Settings.RainfallScale = 1.0f;
                    break;
                case High:
                    Settings.RainfallScale = 1.5f;
                    break;
                case VeryHigh:
                    Settings.RainfallScale = 2.0f;
                    break;
            }
        }

        void faultBox_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case VeryLow:
                    Settings.NumFaults = 0;
                    break;
                case Low:
                    Settings.NumFaults = 1;
                    break;
                case Medium:
                    Settings.NumFaults = 3;
                    break;
                case High:
                    Settings.NumFaults = 5;
                    break;
                case VeryHigh:
                    Settings.NumFaults = 10;
                    break;
            }
        }

        void nativeBox_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case VeryLow:
                    Settings.NumCivilizations = 0;
                    break;
                case Low:
                    Settings.NumCivilizations = 2;
                    break;
                case Medium:
                    Settings.NumCivilizations = 4;
                    break;
                case High:
                    Settings.NumCivilizations = 8;
                    break;
                case VeryHigh:
                    Settings.NumCivilizations = 16;
                    break;
            }
        }

        void sizeBox_OnSelectionModified(string arg)
        {
            switch (arg)
            {
                case VeryLow:
                    Settings.Width = 256;
                    Settings.Height = 256;
                    break;
                case Low:
                    Settings.Width = 350;
                    Settings.Height = 350;
                    break;
                case Medium:
                    Settings.Width = 512;
                    Settings.Height = 512;
                    break;
                case High:
                    Settings.Width = 1024;
                    Settings.Height = 1024;
                    break;
                case VeryHigh:
                    Settings.Width = 2048;
                    Settings.Height = 2048;
                    break;
            }
        }

        ComboBox CreateOptionsBox(string label, string tooltip, FormLayout layout)
        {
            ComboBox box = new ComboBox(GUI, layout)
            {
                ToolTip = tooltip
            };
            box.AddValue(VeryLow);
            box.AddValue(Low);
            box.AddValue(Medium);
            box.AddValue(High);
            box.AddValue(VeryHigh);
            box.CurrentIndex = 2;
            box.CurrentValue = Medium;
            layout.AddItem(label, box);
            return box;
        }

        public static string GetRandomWorldName()
        {
            List<List<string>> templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds);
            return TextGenerator.GenerateRandom(templates);
        }

        void NameRandomButton_OnClicked()
        {
            Settings.Name = GetRandomWorldName();
            NameEdit.Text = Settings.Name;
        }

        void NameEdit_OnTextModified(string arg)
        {
            Settings.Name = arg;
        }

        void AcceptButton_OnClicked()
        {
            WorldGeneratorState state = StateManager.GetState<WorldGeneratorState>("WorldGeneratorState");

            if (state != null)
            {
                state.Settings = Settings;
            }
            StateManager.PopState();
        }

        void BackButton_OnClicked()
        {
            StateManager.PopState();
        }

        public override void OnEnter()
        {
            CreateGUI();
            IsInitialized = true;
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
        public override void Update(DwarfTime gameTime)
        {
            Input.Update();
            GUI.Update(gameTime);
            base.Update(gameTime);
        }


        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizerState);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));
            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();
        }

        public override void Render(DwarfTime gameTime)
        {
            switch(Transitioning)
            {
                case TransitionMode.Running:
                    DrawGUI(gameTime, 0);
                    break;
                case TransitionMode.Entering:
                {
                    float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                    DrawGUI(gameTime, dx);
                }
                    break;
                case TransitionMode.Exiting:
                {
                    float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                    DrawGUI(gameTime, dx);
                }
                    break;
            }

            base.Render(gameTime);
        }
    }

}