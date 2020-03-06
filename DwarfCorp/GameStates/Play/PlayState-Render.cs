using System.IO;
using System.Net.Mime;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    public partial class PlayState : GameState
    {
        /// <summary>
        /// Called when a frame is to be drawn to the screen
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void Render(DwarfTime gameTime)
        {
            Game.Graphics.GraphicsDevice.SetRenderTarget(null);
            Game.Graphics.GraphicsDevice.Clear(Color.Black);
            EnableScreensaver = !World.ShowingWorld;

            if (World.ShowingWorld)
            {
                World.Renderer.ValidateShader();

                if (!MinimapFrame.Hidden && !Gui.RootItem.Hidden)
                    MinimapRenderer.PreRender(DwarfGame.SpriteBatch);

                World.Renderer.Render(gameTime);

                CurrentTool.Render3D(Game, gameTime);
                VoxSelector.Render();

                foreach (var obj in SelectedObjects)
                    if (obj.IsVisible && !obj.IsDead)
                        Drawer3D.DrawBox(obj.GetBoundingBox(), Color.White, 0.01f, true);

                CurrentTool.Render2D(Game, gameTime);

                foreach (CreatureAI creature in World.PersistentData.SelectedMinions)
                {
                    foreach (Task task in creature.Tasks)
                        if (task.IsFeasible(creature.Creature) == Feasibility.Feasible)
                            task.Render(gameTime);

                    if (creature.CurrentTask.HasValue(out var currentTask))
                        currentTask.Render(gameTime);
                }

                DwarfGame.SpriteBatch.Begin();
                BodySelector.Render(DwarfGame.SpriteBatch);
                DwarfGame.SpriteBatch.End();

                if (Gui.RenderData.RealScreen.Width != Gui.RenderData.Device.Viewport.Width || Gui.RenderData.RealScreen.Height != Gui.RenderData.Device.Viewport.Height)
                {
                    Gui.RenderData.CalculateScreenSize();
                    Gui.RootItem.Rect = Gui.RenderData.VirtualScreen;
                    Gui.ResetGui();
                    CreateGUIComponents();
                }

                if (!MinimapFrame.Hidden && !Gui.RootItem.Hidden)
                {
                    Gui.Draw(new Point(0, 0), false);
                    MinimapRenderer.Render(new Rectangle(MinimapFrame.Rect.X, MinimapFrame.Rect.Bottom - 192, 192, 192), Gui);
                    Gui.DrawMesh(MinimapFrame.GetRenderMesh(), Gui.SpriteAtlas.Texture);
                    Gui.RedrawPopups();
                    Gui.DrawMouse();
                }
                else
                    Gui.Draw();
            }

            base.Render(gameTime);
        }

        /// <summary>
        /// If the game is not loaded yet, just draws a loading message centered
        /// </summary>
        /// <param name="gameTime">The current time</param>
        public override void RenderUnitialized(DwarfTime gameTime)
        {
            EnableScreensaver = true;
            World.Renderer.Render(gameTime);
            base.RenderUnitialized(gameTime);
        }
    }
}
