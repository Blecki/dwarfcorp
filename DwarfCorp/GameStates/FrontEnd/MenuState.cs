using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{
    public class MenuState : GameState
    {
        private Gui.Root GuiRoot;
        private Widget LogoWidget;
        private Texture2D LogoTexture;

        public MenuState(DwarfGame game) :
            base(game)
        {
       
        }

        protected Gui.Widget CreateMenu(String Name)
        {
            GuiRoot.RootItem.Clear();

            LogoWidget = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(600, 348),
                Transparent = true,
                AutoLayout = Gui.AutoLayout.FloatTop
            });

            return GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                MinimumSize = new Point(400, 280),
                Font = "font18-outline",
                Border = "basic",
                Background = new TileReference("sbasic", 0),
                BackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.25f),
                AutoLayout = Gui.AutoLayout.FloatBottom,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                Text = Name,
                InteriorMargin = new Gui.Margin(24,0,0,0),
                Padding = new Gui.Margin(2, 2, 2, 2),
                TextColor = Color.White.ToVector4()
            });
        }

        protected Gui.Widget CreateMenuItem(Gui.Widget Menu, string Name, string Tooltip, Action<Gui.Widget, Gui.InputEventArgs> OnClick)
        {
            return Menu.AddChild(new Gui.Widgets.Button
            {
                AutoLayout = Gui.AutoLayout.DockTop,
                Text = Name,
                Border = "none",
                OnClick = OnClick,
                Tooltip = Tooltip,
                TextHorizontalAlign = Gui.HorizontalAlign.Center,
                TextVerticalAlign = Gui.VerticalAlign.Center,
                Font = "font18-outline",
                TextColor = Color.White.ToVector4(),
                HoverTextColor = GameSettings.Current.Colors.GetColor("Highlight", Color.DarkRed).ToVector4()
            });
        }

        protected void FinishMenu()
        {
            GuiRoot.RootItem.AddChild(new Widget()
            {
                Font = "font8",
                TextColor = new Vector4(1, 1, 1, 0.5f),
                AutoLayout = AutoLayout.FloatBottomRight,
#if DEMO
                Text = "DwarfCorp " + Program.Version + " (DEMO)  "
#else
                Text = "DwarfCorp " + Program.Version + " (" + Program.Commit + ")"
#endif
            });

            GuiRoot.RootItem.Layout();
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            LogoTexture = AssetManager.GetContentTexture("newgui/gamelogo");

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);

            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
                GuiRoot.HandleInput(@event.Message, @event.Args);

            GuiRoot.Update(gameTime.ToRealTime());
            SoundManager.Update(gameTime, null, null);
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            if (LogoTexture.IsDisposed)
                LogoTexture = AssetManager.GetContentTexture("newgui/gamelogo");

            GuiRoot.DrawMesh(
                Gui.Mesh.Quad()
                .Scale(LogoWidget.Rect.Width, LogoWidget.Rect.Height)
                .Translate(LogoWidget.Rect.X, LogoWidget.Rect.Y),
                LogoTexture);
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}
