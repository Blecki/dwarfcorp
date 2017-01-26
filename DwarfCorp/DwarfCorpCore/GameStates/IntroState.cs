// IntroState.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.GameStates
{

    /// <summary>
    ///  This game state displays the company and game credits or whatever else needs to go at the beginning of the game.
    /// </summary>
    public class IntroState : GameState
    {
        public Texture2D Logo { get; set; }
        public Timer IntroTimer = new Timer(1, true);

        public IntroState(DwarfGame game, GameStateManager stateManager) :
            base(game, "IntroState", stateManager)
        {
        }

        public override void OnEnter()
        {
            IsInitialized = true;
            Logo = TextureManager.GetTexture(ContentPaths.Logos.companylogo);
            IntroTimer.Reset(3);

            base.OnEnter();
        }


        public override void Update(DwarfTime gameTime)
        {
            Game.IsMouseVisible = false;
            IntroTimer.Update(gameTime);

            if(IntroTimer.HasTriggered)
            {
                StateManager.PushState("MainMenuState");
            }

            if(Keyboard.GetState().GetPressedKeys().Length > 0)
            {
                StateManager.PushState("MainMenuState");
            }

            base.Update(gameTime);
        }


        public override void Render(DwarfTime gameTime)
        {

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            Vector2 screenCenter = new Vector2(Game.GraphicsDevice.Viewport.Width / 2 - Logo.Width / 2, Game.GraphicsDevice.Viewport.Height / 2 - Logo.Height / 2);
            DwarfGame.SpriteBatch.Draw(Logo, screenCenter, null, new Color(1f, 1f, 1f));
            DwarfGame.SpriteBatch.End();

            base.Render(gameTime);
        }
    }

}