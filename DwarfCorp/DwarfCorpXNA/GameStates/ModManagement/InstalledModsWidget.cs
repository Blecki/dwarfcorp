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
using DwarfCorp.AssetManagement.Steam;

namespace DwarfCorp.GameStates.ModManagement
{
    public class InstalledModsWidget : Gui.Widget
    {
        private bool HasChanges = false;
        private Gui.Widgets.Popup UploadPopup = null;
        private UGCItemUploader ModUploader = null;

        public Action<Widget> OnOkayPressed = null;

        private class ModInfo
        {
            public bool Enabled = false;
            public Widget LineItem;
            public ModMetaData MetaData;
        }

        public override void Construct()
        {
            base.Construct();

            var availableMods = AssetManager.EnumerateInstalledMods().ToList();
            var allMods = new List<ModInfo>();

            // Doing it like this ensures that enabled mods are listed first.
            // Todo: Are they listed in the correct order??
            allMods.AddRange(availableMods.Where(m => GameSettings.Default.EnabledMods.Contains(m.Guid)).Select(m => new ModInfo
            {
                Enabled = true,
                MetaData = m
            }));

            allMods.AddRange(availableMods.Where(m => !GameSettings.Default.EnabledMods.Contains(m.Guid)).Select(m => new ModInfo
            {
                Enabled = false,
                MetaData = m
            }));

            this.Padding = new Margin(4, 4, 4, 4);
            this.Font = "font10";

            Root.RegisterForUpdate(this);

            OnUpdate = (sender, time) =>
            {
                if (ModUploader != null)
                {
                    try
                    {
                        if (ModUploader.Status == UGCItemUploader.ItemUpdateStatus.Working)
                            ModUploader.Update();
                        else
                            UploadPopup.AddOkayButton();

                        UploadPopup.Text = ModUploader.Message;
                        UploadPopup.Invalidate();
                    }
                    catch (Exception e)
                    {
                        UploadPopup.Text = e.Message;
                        UploadPopup.Invalidate();
                        ModUploader = null;
                    }
                }
            };

            var bottom = AddChild(new Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockBottom,
                Padding = new Margin(2, 2, 2, 2)
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
                                Root.SafeCall(OnOkayPressed, this);
                            }
                        };
                        Root.ShowModalPopup(confirm);
                    }
                    else
                    {
                        SaveList(allMods);
                        Root.SafeCall(OnOkayPressed, this);
                    }
                },
                AutoLayout = AutoLayout.DockRight
            });

            var list = AddChild(new WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                Border = null,
                SelectedItemBackgroundColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                Padding = new Margin(2, 2, 2, 2)
            }) as WidgetListView;

            foreach (var mod in allMods)
            {
                CreateLineItem(allMods, list, mod);
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
        }

        private void CreateLineItem(List<ModInfo> allMods, WidgetListView list, ModInfo mod)
        {
            var lineItem = Root.ConstructWidget(new Widget
            {
                MinimumSize = new Point(1, 32),
                Background = new TileReference("basic", 0),
                TextColor = new Vector4(0, 0, 0, 1),
            });

            if (mod.MetaData.Source == ModSource.LocalDirectory)
            {
                var upload = lineItem.AddChild(new Button()
                {
                    Text = mod.MetaData.SteamID == 0 ? "Publish mod to Steam" : "Upload update to Steam",
                    AutoLayout = AutoLayout.DockRight,
                    TextColor = new Vector4(0, 0, 0, 1),
                    OnClick = (sender, args) =>
                    {
                        ModUploader = new AssetManagement.Steam.UGCItemUploader(mod.MetaData);
                        UploadPopup = Root.ConstructWidget(new Popup { ShowOkayButton = false, OnClose = (s) => ModUploader = null }) as Gui.Widgets.Popup;
                        Root.ShowModalPopup(UploadPopup);

                        RebuildItems(list, allMods);
                    }
                });
            }

            var toggle = lineItem.AddChild(new CheckBox
            {
                MinimumSize = new Point(128, 32),
                MaximumSize = new Point(128, 32),
                Text = mod.MetaData.Name,
                Padding = new Margin(2, 2, 2, 2),
                InteriorMargin = new Margin(2, 2, 2, 2),
                ToggleOnTextClick = false,
                AutoLayout = AutoLayout.DockLeft,
                TextColor = new Vector4(0, 0, 0, 1),
            }) as CheckBox;

            lineItem.AddChild(new Widget
            {
                Font = "font8",
                Text = String.Format("{2} Source: {0}\nDirectory: {1}", mod.MetaData.Source, mod.MetaData.Directory, mod.MetaData.Guid.ToString().Substring(0, 8)),
                AutoLayout = AutoLayout.DockFill,
                TextColor = new Vector4(0, 0, 0, 1)
            });

            toggle.SilentSetCheckState(mod.Enabled);
            toggle.Tag = mod;
            toggle.OnCheckStateChange += (sender) =>
            {
                (sender.Tag as ModInfo).Enabled = (sender as CheckBox).CheckState;
                HasChanges = true;
            };

            mod.LineItem = lineItem;

            list.AddItem(lineItem);
        }

        private void RebuildItems(WidgetListView View, List<ModInfo> Mods)
        {
            View.ClearItems();
            foreach (var mod in Mods)
                CreateLineItem(Mods, View, mod);
        }

        private void SaveList(List<ModInfo> Mods)
        {
            GameSettings.Default.EnabledMods = Mods.Where(m => m.Enabled).Select(m => m.MetaData.Guid).ToList();
            GameSettings.Save();
        }
    }
}