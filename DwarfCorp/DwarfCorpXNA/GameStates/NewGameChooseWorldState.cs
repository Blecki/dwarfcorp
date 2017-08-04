using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{
    public class NewGameChooseWorldState : GameState
    {
        private Gui.Root GuiRoot;

        public NewGameChooseWorldState(DwarfGame game, GameStateManager stateManager) :
            base(game, "MainMenuState", stateManager)
        {
        }

        private Gui.Widget MakeMenuFrame(String Name)
        {
            GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(600, 348),
                Background = new Gui.TileReference("logo", 0),
                AutoLayout = Gui.AutoLayout.FloatTop,
            });

            return GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(400, 200),
                Font = "outline-font",
                Border = "basic",
                Background = new TileReference("basic", 0),
                BackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f),
                AutoLayout = Gui.AutoLayout.FloatBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                Text = Name,
                InteriorMargin = new Gui.Margin(24, 0, 0, 0),
                Padding = new Gui.Margin(2, 2, 2, 2),
                TextColor = Color.White.ToVector4()
            });
        }

        private void MakeMenuItem(Gui.Widget Menu, string Name, string Tooltip, Action<Gui.Widget, Gui.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gui.Widgets.Button
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Text = Name,
                Border = "none",
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Font = "outline-font",
                TextColor = Color.White.ToVector4(),
                HoverTextColor = Color.Red.ToVector4()
            });
        }

        public void MakeMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("PLAY DWARFCORP");

            //MakeMenuItem(frame, "Generate World", "Create a new world from scratch.", (sender, args) =>
            //    StateManager.PushState(new WorldGeneratorState(Game, StateManager)));

            MakeMenuItem(frame, "Generate World", "Create a new world from scratch.", (sender, args) =>
                StateManager.PushState(new WorldGeneratorState(Game, StateManager, null, true)));

            MakeMenuItem(frame, "Load World", "Load a continent from an existing file.", (sender, args) =>
                StateManager.PushState(new WorldLoaderState(Game, StateManager)));

            MakeMenuItem(frame, "Debug World", "Create a debug world.", (sender, args) => 
                StateManager.PushState(new NewGameCreateDebugWorldState(Game, StateManager)));

            MakeMenuItem(frame, "Back", "Go back to main menu.", (sender, args) => 
                StateManager.PopState());

            GuiRoot.RootItem.Layout();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();
            GuiRoot = new Gui.Root(DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            MakeMenu();
            IsInitialized = true;
            
            base.OnEnter();
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