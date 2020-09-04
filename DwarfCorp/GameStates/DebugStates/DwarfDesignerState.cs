using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using System.Linq;

namespace DwarfCorp.GameStates.Debug
{
    public class DwarfDesignerState : GameState
    {
        private Gui.Root GuiRoot;
        private DwarfCorp.Gui.Widgets.EmployeePortrait SpriteFrame;
        private List<Animation> Animations = new List<Animation>();

        public DwarfDesignerState(DwarfGame game) :
            base(game)
        {
        }

        private static DwarfSprites.Layer GetLayer(String Type, String Name)
        {
            return DwarfSprites.LayerLibrary.EnumerateLayersOfType(Type).Where(l => l.Names[0] == Name).FirstOrDefault();
        }

        private static DwarfSprites.Palette GetPalette(String Name)
        {
            if (Name == "base palette")
                return DwarfSprites.LayerLibrary.BasePalette;
            return DwarfSprites.LayerLibrary.EnumeratePalettes().First(p => p.Name == Name);
        }

        private void AddSelector(Gui.Widget Panel, String LayerType)
        {
            var panel = Panel.AddChild(new Widget
            {
                MinimumSize = new Point(0, 44),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(2, 2, 2, 2)
            });

            panel.AddChild(new Widget
            {
                Text = LayerType.ToString(),
                MinimumSize = new Point(64, 0),
                AutoLayout = AutoLayout.DockLeft,
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Center,
            });

            var layerCombo = panel.AddChild(new Gui.Widgets.ComboBox
            {
                Items = DwarfSprites.LayerLibrary.EnumerateLayersOfType(LayerType).Select(l => l.Names[0]).ToList(),
                Border = "border-thin",
                AutoLayout = AutoLayout.DockTop,
                ItemsVisibleInPopup = 12,
            }) as Gui.Widgets.ComboBox;

            var paletteCombo = panel.AddChild(new Gui.Widgets.ComboBox
            {
                Items = DwarfSprites.LayerLibrary.EnumeratePalettes().Select(p => p.Name).ToList(),
                Border = "border-thin",
                AutoLayout = AutoLayout.DockTop,
                ItemsVisibleInPopup = 12,
            }) as Gui.Widgets.ComboBox;

            layerCombo.SelectedIndex = 0;
            paletteCombo.SelectedIndex = 0;

            layerCombo.OnSelectedIndexChanged = (sender) =>
            {
                var layer = (panel.Children[1] as Gui.Widgets.ComboBox).SelectedItem;
                var palette = (panel.Children[2] as Gui.Widgets.ComboBox).SelectedItem;

                SpriteFrame.Sprite.AddLayer(GetLayer(LayerType, layer), GetPalette(palette));
            };

            paletteCombo.OnSelectedIndexChanged = (sender) =>
            {
                var layer = (panel.Children[1] as Gui.Widgets.ComboBox).SelectedItem;
                var palette = (panel.Children[2] as Gui.Widgets.ComboBox).SelectedItem;

                SpriteFrame.Sprite.AddLayer(GetLayer(LayerType, layer), GetPalette(palette));
            };

            layerCombo.SelectedIndex = 0;
            paletteCombo.SelectedIndex = 0;
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();
   

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);

            var panel = GuiRoot.RootItem.AddChild(new Widget
            {
                AutoLayout = AutoLayout.FloatCenter,
                MinimumSize = new Point((int)(GuiRoot.RenderData.VirtualScreen.Width * 0.75f), (int)(GuiRoot.RenderData.VirtualScreen.Height * 0.75f)),
                Border = "border-fancy"
            });

            panel.AddChild(new Widget
            {
                Text = "Exit Designer",
                Border = "border-button",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) => GameStateManager.PopState()
            });

            SpriteFrame = panel.AddChild(new DwarfCorp.Gui.Widgets.EmployeePortrait
            {
                MinimumSize = new Point(48 * 6, 40 * 6),
                MaximumSize = new Point(48 * 6, 40 * 6),
                AutoLayout = AutoLayout.DockLeft
            }) as DwarfCorp.Gui.Widgets.EmployeePortrait;

            SpriteFrame.Sprite = new DwarfSprites.LayerStack();
            foreach (var animation in Library.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations))
            {
                animation.Value.Loops = true;
                Animations.Add(animation.Value);
            }

            SpriteFrame.AnimationPlayer = new AnimationPlayer();
            SpriteFrame.AnimationPlayer.ChangeAnimation(Animations[0], AnimationPlayer.ChangeAnimationOptions.ResetAndPlay);

            foreach (var layerType in DwarfSprites.LayerLibrary.EnumerateLayerTypes().OrderBy(l => l.Precedence))
                AddSelector(panel, layerType.Name);

            var anim = panel.AddChild(new Widget
            {
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(2, 2, 2, 2)
            });

            anim.AddChild(new Widget
            {
                Text = "animation",
                MinimumSize = new Point(64, 0),
                AutoLayout = AutoLayout.DockLeft,
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Center
            });

            var animCombo = anim.AddChild(new Gui.Widgets.ComboBox
            {
                Items = Animations.Select(a => a.Name).ToList(),
                Border = "border-thin",
                AutoLayout = AutoLayout.DockTop,
                OnSelectedIndexChanged = (sender) => SpriteFrame.AnimationPlayer.ChangeAnimation(Animations.First(a => a.Name == (sender as Gui.Widgets.ComboBox).SelectedItem),
                AnimationPlayer.ChangeAnimationOptions.ResetAndPlay),
                ItemsVisibleInPopup = 12,
            }) as Gui.Widgets.ComboBox;

            animCombo.SelectedIndex = animCombo.Items.IndexOf("WalkingFORWARD");

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

            SpriteFrame.AnimationPlayer.Update(gameTime, false, Timer.TimerMode.Real);
            SpriteFrame.Sprite.Update(Game.GraphicsDevice);
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