using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{
    public class NewGameChooseWorldState : GameState
    {
        private Gum.Root GuiRoot;

        public NewGameChooseWorldState(DwarfGame game, GameStateManager stateManager) :
            base(game, "MainMenuState", stateManager)
        {
        }

        private Gum.Widget MakeMenuFrame(String Name)
        {
            GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(600, 348),
                Background = new Gum.TileReference("logo", 0),
                AutoLayout = Gum.AutoLayout.FloatTop,
            });

            return GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                MinimumSize = new Point(256, 200),
                Border = "border-fancy",
                AutoLayout = Gum.AutoLayout.FloatBottom,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                Text = Name,
                InteriorMargin = new Gum.Margin(12,0,0,0),
                Padding = new Gum.Margin(2, 2, 2, 2)
            });
        }

        private void MakeMenuItem(Gum.Widget Menu, string Name, string Tooltip, Action<Gum.Widget, Gum.InputEventArgs> OnClick)
        {
            Menu.AddChild(new Gum.Widgets.Button
            {
                AutoLayout = Gum.AutoLayout.DockTop,
                Border = "border-thin",
                Text = Name,
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gum.HorizontalAlign.Center,
                TextVerticalAlign = Gum.VerticalAlign.Center
            });
        }

        public void MakeMenu()
        {
            GuiRoot.RootItem.Clear();

            var frame = MakeMenuFrame("PLAY DWARFCORP");

            MakeMenuItem(frame, "Generate World", "Create a new world from scratch.", (sender, args) =>
                StateManager.PushState(new WorldGeneratorState(Game, StateManager)));

            MakeMenuItem(frame, "Generate World (new)", "Create a new world from scratch.", (sender, args) =>
                StateManager.PushState(new NewWorldGeneratorState(Game, StateManager)));

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
            GuiRoot = new Gum.Root(Gum.Root.MinimumSize, DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);
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