using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp.Gui
{
    public class RenderData
    {
        public GraphicsDevice Device;
        public ContentManager Content;

        public Point ActualScreenBounds
        {
            get
            {
                if (Device != null)
                    return new Point(Device.Viewport.Width, Device.Viewport.Height);
                throw new InvalidOperationException("Graphics device was null.");
            }
        }

        public Effect Effect { get; private set; }
        public Rectangle VirtualScreen { get; private set; }
        public Rectangle RealScreen { get; private set; }
        public int ScaleRatio { get { return CalculateScale(); } }

        public int CalculateScale()
        {
            if (!DwarfCorp.GameSettings.Current.GuiAutoScale)
                return DwarfCorp.GameSettings.Current.GuiScale;

            float scaleX = ActualScreenBounds.X/1920.0f;
            float scaleY = ActualScreenBounds.Y/1080.0f;
            float maxScale = Math.Max(scaleX, scaleY);
            var scale = MathFunctions.Clamp((int)Math.Ceiling(maxScale), 1, 10);
            GameSettings.Current.GuiScale = scale;
            return scale;
        }

        public RenderData(GraphicsDevice Device, ContentManager Content)
        {
            this.Device = Device;
            this.Content = Content;
            this.Effect = Content.Load<Effect>(ContentPaths.GUI.Shader);
            CalculateScreenSize();
        }

        public void CalculateScreenSize()
        {
            VirtualScreen = new Rectangle(0, 0, ActualScreenBounds.X / ScaleRatio, ActualScreenBounds.Y / ScaleRatio);
            RealScreen = new Rectangle(0, 0, VirtualScreen.Width * ScaleRatio, VirtualScreen.Height * ScaleRatio);
            RealScreen = new Rectangle((ActualScreenBounds.X - RealScreen.Width) / 2, (ActualScreenBounds.Y - RealScreen.Height) / 2, RealScreen.Width, RealScreen.Height);
        }
    }
}
