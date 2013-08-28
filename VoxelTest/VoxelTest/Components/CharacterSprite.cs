using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class CharacterSprite : OrientedAnimation
    {
        public GraphicsDevice Graphics { get; set; }

        public CharacterSprite(GraphicsDevice graphics, ComponentManager manager, string name, GameComponent parent, Matrix localTransform) :
            base(manager, name, parent, localTransform)
        {
            Graphics = graphics;
        }

        public bool HasAnimation(Creature.CharacterMode mode, Orientation orient)
        {
            return Animations.ContainsKey(mode.ToString() + OrientationStrings[(int)orient]);
        }

        public Animation GetAnimation(Creature.CharacterMode mode, Orientation orient)
        {
            if(HasAnimation(mode, orient))
            {
                return Animations[mode.ToString() + OrientationStrings[(int)orient]];
            }
            else
            {
                return null;
            }
        }

        public void AddAnimation(Creature.CharacterMode mode, 
                                 Orientation orient, 
                                 Texture2D texture,
                                 float frameHz,
                                 int frameWidth,
                                 int frameHeight,
                                 int row,
                                 params int[] cols)
        {
            List<Point> frames = new List<Point>();
            int numCols = texture.Width / frameWidth;

            if (cols.Length == 0)
            {
                for (int i = 0; i < numCols; i++)
                {
                    frames.Add(new Point(i, row));
                }
            }
            else
            {
                foreach (int c in cols)
                {
                    frames.Add(new Point(c, row));
                }
            }

            Animation animation = new Animation(Graphics, texture, mode.ToString() + OrientationStrings[(int)orient], frameWidth, frameHeight,frames, true, Color.White, frameHz,  (float)frameWidth / 35.0f, (float)frameHeight / 35.0f, false);
            Animations[mode.ToString() + OrientationStrings[(int)orient]] = animation;
            animation.Play();
        }

    }
}
