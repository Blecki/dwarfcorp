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

        /*
        public static string[] SplitCsvToLines(string csv, char delimeter = '\n')
        {
            csv = csv.Replace("\r\n", "\n").Replace("\n\r", "\n");
            List<string> lines = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool isInsideACell = false;

            foreach (char ch in csv)
            {
                if (ch == delimeter)
                {
                    if (isInsideACell == false)
                    {
                        // nasli sme koniec riadka, vsetko co je teraz v StringBuilder-y je riadok
                        lines.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else
                {
                    sb.Append(ch);
                    if (ch == '"')
                    {
                        isInsideACell = !isInsideACell;
                    }
                }
            }

            if (sb.Length > 0)
            {
                lines.Add(sb.ToString());
            }

            return lines.ToArray();
        }

        public static string[] SplitCsvLineToCells(string line, char delimeter = ',')
        {
            List<string> list = new List<string>();
            do
            {
                if (line.StartsWith("\""))
                {
                    line = line.Substring(1);
                    int idx = line.IndexOf("\"");
                    while (line.IndexOf("\"", idx) == line.IndexOf("\"\"", idx))
                    {
                        idx = line.IndexOf("\"\"", idx) + 2;
                    }
                    idx = line.IndexOf("\"", idx);
                    list.Add(line.Substring(0, idx).Replace("\"\"", "\""));
                    if (idx + 2 < line.Length)
                    {
                        line = line.Substring(idx + 2);
                    }
                    else
                    {
                        line = String.Empty;
                    }
                }
                else
                {
                    list.Add(line.Substring(0, Math.Max(line.IndexOf(delimeter), 0)).Replace("\"\"", "\""));
                    line = line.Substring(line.IndexOf(delimeter) + 1);
                }
            }
            while (line.IndexOf(delimeter) != -1);
            if (!String.IsNullOrEmpty(line))
            {
                if (line.StartsWith("\"") && line.EndsWith("\""))
                {
                    line = line.Substring(1, line.Length - 2);
                }
                list.Add(line.Replace("\"\"", "\""));
            }

            return list.ToArray();
        }

        public static List<CreditEntry> ParseCSVFile(string file, SpriteFont font)
        {
            string data = File.ReadAllText(file);
            string[] lines = SplitCsvToLines(data);
            List<KeyValuePair<int, string>> backers = new List<KeyValuePair<int, string>>();
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] cells = SplitCsvLineToCells(line);
                int money = int.Parse(cells[4]);
                string name = cells[5];
                backers.Add(new KeyValuePair<int, string>(money, name));
            }
            backers.Sort((a, b) => a.Key.CompareTo(b.Key));
            backers.Reverse();
            List<CreditEntry> toReturn = new List<CreditEntry>();

            foreach (var backer in backers)
            {
                string role = "Backer";
                Color color = Color.White;
                bool flash = false;
                if (backer.Key >= 1000)
                {
                    role = "Mega-Producer";
                    color = Color.Yellow;
                    flash = true;
                }
                else if (backer.Key >= 500)
                {
                    role = "Super-Producer";
                    color = Color.Red;
                    flash = true;
                }
                else if (backer.Key >= 100)
                {
                    role = "Producer";
                }
                else if (backer.Key >= 20)
                {
                    role = "Mega-backer";
                }
                else if (backer.Key >= 10)
                {
                    role = "Super-backer";
                }
                else
                {
                    role = "Backer";
                }

                toReturn.Add(new CreditEntry()
                {
                    Name = Drawer2D.Internationalize(backer.Value, font),
                    Role = Drawer2D.Internationalize(role, font),
                    Color = color,
                    RandomFlash = flash
                });
            }
            return toReturn;
        }
        */

        public float ScrollSpeed { get; set; }
        public float CurrentScroll { get; set; }
        public float EntryHeight { get; set; }
        public float DividerHeight { get; set; }
        public SpriteFont CreditsFont { get; set; }
        public List<CreditEntry> Entries { get; set; }
        public int Padding { get; set; }
        public bool IsDone { get; set; }
        public CreditsState(DwarfGame game, string name, GameStateManager stateManager) 
            : base(game, name, stateManager)
        {
            ScrollSpeed = 30;
            EntryHeight = 30;
            Padding = 150;
            DividerHeight = EntryHeight*4;
            IsDone = false;
        }

        public override void OnEnter()
        {
            CurrentScroll = 0;
            CreditsFont = GameState.Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default);
            Entries = ContentPaths.LoadFromJson<List<CreditEntry>>("credits.json");
            IsInitialized = true;
            IsDone = false;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {

            if (!IsDone)
            {
                CurrentScroll += ScrollSpeed*(float) gameTime.ElapsedGameTime.TotalSeconds;
                KeyboardState state = Keyboard.GetState();
                MouseState mouseState = Mouse.GetState();
                if (state.GetPressedKeys().Length > 0 || mouseState.LeftButton == ButtonState.Pressed)
                {
                    IsDone = true;
                    StateManager.PopState();
                }
            }
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap,
                DepthStencilState.Default, RasterizerState.CullNone);
            float y = -CurrentScroll;
            int w = GameState.Game.GraphicsDevice.Viewport.Width;
            int h = GameState.Game.GraphicsDevice.Viewport.Height; 
            Drawer2D.FillRect(DwarfGame.SpriteBatch, new Rectangle(Padding - 30, 0, w-Padding*2 + 30, h), new Color(5, 5, 5, 150));
            foreach (CreditEntry entry in Entries)
            {
                if (entry.Divider)
                {
                    y += EntryHeight;
                    continue;
                }

                if (y + EntryHeight < -EntryHeight*2 ||
                    y + EntryHeight > GameState.Game.GraphicsDevice.Viewport.Height + EntryHeight*2)
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
            DwarfGame.SpriteBatch.End();
            base.Render(gameTime);
        }
    }
}
