// CharacterSprite.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     This is a special kind of Sprite which assumes that it is attached to a character
    ///     which has certain animations and can face in four directions. Also provides interfaces to
    ///     certain effects such as blinking.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CharacterSprite : OrientedAnimation
    {
        /// <summary>
        /// Timer that triggers whenever the sprite should blink on and off.
        /// </summary>
        private readonly Timer blinkTimer = new Timer(0.1f, false);
        /// <summary>
        /// Timer that triggers whenever the sprite begins blinking.
        /// </summary>
        private readonly Timer blinkTrigger = new Timer(0.0f, true);
        /// <summary>
        /// Timer that prevents the character sprite from blinking.
        /// </summary>
        private readonly Timer coolDownTimer = new Timer(1.0f, false);
        /// <summary>
        /// If the character sprite is blinking, this is true.
        /// </summary>
        private bool isBlinking;
        /// <summary>
        /// If the character sprite is no longer blinking, but is cooling down.
        /// </summary>
        private bool isCoolingDown;


        public CharacterSprite()
        {
            currentMode = "Idle";
        }

        public CharacterSprite(GraphicsDevice graphics, ComponentManager manager, string name, GameComponent parent,
            Matrix localTransform) :
                base(manager, name, parent, localTransform)
        {
            Graphics = graphics;
            currentMode = "Idle";
        }

        [JsonIgnore]
        public GraphicsDevice Graphics { get; set; }

        public override void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
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

        /// <summary>
        /// Called when the sprite is deserialized from JSON.
        /// </summary>
        /// <param name="context">The context.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Graphics = PlayState.ChunkManager.Graphics;
        }

        /// <summary>
        /// Determines whether the specified mode has animation.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="orient">The orientation</param>
        /// <returns>
        ///   <c>true</c> if the specified mode has animation; otherwise, <c>false</c>.
        /// </returns>
        public bool HasAnimation(Creature.CharacterMode mode, Orientation orient)
        {
            return Animations.ContainsKey(mode + OrientationStrings[(int) orient]);
        }

        /// <summary>
        /// Gets the animations for the specified mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public List<Animation> GetAnimations(Creature.CharacterMode mode)
        {
            return
                OrientationStrings.Where((t, i) => HasAnimation(mode, (Orientation) i))
                    .Select(t => Animations[mode.ToString() + t])
                    .ToList();
        }

        /// <summary>
        /// For each animation, determines if it is done, and resets it if it is.
        /// </summary>
        /// <param name="mode">The mode.</param>
        public void ReloopAnimations(Creature.CharacterMode mode)
        {
            List<Animation> animations = GetAnimations(mode);
            foreach (Animation a in animations)
            {
                if (a.IsDone())
                {
                    a.Reset();
                }
            }
        }

        /// <summary>
        /// Resets all animations.
        /// </summary>
        /// <param name="mode">The mode.</param>
        public void ResetAnimations(Creature.CharacterMode mode)
        {
            List<Animation> animations = GetAnimations(mode);
            foreach (Animation a in animations)
            {
                a.Reset();
            }
        }

        /// <summary>
        /// Gets the animation corresponding to a mode and orientation.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="orient">The orientation</param>
        /// <returns>The animation corresponding to the mode and orientation</returns>
        public Animation GetAnimation(Creature.CharacterMode mode, Orientation orient)
        {
            if (HasAnimation(mode, orient))
            {
                return Animations[mode + OrientationStrings[(int) orient]];
            }
            return null;
        }


        /// <summary>
        /// Creates a new animation.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="orient">The orientation</param>
        /// <param name="texture">The texture.</param>
        /// <param name="frameHz">The framerate hz.</param>
        /// <param name="frameWidth">Width of the frame in pixels.</param>
        /// <param name="frameHeight">Height of the frame in pixels.</param>
        /// <param name="row">The row of the sprite sheet</param>
        /// <param name="cols">The columns of the animation in the sprite sheet.</param>
        /// <returns>A new animation for the character.</returns>
        public static Animation CreateAnimation(Creature.CharacterMode mode,
            Orientation orient,
            SpriteSheet texture,
            float frameHz,
            int frameWidth,
            int frameHeight,
            int row,
            params int[] cols)
        {
            return CreateAnimation(mode, orient, texture, frameHz, frameWidth, frameHeight, row, cols.ToList());
        }


        /// <summary>
        /// Creates a composite (layered) animation.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="orient">The orientation.</param>
        /// <param name="composite">The composite from <see cref="CompositeLibrary"/></param>
        /// <param name="frameHz">The framerate in hz.</param>
        /// <param name="layers">The layers of the animation</param>
        /// <param name="tints">The tints of the layers.</param>
        /// <param name="frames">The frames of the animation.</param>
        /// <returns>A new animation for the character.</returns>
        public static CompositeAnimation CreateCompositeAnimation(Creature.CharacterMode mode,
            Orientation orient,
            string composite,
            float frameHz,
            List<SpriteSheet> layers,
            List<Color> tints,
            params int[][] frames
            )
        {
            return new CompositeAnimation(composite, layers, tints, frames)
            {
                FrameHZ = frameHz,
                Name = mode + OrientationStrings[(int) orient],
                Loops = true,
                CurrentFrame = 0
            };
        }

        /// <summary>
        /// Creates an animation for the specified mode and orientation.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="orient">The orienttation</param>
        /// <param name="texture">The texture sprite sheet.</param>
        /// <param name="frameHz">The framerate in hz.</param>
        /// <param name="frameWidth">Width of the frame in pixels.</param>
        /// <param name="frameHeight">Height of the frame in pixels.</param>
        /// <param name="row">The row of the spritesheet for the animation.</param>
        /// <param name="cols">The columns of the frames in the animation.</param>
        /// <returns>A new animation.</returns>
        public static Animation CreateAnimation(Creature.CharacterMode mode,
            Orientation orient,
            SpriteSheet texture,
            float frameHz,
            int frameWidth,
            int frameHeight,
            int row,
            List<int> cols)
        {
            var frames = new List<Point>();
            int numCols = texture.Width/frameWidth;

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

            return new Animation(GameState.Game.GraphicsDevice, texture, mode + OrientationStrings[(int) orient],
                frameWidth, frameHeight, frames, true, Color.White, frameHz, frameWidth/35.0f, frameHeight/35.0f, false);
        }


        /// <summary>
        /// Creates a new animation for the given character mode and orientation.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="orient">The orientation</param>
        /// <param name="texture">The texture sprite sheet.</param>
        /// <param name="frameHz">The framerate in hz.</param>
        /// <param name="frameWidth">Width of the frame in pixels.</param>
        /// <param name="frameHeight">Height of the frame in pixels.</param>
        /// <param name="row">The row of the spritesheet.</param>
        /// <param name="cols">The frame columns of the spritesheet.</param>
        public void AddAnimation(Creature.CharacterMode mode,
            Orientation orient,
            SpriteSheet texture,
            float frameHz,
            int frameWidth,
            int frameHeight,
            int row,
            params int[] cols)
        {
            var ints = new List<int>();
            ints.AddRange(cols);
            Animation animation = CreateAnimation(mode, orient, texture, frameHz, frameWidth, frameHeight, row, ints);
            Animations[mode + OrientationStrings[(int) orient]] = animation;
            animation.Play();
        }

        /// <summary>
        /// Blinks for the specified blink time in seconds.
        /// </summary>
        /// <param name="blinkTime">The blink time.</param>
        public void Blink(float blinkTime)
        {
            if (isBlinking || isCoolingDown)
            {
                return;
            }

            isBlinking = true;
            blinkTrigger.Reset(blinkTime);
        }

        /// <summary>
        /// Updates the character sprite.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="chunks">The chunks.</param>
        /// <param name="camera">The camera.</param>
        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (isBlinking)
            {
                blinkTrigger.Update(gameTime);

                if (blinkTrigger.HasTriggered)
                {
                    isBlinking = false;
                    isCoolingDown = true;
                }
            }

            if (isCoolingDown)
            {
                coolDownTimer.Update(gameTime);

                if (coolDownTimer.HasTriggered)
                {
                    isCoolingDown = false;
                }
            }

            blinkTimer.Update(gameTime);


            base.Update(gameTime, chunks, camera);
        }

        /// <summary>
        /// Pauses the animations for the gven mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        public void PauseAnimations(Creature.CharacterMode mode)
        {
            List<Animation> animations = GetAnimations(mode);
            foreach (Animation a in animations)
            {
                a.IsPlaying = false;
            }
        }

        /// <summary>
        /// Plays the animations for the given mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        public void PlayAnimations(Creature.CharacterMode mode)
        {
            List<Animation> animations = GetAnimations(mode);
            foreach (Animation a in animations)
            {
                a.IsPlaying = true;
            }
        }
    }
}