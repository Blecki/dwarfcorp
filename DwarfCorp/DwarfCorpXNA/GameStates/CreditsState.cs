using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class CreditsState : GameState
    {
        public class CreditEntry
        {
            public string Name { get; set; }
            public string Role { get; set; }
            public Color Color { get; set; }
            public bool RandomFlash { get; set; }
            public bool Divider { get; set; }
        }

        public float ScrollSpeed { get; set; }
        public float CurrentScroll { get; set; }
        public float EntryHeight { get; set; }
        public float DividerHeight { get; set; }
        public SpriteFont CreditsFont { get; set; }
        public List<CreditEntry> Entries { get; set; }
        public int Padding { get; set; }
        public bool IsDone { get; set; }
        public CreditsState(DwarfGame game, GameStateManager stateManager) 
            : base(game, "CreditsState", stateManager)
        {
            ScrollSpeed = 30;
            EntryHeight = 30;
            Padding = 150;
            DividerHeight = EntryHeight*4;
            IsDone = false;
        }

        public override void OnEnter()
        {
            // Todo - HACK - Remove when input transition is complete.
            DwarfGame.GumInputMapper.GetInputQueue();

            CurrentScroll = 0;
            CreditsFont = GameState.Game.Content.Load<SpriteFont>(AssetManager.ResolveContentPath(ContentPaths.Fonts.Default));
            Entries = FileUtils.LoadJsonFromResolvedPath<List<CreditEntry>>("credits.json");
            IsInitialized = true;
            IsDone = false;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            // Use new input system so event is not captured by both GUIs.
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                if (@event.Message == Gui.InputEvents.KeyPress || @event.Message == Gui.InputEvents.MouseClick)
                {
                    IsDone = true;
                    StateManager.PopState();
                }
            }

            if (!IsDone)
            {
                CurrentScroll += ScrollSpeed*(float) gameTime.ElapsedRealTime.TotalSeconds;
             }

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            try
            {
                DwarfGame.SafeSpriteBatchBegin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap,
                    DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.Identity);
                float y = -CurrentScroll;
                int w = GameState.Game.GraphicsDevice.Viewport.Width;
                int h = GameState.Game.GraphicsDevice.Viewport.Height;
                Drawer2D.FillRect(DwarfGame.SpriteBatch, new Rectangle(Padding - 30, 0, w - Padding * 2 + 30, h), new Color(5, 5, 5, 150));
                foreach (CreditEntry entry in Entries)
                {
                    if (entry.Divider)
                    {
                        y += EntryHeight;
                        continue;
                    }

                    if (y + EntryHeight < -EntryHeight * 2 ||
                        y + EntryHeight > GameState.Game.GraphicsDevice.Viewport.Height + EntryHeight * 2)
                    {
                        y += EntryHeight;
                        continue;
                    }
                    Color color = entry.Color;

                    if (entry.RandomFlash)
                    {
                        color = new Color(MathFunctions.RandVector3Box(-1, 1, -1, 1, -1, 1) * 0.5f + color.ToVector3());
                    }
                    DwarfGame.SpriteBatch.DrawString(CreditsFont, entry.Role, new Vector2(w / 2 - Datastructures.SafeMeasure(CreditsFont, entry.Role).X - 5, y), color);
                    DwarfGame.SpriteBatch.DrawString(CreditsFont, entry.Name, new Vector2(w / 2 + 5, y), color);

                    y += EntryHeight;
                }
            }
            finally
            {
                DwarfGame.SpriteBatch.End();
            }
            base.Render(gameTime);
        }
    }
}
