// PolygonDrawCommand.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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

        public PolygonDrawCommand(Camera camera, IEnumerable<Vector3> points, bool isClosed, Color lineColor, int lineWidth)
        {
            Points = new List<Vector2>();

            BoundingFrustum cameraFrustrum = camera.GetFrustrum();
            foreach(Vector3 point in points)
            {
                if(cameraFrustrum.Contains(point) == ContainmentType.Contains)
                {
                    Vector3 screenVec = GameState.Game.GraphicsDevice.Viewport.Project(point, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
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
