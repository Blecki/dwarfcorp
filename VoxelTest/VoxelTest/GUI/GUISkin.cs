using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class GUISkin
    {
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public Texture2D Texture { get; set; }
        public Dictionary<string, Point> Frames { get; set; }

        public static string PanelUpperLeft = "PanelUpperLeft";
        public static string PanelUpperRight = "PanelUpperRight";
        public static string PanelLowerLeft = "PanelLowerLeft";
        public static string PanelLowerRight = "PanelLowerRight";
        public static string PanelLeft = "PanelLeft";
        public static string PanelRight = "PanelRight";
        public static string PanelUpper = "PanelUpper";
        public static string PanelLower = "PanelLower";
        public static string PanelCenter = "PanelCenter";
        public static string CheckboxUnchecked = "CheckboxUnchecked";
        public static string CheckboxChecked = "CheckboxChecked";
        public static string Radiobutton = "Radiobutton";
        public static string RadiobuttonPushed = "RadiobuttonPushed";
        public static string ButtonUpperLeft = "ButtonUpperLeft";
        public static string ButtonUpperRight = "ButtonUpperRight";
        public static string ButtonLowerLeft = "ButtonLowerLeft";
        public static string ButtonLowerRight = "ButtonLowerRight";
        public static string ButtonLeft = "ButtonLeft";
        public static string ButtonRight = "ButtonRight";
        public static string ButtonUpper = "ButtonUpper";
        public static string ButtonLower = "ButtonLower";
        public static string ButtonCenter = "ButtonCenter";
        public static string Track = "Track";
        public static string TrackVert = "TrackVertical";
        public static string SliderTex = "Slider";
        public static string SliderVertical = "SliderVertical";
        public static string FieldLeft = "FieldLeft";
        public static string FieldRight = "FieldRight";
        public static string FieldCenter = "FieldCenter";
        public static string DownArrow = "DownArrow";
        public static string GroupUpperLeft = "GroupUpperLeft";
        public static string GroupUpper = "GroupUpper";
        public static string GroupUpperRight = "GroupUpperRight";
        public static string GroupLeft = "GroupLeft";
        public static string GroupRight = "GroupRight";
        public static string GroupLower = "GroupLower";
        public static string GroupLowerRight = "GroupLowerRight";
        public static string GroupLowerLeft = "GroupLowerLeft";
        public static string ProgressLeft = "ProgressLeft";
        public static string ProgressFilled = "ProgressFilled";
        public static string ProgressEmpty = "ProgressEmpty";
        public static string ProgressCap = "ProgressCap";
        public static string ProgressRight = "ProgressRight";
        public static string Check = "Check";
        public static string Ex = "X";
        public static string RightArrow = "->";
        public static string LeftArrow = "<-";
        public static string Save = "Save";

        public GUISkin(Texture2D texture, int tileWidth, int tileHeight)
        {
            Texture = texture;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            Frames = new Dictionary<string, Point>();
        }

        public Rectangle GetSourceRect(Point p)
        {
            return new Rectangle(p.X * TileWidth, p.Y * TileHeight, TileWidth, TileHeight);
        }

        public ImageFrame GetSpecialFrame(string key)
        {
            return new ImageFrame(Texture, GetSourceRect(Frames[key]));
        }

        public Rectangle GetSourceRect(string s)
        {
            return GetSourceRect(Frames[s]);
        }

        public void SetDefaults()
        {
            Frames[PanelUpperLeft] = new Point(0, 0);
            Frames[PanelUpper] = new Point(1, 0);
            Frames[PanelUpperRight] = new Point(2, 0);
            Frames[PanelLeft] = new Point(0, 1);
            Frames[PanelCenter] = new Point(1, 1);
            Frames[PanelRight] = new Point(2, 1);
            Frames[PanelLowerLeft] = new Point(0, 2);
            Frames[PanelLower] = new Point(1, 2);
            Frames[PanelLowerRight] = new Point(2, 2);

            Frames[CheckboxUnchecked] = new Point(3, 0);
            Frames[CheckboxChecked] = new Point(3, 1);

            Frames[Radiobutton] = new Point(4, 0);
            Frames[RadiobuttonPushed] = new Point(4, 1);

            Frames[ButtonUpperLeft] = new Point(5, 0);
            Frames[ButtonUpper] = new Point(6, 0);
            Frames[ButtonUpperRight] = new Point(7, 0);
            Frames[ButtonLeft] = new Point(5, 1);
            Frames[ButtonCenter] = new Point(6, 1);
            Frames[ButtonRight] = new Point(7, 1);
            Frames[ButtonLowerLeft] = new Point(5, 2);
            Frames[ButtonLower] = new Point(6, 2);
            Frames[ButtonLowerRight] = new Point(7, 2);

            Frames[FieldLeft] = new Point(3, 3);
            Frames[FieldCenter] = new Point(4, 3);
            Frames[FieldRight] = new Point(5, 3);

            Frames[DownArrow] = new Point(6, 3);

            Frames[Track] = new Point(3, 4);
            Frames[SliderTex] = new Point(4, 4);
            Frames[TrackVert] = new Point(5, 4);
            Frames[SliderVertical] = new Point(6, 4);

            Frames[GroupUpperLeft] = new Point(0, 6);
            Frames[GroupUpper] = new Point(1, 6);
            Frames[GroupUpperRight] = new Point(2, 6);
            Frames[GroupLeft] = new Point(0, 7);
            Frames[GroupRight] = new Point(2, 7);
            Frames[GroupLowerLeft] = new Point(0, 8);
            Frames[GroupLower] = new Point(1, 8);
            Frames[GroupLowerRight] = new Point(2, 8);

            Frames[ProgressLeft] = new Point(3, 6);
            Frames[ProgressFilled] = new Point(7, 6);
            Frames[ProgressCap] = new Point(6, 6);
            Frames[ProgressEmpty] = new Point(5, 6);
            Frames[ProgressRight] = new Point(4, 6);

            Frames[Check] = new Point(3, 7);
            Frames[Ex] = new Point(4, 7);
            Frames[Save] = new Point(3, 8);
            Frames[LeftArrow] = new Point(4, 8);
            Frames[RightArrow] = new Point(5, 8);
        }

        public void RenderPanel(Rectangle rectbounds, SpriteBatch spriteBatch)
        {
            Rectangle rect = new Rectangle((int) (rectbounds.X + TileWidth / 4), (int) (rectbounds.Y + TileHeight / 4), rectbounds.Width - TileWidth / 2, rectbounds.Height - TileHeight / 2);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(PanelUpperLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(PanelLowerLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(PanelUpperRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(PanelLowerRight), Color.White);

            int maxX = rect.X + rect.Width;
            int diffX = rect.Width % TileWidth;
            int maxY = rect.Y + rect.Height;
            int diffY = rect.Height % TileHeight;
            int right = maxX - diffX - TileWidth;
            int bottom = maxY - diffY - TileHeight;
            int left = rect.X;
            int top = rect.Y;

            for(int x = left; x <= right; x += TileWidth)
            {
                spriteBatch.Draw(Texture, new Rectangle(x, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(PanelUpper), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, rect.Y - TileHeight, diffX, TileHeight), GetSourceRect(PanelUpper), Color.White);

            for(int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, y, TileWidth, TileHeight), GetSourceRect(PanelLeft), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, maxY - diffY, TileWidth, diffY), GetSourceRect(PanelLeft), Color.White);

            for(int x = left; x <= right; x += TileWidth)
            {
                spriteBatch.Draw(Texture, new Rectangle(x, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(PanelLower), Color.White);
            }


            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, rect.Y + rect.Height, diffX, TileHeight), GetSourceRect(PanelLower), Color.White);

            for(int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, y, TileWidth, TileHeight), GetSourceRect(PanelRight), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, maxY - diffY, TileWidth, diffY), GetSourceRect(PanelRight), Color.White);

            for(int x = left; x <= right; x += TileWidth)
            {
                for(int y = top; y <= bottom; y += TileHeight)
                {
                    spriteBatch.Draw(Texture, new Rectangle(x, y, TileWidth, TileHeight), GetSourceRect(PanelCenter), Color.White);
                }
                spriteBatch.Draw(Texture, new Rectangle(x, maxY - diffY, TileWidth, diffY), GetSourceRect(PanelCenter), Color.White);
            }

            for(int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, y, diffX, TileHeight), GetSourceRect(PanelCenter), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, maxY - diffY, diffX, diffY), GetSourceRect(PanelCenter), Color.White);
        }

        public void RenderButton(Rectangle rectbounds, SpriteBatch spriteBatch)
        {
            int w = Math.Max(rectbounds.Width - TileWidth / 4, TileWidth / 4);
            int h = Math.Max(rectbounds.Height - TileHeight / 4, TileHeight / 4);
            Rectangle rect = new Rectangle((int) (rectbounds.X + TileWidth / 8),
                (int) (rectbounds.Y + TileHeight / 8),
                w,
                h);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(ButtonUpperLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(ButtonLowerLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(ButtonUpperRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(ButtonLowerRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y - TileHeight, rect.Width, TileHeight), GetSourceRect(ButtonUpper), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y, TileWidth, rect.Height), GetSourceRect(ButtonLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y + rect.Height, rect.Width, TileHeight), GetSourceRect(ButtonLower), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y, TileWidth, rect.Height), GetSourceRect(ButtonRight), Color.White);
            spriteBatch.Draw(Texture, rect, GetSourceRect(ButtonCenter), Color.White);
        }

        public void RenderCheckbox(Rectangle rect, bool checkstate, SpriteBatch spriteBatch)
        {
            if(checkstate)
            {
                spriteBatch.Draw(Texture, rect, GetSourceRect(CheckboxChecked), Color.White);
            }
            else
            {
                spriteBatch.Draw(Texture, rect, GetSourceRect(CheckboxUnchecked), Color.White);
            }
        }

        public void RenderDownArrow(Rectangle rect, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, rect, GetSourceRect(DownArrow), Color.White);
        }

        public void RenderRadioButton(Rectangle rect, bool checkstate, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, rect, checkstate ? GetSourceRect(RadiobuttonPushed) : GetSourceRect(Radiobutton), Color.White);
        }

        public void RenderField(Rectangle rect, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y, TileWidth, TileHeight), GetSourceRect(FieldLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + TileWidth, rect.Y, rect.Width - TileWidth * 2, TileHeight), GetSourceRect(FieldCenter), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.Right - TileWidth, rect.Top, TileWidth, TileHeight), GetSourceRect(FieldRight), Color.White);
        }

        public void RenderProgressBar(Rectangle rectBounds, float progress, SpriteBatch spriteBatch)
        {
            float n = (float) Math.Max(Math.Min(progress, 1.0), 0.0);

            if(n > 0)
            {
                Rectangle drawFillRect = new Rectangle(rectBounds.X + TileWidth / 2 - 8, rectBounds.Y, (int) ((rectBounds.Width - TileWidth / 2 - 4) * n) - 8, rectBounds.Height);
                Rectangle filledRect = GetSourceRect(ProgressFilled);
                filledRect.Width = 1;
                spriteBatch.Draw(Texture, drawFillRect, filledRect, Color.White);

                Rectangle progressRect = GetSourceRect(ProgressCap);
                progressRect.Width = 8;

                Rectangle capRect = new Rectangle((int) ((rectBounds.Width - TileWidth / 2 - 4) * n) + rectBounds.X, rectBounds.Y, 8, rectBounds.Height);
                spriteBatch.Draw(Texture, capRect, progressRect, Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rectBounds.X, rectBounds.Y, TileWidth, rectBounds.Height), GetSourceRect(ProgressLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rectBounds.X + rectBounds.Width - TileWidth, rectBounds.Y, TileWidth, rectBounds.Height), GetSourceRect(ProgressRight), Color.White);

            int steps = (rectBounds.Width - TileWidth) / TileWidth;

            for(int i = 0; i < steps; i++)
            {
                spriteBatch.Draw(Texture, new Rectangle(rectBounds.X + TileWidth / 2 + i * TileWidth, rectBounds.Y, TileWidth, rectBounds.Height), GetSourceRect(ProgressEmpty), Color.White);
            }

            int remainder = (rectBounds.Width - TileWidth) - steps * TileWidth;

            if(remainder > 0)
            {
                spriteBatch.Draw(Texture, new Rectangle(rectBounds.X + TileWidth / 2 + steps * TileWidth, rectBounds.Y, remainder, rectBounds.Height), GetSourceRect(ProgressEmpty), Color.White);
            }
        }

        public void RenderGroup(Rectangle rectbounds, SpriteBatch spriteBatch)
        {
            Rectangle rect = new Rectangle((int) (rectbounds.X + TileWidth / 4), (int) (rectbounds.Y + TileHeight / 4), rectbounds.Width - TileWidth / 2, rectbounds.Height - TileHeight / 2);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(GroupUpperLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(GroupLowerLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y - TileHeight, TileWidth, TileHeight), GetSourceRect(GroupUpperRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y + rect.Height, TileWidth, TileHeight), GetSourceRect(GroupLowerRight), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y - TileHeight, rect.Width, TileHeight), GetSourceRect(GroupUpper), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X - TileWidth, rect.Y, TileWidth, rect.Height), GetSourceRect(GroupLeft), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X, rect.Y + rect.Height, rect.Width, TileHeight), GetSourceRect(GroupLower), Color.White);
            spriteBatch.Draw(Texture, new Rectangle(rect.X + rect.Width, rect.Y, TileWidth, rect.Height), GetSourceRect(GroupRight), Color.White);
        }

        public void RenderSliderVertical(SpriteFont font, Rectangle boundingRect, float value, float minvalue, float maxValue, Slider.SliderMode mode, bool drawLabel, bool invert, SpriteBatch spriteBatch)
        {
            const int padding = 5;

            if(invert)
            {
                value = maxValue - value;
            }


            int fieldSize = Math.Max(Math.Min((int) (0.2f * boundingRect.Width), 150), 64);
            Rectangle rect = new Rectangle(boundingRect.X + boundingRect.Width / 2 - TileWidth / 2, boundingRect.Y + padding, boundingRect.Width, boundingRect.Height - TileHeight - padding * 2);
            Rectangle fieldRect = new Rectangle(boundingRect.Right - fieldSize, boundingRect.Y + boundingRect.Height - TileHeight / 2, fieldSize, TileHeight);

            int maxY = rect.Y + rect.Height;
            int diffY = rect.Height % TileHeight;
            int bottom = maxY;
            int left = rect.X;
            int top = rect.Y;


            for(int y = top; y <= bottom; y += TileHeight)
            {
                spriteBatch.Draw(Texture, new Rectangle(rect.X, y, TileWidth, TileHeight), GetSourceRect(TrackVert), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(rect.X, maxY - diffY, TileWidth, diffY), GetSourceRect(TrackVert), Color.White);

            float d = (value - minvalue) / (maxValue - minvalue);

            int sliderY = (int) ((d) * rect.Height + rect.Y);

            spriteBatch.Draw(Texture, new Rectangle(rect.X, sliderY - TileHeight / 2, TileWidth, TileHeight), GetSourceRect(SliderVertical), Color.White);

            if(!drawLabel)
            {
                return;
            }

            RenderField(fieldRect, spriteBatch);

            if(invert)
            {
                value = -(value - maxValue);
            }

            float v = 0.0f;
            if(mode == Slider.SliderMode.Float)
            {
                v = (float) Math.Round(value, 2);
            }
            else
            {
                v = (int) value;
            }

            string toDraw = "" + v;

            Vector2 origin = Datastructures.SafeMeasure(font, toDraw) * 0.5f;

            Drawer2D.SafeDraw(spriteBatch, toDraw, font, Color.Black, new Vector2(fieldRect.X + fieldRect.Width / 2, fieldRect.Y + 16), origin);
        }

        public void RenderSliderHorizontal(SpriteFont font, Rectangle boundingRect, float value, float minvalue, float maxValue, Slider.SliderMode mode, bool drawLabel, bool invertValue, SpriteBatch spriteBatch)
        {
            const int padding = 5;

            if(invertValue)
            {
                value = maxValue - value;
            }

            int fieldSize = Math.Max(Math.Min((int) (0.2f * boundingRect.Width), 150), 64);
            Rectangle rect = new Rectangle(boundingRect.X + padding, boundingRect.Y + boundingRect.Height / 2 - TileHeight / 2, boundingRect.Width - fieldSize - padding * 2, boundingRect.Height / 2);
            Rectangle fieldRect = new Rectangle(boundingRect.Right - fieldSize, boundingRect.Y + boundingRect.Height / 2 - TileHeight / 2, fieldSize, boundingRect.Height / 2);
            int maxX = rect.X + rect.Width;
            int diffX = rect.Width % TileWidth;
            int right = maxX;
            int left = rect.X;
            int top = rect.Y;


            for(int x = left; x <= right; x += TileWidth)
            {
                spriteBatch.Draw(Texture, new Rectangle(x, rect.Y, TileWidth, TileHeight), GetSourceRect(Track), Color.White);
            }

            spriteBatch.Draw(Texture, new Rectangle(maxX - diffX, rect.Y, diffX, TileHeight), GetSourceRect(Track), Color.White);

            int sliderX = (int) ((value - minvalue) / (maxValue - minvalue) * rect.Width + rect.X);

            spriteBatch.Draw(Texture, new Rectangle(sliderX - TileWidth / 2, rect.Y, TileWidth, TileHeight), GetSourceRect(SliderTex), Color.White);

            if(!drawLabel)
            {
                return;
            }

            RenderField(fieldRect, spriteBatch);

            float v = 0.0f;
            if(invertValue)
            {
                value = value - maxValue;
            }
            if(mode == Slider.SliderMode.Float)
            {
                v = (float) Math.Round(value, 2);
            }
            else
            {
                v = (int) value;
            }

            string toDraw = "" + v;

            Vector2 origin = Datastructures.SafeMeasure(font, toDraw) * 0.5f;

            Drawer2D.SafeDraw(spriteBatch, toDraw, font, Color.Black, new Vector2(fieldRect.X + fieldRect.Width / 2, fieldRect.Y + 16), origin);
        }
    }

}