using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.Gui;

namespace DwarfCorp
{
    public class WorldPopup
    {
        public Widget Widget;
        public GameComponent BodyToTrack;
        public Vector2 ScreenOffset;

        public void Update(DwarfTime time, Camera camera, Viewport viewport)
        {
            if (Widget == null || BodyToTrack == null || BodyToTrack.IsDead)
                return;

            var projectedPosition = viewport.Project(BodyToTrack.Position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            if (projectedPosition.Z > 0.999f)
            {
                Widget.Hidden = true;
                return;
            }

            var projectedCenter = new Vector2(projectedPosition.X / DwarfGame.GuiSkin.CalculateScale(), projectedPosition.Y / DwarfGame.GuiSkin.CalculateScale()) + ScreenOffset - new Vector2(0, Widget.Rect.Height);
            if ((new Vector2(Widget.Rect.Center.X, Widget.Rect.Center.Y) - projectedCenter).Length() < 0.1f)
                return;

            Widget.Rect = new Rectangle((int)projectedCenter.X - Widget.Rect.Width / 2,
                (int)projectedCenter.Y - Widget.Rect.Height / 2, Widget.Rect.Width, Widget.Rect.Height);

            if (!viewport.Bounds.Intersects(Widget.Rect))
                Widget.Hidden = true;
            else
                Widget.Hidden = false;

            Widget.Layout();
            Widget.Invalidate();
        }
    }
}