using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using System.Linq;

namespace DwarfCorp.Gui.Widgets
{
    public class EmployeePortrait : Widget
    {
        private Gui.Mesh SpriteMesh;
        public DwarfCorp.LayeredSprites.LayerStack Sprite;
        public AnimationPlayer AnimationPlayer;

        public override void Layout()
        {
            base.Layout();
            int x = 32;
            int y = 40;
            float ratio = Math.Max((float)Rect.Height / y, 1.0f);

            SpriteMesh = Gui.Mesh.Quad()
                .Scale((ratio * x), (ratio * y))
                .Translate(Rect.X, Rect.Y);
        }

        public override void PostDraw(GraphicsDevice device)
        {
            if (Hidden || Transparent)
                return;

            if (IsAnyParentHidden())
                return;

            if (Sprite == null)
            {
                return;
            }
            var texture = Sprite.GetCompositeTexture();
            if (texture != null)
            {
                var sheet = new SpriteSheet(texture, 32, 40);
                SpriteMesh.ResetQuadTexture();
                var frame = AnimationPlayer.GetCurrentAnimation().Frames[AnimationPlayer.CurrentFrame];
                SpriteMesh.Texture(sheet.TileMatrix(frame.X, frame.Y));
                Root.DrawMesh(SpriteMesh, texture);
            }

            base.PostDraw(device);
        }
    }

}

namespace DwarfCorp.GameStates
{
    public class DwarfDesignerState : GameState
    {
        private Gui.Root GuiRoot;
        private DwarfCorp.Gui.Widgets.EmployeePortrait SpriteFrame;
        private List<LayeredSprites.LayeredAnimationProxy> Animations = new List<LayeredSprites.LayeredAnimationProxy>();

        public DwarfDesignerState(DwarfGame game, GameStateManager stateManager) :
            base(game, "GuiDebugState", stateManager)
        {
        }

        private static LayeredSprites.Layer GetLayer(String Type, String Name)
        {
            return LayeredSprites.LayerLibrary.EnumerateLayers(Type).Where(l => l.Asset == Name).FirstOrDefault();
        }

        private static LayeredSprites.Palette GetPalette(String Name)
        {
            if (Name == "base palette")
                return LayeredSprites.LayerLibrary.BaseDwarfPalette;
            return LayeredSprites.LayerLibrary.EnumeratePalettes().First(p => p.Asset == Name);
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
                Text = LayerType,
                MinimumSize = new Point(64, 0),
                AutoLayout = AutoLayout.DockLeft,
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Center,
            });

            var layerCombo = panel.AddChild(new Gui.Widgets.ComboBox
            {
                Items = LayeredSprites.LayerLibrary.EnumerateLayers(LayerType).Select(l => l.Asset).ToList(),
                Border = "border-thin",
                AutoLayout = AutoLayout.DockTop,
                ItemsVisibleInPopup = 12,
            }) as Gui.Widgets.ComboBox;

            var paletteCombo = panel.AddChild(new Gui.Widgets.ComboBox
            {
                Items = new string[] { "base palette" }.Concat(LayeredSprites.LayerLibrary.EnumeratePalettes().Select(p => p.Asset)).ToList(),
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
                OnClick = (sender, args) => StateManager.PopState()
            });

            SpriteFrame = panel.AddChild(new DwarfCorp.Gui.Widgets.EmployeePortrait
            {
                MinimumSize = new Point(256, 320),
                MaximumSize = new Point(256, 320),
                AutoLayout = AutoLayout.DockLeft
            }) as DwarfCorp.Gui.Widgets.EmployeePortrait;

            SpriteFrame.Sprite = new LayeredSprites.LayerStack();
            foreach (Animation animation in AnimationLibrary.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations))
            {
                var proxyAnim = SpriteFrame.Sprite.ProxyAnimation(animation);
                proxyAnim.Loops = true;
                Animations.Add(proxyAnim);
            }

            SpriteFrame.AnimationPlayer = new AnimationPlayer();
            SpriteFrame.AnimationPlayer.ChangeAnimation(Animations[0], AnimationPlayer.ChangeAnimationOptions.ResetAndPlay);

            AddSelector(panel, "body");
            AddSelector(panel, "face");
            AddSelector(panel, "nose");
            AddSelector(panel, "beard");
            AddSelector(panel, "hair");
            AddSelector(panel, "tool");

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
            SpriteFrame.Sprite.Update(StateManager.Game.GraphicsDevice);
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