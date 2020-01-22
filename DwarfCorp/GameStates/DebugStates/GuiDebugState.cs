using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using System.Linq;

namespace DwarfCorp.GameStates.Debug
{
    public class GuiDebugState : GameState
    {
        private Gui.Root GuiRoot;
        private Widget AtlasPanel;

        public GuiDebugState(DwarfGame game) :
            base(game)
        {
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();   

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);

            var panel = GuiRoot.RootItem.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill,
            });

            panel.AddChild(new Widget
            {
                Text = "Exit",
                Border = "border-button",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) => GameStateManager.PopState(),
                TextSize = 2,
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center
            });

            AtlasPanel = panel.AddChild(new GuiDebugPanel
            {
                AutoLayout = AutoLayout.FloatCenter,
                MinimumSize = new Point((int)(GuiRoot.RenderData.VirtualScreen.Width * 0.9f), (int)(GuiRoot.RenderData.VirtualScreen.Height * 0.9f)),
            });

            GuiRoot.RootItem.Layout();

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

            GuiRoot.Update(gameTime.ToRealTime());

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }
}