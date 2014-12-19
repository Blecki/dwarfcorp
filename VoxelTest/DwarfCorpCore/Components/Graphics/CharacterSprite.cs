using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This is a special kind of sprite which assumes that it is attached to a character
    /// which has certain animations and can face in four directions. Also provides interfaces to
    /// certain effects such as blinking.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CharacterSprite : OrientedAnimation
    {
        [JsonIgnore]
        public GraphicsDevice Graphics { get; set; }

        private Timer blinkTimer = new Timer(0.1f, false);
        private Timer coolDownTimer = new Timer(1.0f, false);
        private Timer blinkTrigger = new Timer(0.0f, true);
        private bool isBlinking = false;
        private bool isCoolingDown = false;


        public override void Render(GameTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            if (!isBlinking)
            {
                base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            }
            else
            {
                if (blinkTimer.CurrentTimeSeconds < 0.5f*blinkTimer.TargetTimeSeconds)
                {
                    base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
                }
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Graphics = PlayState.ChunkManager.Graphics;
        }

        public CharacterSprite()
        {

        }

        public CharacterSprite(GraphicsDevice graphics, ComponentManager manager, string name, GameComponent parent,
            Matrix localTransform) :
                base(manager, name, parent, localTransform)
        {
            Graphics = graphics;
        }

        public bool HasAnimation(Creature.CharacterMode mode, Orientation orient)
        {
            return Animations.ContainsKey(mode.ToString() + OrientationStrings[(int) orient]);
        }

        public List<Animation> GetAnimations(Creature.CharacterMode mode)
        {
            return
                OrientationStrings.Where((t, i) => HasAnimation(mode, (Orientation) i))
                    .Select(t => Animations[mode.ToString() + t])
                    .ToList();
        }

        public void ResetAnimations(Creature.CharacterMode mode)
        {
            List<Animation> animations = GetAnimations(mode);
            foreach (Animation a in animations)
            {
                a.Reset();
            }
        }

        public Animation GetAnimation(Creature.CharacterMode mode, Orientation orient)
        {
            if (HasAnimation(mode, orient))
            {
                return Animations[mode.ToString() + OrientationStrings[(int) orient]];
            }
            else
            {
                return null;
            }
        }


        public static Animation CreateAnimation(Creature.CharacterMode mode,
            Orientation orient,
            Texture2D texture,
            float frameHz,
            int frameWidth,
            int frameHeight,
            int row,
            params int[] cols)
        {
            return CreateAnimation(mode, orient, texture, frameHz, frameWidth, frameHeight, row, cols.ToList());
        }


        public static CompositeAnimation CreateCompositeAnimation(Creature.CharacterMode mode,
            Orientation orient,
            Composite composite,
            float frameHz,
            List<SpriteSheet> layers,
            List<Color> tints,
            params int[][] frames
            )
        {
            return new CompositeAnimation(composite, layers, tints, frames)
            {
                FrameHZ = frameHz,
                Name = mode.ToString() + OrientationStrings[(int) orient],
                Loops = true,
                CurrentFrame = 0
            };
        }

        public static Animation CreateAnimation(Creature.CharacterMode mode,
            Orientation orient,
            Texture2D texture,
            float frameHz,
            int frameWidth,
            int frameHeight,
            int row,
            List<int> cols)
        {
            List<Point> frames = new List<Point>();
            int numCols = texture.Width / frameWidth;

            if (cols.Count == 0)
            {
                for (int i = 0; i < numCols; i++)
                {
                    frames.Add(new Point(i, row));
                }
            }
            else
            {
                frames.AddRange(cols.Select(c => new Point(c, row)));
            }

            return new Animation(GameState.Game.GraphicsDevice, texture, mode.ToString() + OrientationStrings[(int)orient], frameWidth, frameHeight, frames, true, Color.White, frameHz, (float)frameWidth / 35.0f, (float)frameHeight / 35.0f, false);
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
            List<int> ints = new List<int>();
            ints.AddRange(cols);
            Animation animation = CreateAnimation(mode, orient, texture, frameHz, frameWidth, frameHeight, row, ints);
            Animations[mode.ToString() + OrientationStrings[(int) orient]] = animation;
            animation.Play();
        }

        public void Blink(float blinkTime)
        {
            if(isBlinking || isCoolingDown)
            {
                return;
            }

            isBlinking = true;
            blinkTrigger.Reset(blinkTime);
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if(isBlinking)
            {
                blinkTrigger.Update(gameTime);

                if(blinkTrigger.HasTriggered)
                {
                    isBlinking = false;
                    isCoolingDown = true;
                }
            }

            if(isCoolingDown)
            {
                coolDownTimer.Update(gameTime);

                if(coolDownTimer.HasTriggered)
                {
                    isCoolingDown = false;
                }
            }

            blinkTimer.Update(gameTime);


            base.Update(gameTime, chunks, camera);
        }
    }

}