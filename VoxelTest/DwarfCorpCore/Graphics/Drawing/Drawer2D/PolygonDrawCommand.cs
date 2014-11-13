using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
  
    public class PolygonDrawCommand : DrawCommand2D
    {

        public List<Vector2> Points { get; set; }
        public bool IsClosed { get; set; }
        public Color LineColor { get; set; }
        public int LineWidth { get; set; }

        public PolygonDrawCommand()
        {
            Points = new List<Vector2>();
            IsClosed = true;
        }

        public PolygonDrawCommand(List<Vector2> points, bool isClosed, Color lineColor, int lineWidth)
        {
            Points = points;
            IsClosed = isClosed;
            LineColor = lineColor;
            LineWidth = lineWidth;
        }

        public PolygonDrawCommand(List<Vector3> points, bool isClosed, Color lineColor, int lineWidth)
        {
            Points = new List<Vector2>();

            BoundingFrustum cameraFrustrum = PlayState.Camera.GetFrustrum();
            foreach(Vector3 point in points)
            {
                if(cameraFrustrum.Contains(point) == ContainmentType.Contains)
                {
                    Vector3 screenVec = GameState.Game.GraphicsDevice.Viewport.Project(point, PlayState.Camera.ProjectionMatrix, PlayState.Camera.ViewMatrix, Matrix.Identity);
                    Points.Add(new Vector2(screenVec.X, screenVec.Y));
                }
            }

            IsClosed = isClosed;
            LineColor = lineColor;
            LineWidth = lineWidth;
        }

        public override void Render(SpriteBatch batch, Camera camera, Viewport viewport)
        {
            if(Points.Count <= 1)
            {
                return;
            }

            for(int i = 0; i < Points.Count - 1; i++)
            {
                Drawer2D.DrawLine(batch, Points[i], Points[i + 1], LineColor, LineWidth);
            }

            if(IsClosed && Points.Count > 2)
            {
                Drawer2D.DrawLine(batch, Points.Last(), Points.First(), LineColor, LineWidth);
            }
        }
    }
}
