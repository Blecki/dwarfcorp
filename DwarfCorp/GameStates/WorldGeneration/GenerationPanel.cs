using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System;

namespace DwarfCorp.GameStates
{
    public class GenerationPanel : Widget
    {
        private Gui.Widget StartButton;
        private WorldGenerator Generator => GetGenerator();
        private OverworldGenerationSettings Settings;
        private DwarfGame Game;
        private GameStateManager StateManager;

        public Action RestartGeneration;
        public Func<WorldGenerator> GetGenerator;
        public Action OnVerified;
        
        public GenerationPanel(DwarfGame Game, GameStateManager StateManager, OverworldGenerationSettings Settings)
        {
            this.Settings = Settings;
            this.Game = Game;
            this.StateManager = StateManager;
        }

        public override void Construct()
        {
            AddChild(new Gui.Widget
            {
                Text = "Regenerate",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) => {
                    GameStates.GameState.Game.LogSentryBreadcrumb("WorldGenerator", "User is regeneating the world.");
                    //Settings = new OverworldGenerationSettings();
                    RestartGeneration();
                }
            });

            AddChild(new Gui.Widget
            {
                Text = "Save World",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    GameStates.GameState.Game.LogSentryBreadcrumb("WorldGenerator", "User is saving the world.");
                    if (Generator.CurrentState != WorldGenerator.GenerationState.Finished)
                        Root.ShowTooltip(Root.MousePosition, "Generator is not finished.");
                    else
                    {
                        global::System.IO.DirectoryInfo worldDirectory = global::System.IO.Directory.CreateDirectory(DwarfGame.GetWorldDirectory() + global::System.IO.Path.DirectorySeparatorChar + Settings.Name);
                        var file = new NewOverworldFile(Game.GraphicsDevice, Settings);
                        file.WriteFile(worldDirectory.FullName);
                        Root.ShowModalPopup(Root.ConstructWidget(new Gui.Widgets.Popup
                        {
                            Text = "File saved."
                        }));
                    }
                }
            });

            AddChild(new Gui.Widget
            {
                Text = "Advanced",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    GameStates.GameState.Game.LogSentryBreadcrumb("WorldGenerator", "User is modifying advanced settings.");
                    var advancedSettingsEditor = Root.ConstructWidget(new Gui.Widgets.WorldGenerationSettingsDialog
                    {
                        Settings = Settings,
                        OnClose = (s) =>
                        {
                            if ((s as Gui.Widgets.WorldGenerationSettingsDialog).Result == Gui.Widgets.WorldGenerationSettingsDialog.DialogResult.Okay)
                                RestartGeneration();
                        }
                    });

                    Root.ShowModalPopup(advancedSettingsEditor);
                }
            });

            AddChild(new Gui.Widget
            {
                Text = "Back",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockTop,
                OnClick = (sender, args) =>
                {
                    Generator.Abort();
                    StateManager.PopState();
                }
            });

            StartButton = AddChild(new Gui.Widget
            {
                Text = "I like this world",
                Border = "border-button",
                ChangeColorOnHover = true,
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font16",
                AutoLayout = Gui.AutoLayout.DockBottom,
                OnClick = (sender, args) =>
                {
                    OnVerified?.Invoke();
                }
            });

            AddChild(new Gui.Widget
            {
                Text = "Difficulty",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0,0,0,1)
            });

            var difficultySelectorCombo = AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Items = EmbarkmentLibrary.Enumerate().Select(e => e.Name).ToList(),
                TextColor = new Vector4(0, 0, 0, 1),
                Font = "font8",
                OnSelectedIndexChanged = (sender) =>
                {
                    Settings.InitalEmbarkment = EmbarkmentLibrary.GetEmbarkment((sender as Gui.Widgets.ComboBox).SelectedItem);
                }
            }) as Gui.Widgets.ComboBox;

            AddChild(new Gui.Widget
            {
                Text = "Caves",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
            });

            var layerSetting = AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = AutoLayout.DockTop,
                Items = new List<string>(new string[] { "Barely any", "Few", "Normal", "Lots", "Way too many" }),
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
                OnSelectedIndexChanged = (sender) =>
                {
                    switch ((sender as Gui.Widgets.ComboBox).SelectedItem)
                    {
                        case "Barely any": Settings.NumCaveLayers = 2; break;
                        case "Few": Settings.NumCaveLayers = 3; break;
                        case "Normal": Settings.NumCaveLayers = 4; break;
                        case "Lots": Settings.NumCaveLayers = 6; break;
                        case "Way too many": Settings.NumCaveLayers = 9; break;
                    }
                }
            }) as Gui.Widgets.ComboBox;

            AddChild(new Gui.Widget
            {
                Text = "Z Levels",
                AutoLayout = Gui.AutoLayout.DockTop,
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
            });

            var zLevelSetting = AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = AutoLayout.DockTop,
                Items = new List<string>(new string[] { "16", "64", "128" }),
                Font = "font8",
                TextColor = new Vector4(0, 0, 0, 1),
                OnSelectedIndexChanged = (sender) =>
                {
                    switch ((sender as Gui.Widgets.ComboBox).SelectedItem)
                    {
                        case "16": Settings.zLevels = 1; break;
                        case "64": Settings.zLevels = 4; break;
                        case "128": Settings.zLevels = 8; break;
                    }
                }
            }) as Gui.Widgets.ComboBox;

            zLevelSetting.SelectedIndex = 1;
            difficultySelectorCombo.SelectedIndex = difficultySelectorCombo.Items.IndexOf("Normal");
            layerSetting.SelectedIndex = layerSetting.Items.IndexOf("Normal");

            base.Construct();
        }
    }
}