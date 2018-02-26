// CompanyMakerState.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;
using System;

namespace DwarfCorp.GameStates
{
    /// <summary>
    /// This game state allows the player to design their own dwarf company.
    /// </summary>
    public class ManageModsState : GameState
    {
        private Gui.Root GuiRoot;
        private bool HasChanges = false;

        public ManageModsState(DwarfGame game, GameStateManager stateManager) :
            base(game, "ManageModsState", stateManager)
        {
        }

        private List<string> DetectMods()
        {
            if (System.IO.Directory.Exists("Mods"))
                return System.IO.Directory.EnumerateDirectories("Mods").Select(p => System.IO.Path.GetFileName(p)).ToList();
            else
                return new List<string>();
        }

        private class Mod
        {
            public bool Enabled = false;
            public string Name;
            public Widget LineItem;
        }

        public override void OnEnter()
        {
            var availableMods = DetectMods();
            var enabledMods = new List<String>(GameSettings.Default.EnabledMods);
            foreach (var mod in GameSettings.Default.EnabledMods)
                if (!availableMods.Contains(mod))
                    enabledMods.Remove(mod);
            foreach (var mod in enabledMods)
                availableMods.Remove(mod);
            var allMods = new List<Mod>();
            allMods.AddRange(enabledMods.Select(m => new Mod { Enabled = true, Name = m }));
            allMods.AddRange(availableMods.Select(m => new Mod { Enabled = false, Name = m }));



            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);

            var screen = GuiRoot.RenderData.VirtualScreen;
            float scale = 0.75f;
            float newWidth = System.Math.Min(System.Math.Max(screen.Width * scale, 640), screen.Width * scale);
            float newHeight = System.Math.Min(System.Math.Max(screen.Height * scale, 480), screen.Height * scale);
            Rectangle rect = new Rectangle((int)(screen.Width / 2 - newWidth / 2), (int)(screen.Height / 2 - newHeight / 2), (int)newWidth, (int)newHeight);
            // CONSTRUCT GUI HERE...
            var main = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = rect,
                Padding = new Margin(4, 4, 4, 4),
                Transparent = false,
                Border = "border-fancy",
                MinimumSize = new Point(640, 480),
                Font = "font10"
            });

            var bottom = main.AddChild(new Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockBottom,
                Padding = new Margin(2,2,2,2)
            });

            bottom.AddChild(new Gui.Widgets.Button
            {
                Text = "Close",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    // If changes, prompt before closing.
                    if (HasChanges)
                    {
                        var confirm = new Popup
                        {
                            Text = "Dwarf Corp must be restarted for changes to take effect.",
                            OkayText = "Okay",
                            OnClose = (s2) =>
                            {
                                SaveList(allMods);
                                StateManager.PopState();
                            }
                        };
                        GuiRoot.ShowModalPopup(confirm);
                    }
                    else
                    {
                        SaveList(allMods);
                        StateManager.PopState();
                    }
                },
                AutoLayout = AutoLayout.DockRight
            });

            var list = main.AddChild(new WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                Border = null,
                SelectedItemBackgroundColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                Padding = new Margin(2,2,2,2)
            }) as WidgetListView;

            foreach (var mod in allMods)
            {
                var lineItem = GuiRoot.ConstructWidget(new CheckBox
                {
                    MinimumSize = new Point(1, 32),
                    Text = mod.Name,
                    Padding = new Margin(2,2,2,2),
                    Background = new TileReference("basic", 0),
                    InteriorMargin = new Margin(2,2,2,2),
                    ToggleOnTextClick = false

                }) as CheckBox;

                lineItem.SilentSetCheckState(mod.Enabled);
                lineItem.Tag = mod;
                lineItem.OnCheckStateChange += (sender) =>
                {
                    (sender.Tag as Mod).Enabled = (sender as CheckBox).CheckState;
                    HasChanges = true;
                };

                mod.LineItem = lineItem;

                list.AddItem(lineItem);               
            }

            bottom.AddChild(new Gui.Widgets.Button
            {
                Text = "Move Up",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    var select = list.SelectedIndex;
                    if (select > 0)
                    {
                        var mod = allMods[list.SelectedIndex];
                        allMods.RemoveAt(list.SelectedIndex);
                        allMods.Insert(list.SelectedIndex - 1, mod);
                        list.SelectedIndex -= 1;
                        RebuildItems(list, allMods);
                        HasChanges = true;
                    }
                },
                AutoLayout = AutoLayout.DockLeft
            });

            bottom.AddChild(new Gui.Widgets.Button
            {
                Text = "Move Down",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    var select = list.SelectedIndex;
                    if (select < allMods.Count - 1)
                    {
                        var mod = allMods[list.SelectedIndex];
                        allMods.RemoveAt(list.SelectedIndex);
                        allMods.Insert(list.SelectedIndex + 1, mod);
                        list.SelectedIndex += 1;
                        RebuildItems(list, allMods);
                        HasChanges = true;
                    }
                },
                AutoLayout = AutoLayout.DockLeft
            });

            GuiRoot.RootItem.Layout();

            // Must be true or Render will not be called.
            IsInitialized = true;

            base.OnEnter();
        }

        private void RebuildItems(WidgetListView View, List<Mod> Mods)
        {
            View.ClearItems();
            foreach (var mod in Mods)
                View.AddItem(mod.LineItem);
        }

        private void SaveList(List<Mod> Mods)
        {
            GameSettings.Default.EnabledMods = Mods.Where(m => m.Enabled).Select(m => m.Name).ToList();
            GameSettings.Save();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToGameTime());
            base.Update(gameTime);
        }
        
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}