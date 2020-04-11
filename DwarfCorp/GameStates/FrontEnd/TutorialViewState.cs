﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;
using System.Text;
using System;

namespace DwarfCorp.GameStates
{ 
    public class TutorialIcon : Widget
    {
        public string ImageSource;
        
        public override void PostDraw(GraphicsDevice device)
        {
            if (IsAnyParentHidden())
            {
                return;
            }
            if (!String.IsNullOrEmpty(ImageSource) && AssetManager.DoesTextureExist(ImageSource))
            { 
                var texture = AssetManager.GetContentTexture(ImageSource);
                DwarfGame.SpriteBatch.Begin();
                var interior = GetDrawableInterior();

                interior.X *= Root.RenderData.ScaleRatio;
                interior.Y *= Root.RenderData.ScaleRatio;
                interior.Width *= Root.RenderData.ScaleRatio;
                interior.Height *= Root.RenderData.ScaleRatio;

                DwarfGame.SpriteBatch.Draw(texture, interior, Color.White);
                DwarfGame.SpriteBatch.End();
            }
            base.PostDraw(device);
        }
    }

    public class TutorialViewState : GameState
    {
        private Gui.Root GuiRoot;
        private Gui.Widget mainPanel;
        WorldManager World;
        public TutorialViewState(DwarfGame game, WorldManager world) :
            base(game)
        {
            World = world;
        }

        public void Reset()
        {
            mainPanel.Clear();
            Rectangle rect = GuiRoot.RenderData.VirtualScreen;

            mainPanel.AddChild(new Gui.Widgets.Button
            {
                Text = "< Back",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                Font = "font16",
                OnClick = (sender, args) =>
                {
                    GameStateManager.PopState();
                },
                AutoLayout = AutoLayout.FloatBottomLeft,
            });
            var interior = mainPanel.GetDrawableInterior();

            var widgetList = mainPanel.AddChild(new WidgetListView()
            {
                Font = "font10",
                AutoLayout = AutoLayout.DockLeft,
                ItemHeight = 32,
                MinimumSize = new Point(interior.Width / 2, interior.Height - 128),
                MaximumSize = new Point(interior.Width / 2, interior.Height - 128),
                ChangeColorOnHover = true,
                ChangeColorOnSelected = true
            }) as WidgetListView;

            var detailsPanel = mainPanel.AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(interior.Width / 2, interior.Height - 128)
            });



            var icon = detailsPanel.AddChild(new TutorialIcon()
            {
                MinimumSize = new Point(256, 128),
                MaximumSize = new Point(256, 128),
                AutoLayout = AutoLayout.DockTop
            }) as TutorialIcon;


            var title = detailsPanel.AddChild(new Widget()
            {
                Font = "font16",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(256, 32),
                TextVerticalAlign = VerticalAlign.Center
            });

            var details = detailsPanel.AddChild(new Widget()
            {
                Font = "font10",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(256, 256)
            });


            foreach (var tutorial in World.TutorialManager.EnumerateTutorials())
            {
                widgetList.AddItem(new Widget()
                {
                    Background = new TileReference("basic", 0),
                    Text = tutorial.Value.Title,
                    OnClick = (sender, args) =>
                    {
                        details.Text = tutorial.Value.Text;
                        title.Text = tutorial.Value.Title;
                        var asset = "newgui\\tutorials\\" + tutorial.Key;
                        icon.ImageSource = AssetManager.DoesTextureExist(asset) ? asset : null;

                        if (icon.ImageSource == null && tutorial.Value.Icon != null)
                        {
                            icon.Background = null;// tutorial.Value.Icon;
                            icon.MinimumSize = new Point(128, 128);
                            icon.MaximumSize = new Point(128, 128);
                        }
                        else
                        {
                            icon.MinimumSize = new Point(256, 128);
                            icon.MaximumSize = new Point(256, 128);
                            icon.Background = null;
                        }
                       
                        icon.Invalidate();
                        icon.Parent.Layout();
                    },
                    TextVerticalAlign = VerticalAlign.Center,
                });
            }

            mainPanel.Layout();
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            var rect = GuiRoot.RenderData.VirtualScreen;
            mainPanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = rect,
                MinimumSize = new Point(3 * GuiRoot.RenderData.VirtualScreen.Width / 4, 3 * GuiRoot.RenderData.VirtualScreen.Height / 4),
                AutoLayout = AutoLayout.FloatCenter,
                Border = "border-fancy",
                Padding = new Margin(4, 4, 4, 4),
                InteriorMargin = new Margin(2, 0, 0, 0),
                TextSize = 1,
                Font = "font10"
            });
            Reset();

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