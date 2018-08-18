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
        // Todo: Need to be able to refresh the list.

        public ManageModsState OwnerState;

        private Gui.Widgets.WidgetListView ModList;

        private class ModInfo
        {
            public bool Enabled = false;
            public Widget LineItem;
            public ModMetaData MetaData;
        }

        public override void Construct()
        {
            base.Construct();

            this.Padding = new Margin(4, 4, 4, 4);
            this.Font = "font10";

            var top = AddChild(new Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(2, 2, 2, 2)
            });

            var availableMods = AssetManager.EnumerateInstalledMods().ToList();
            var allMods = new List<ModInfo>();

            // Doing it like this ensures that enabled mods are listed first.
            allMods.AddRange(GameSettings.Default.EnabledMods.Select(m => new ModInfo
                {
                    Enabled = true,
                    MetaData = availableMods.First(mod => mod.IdentifierString == m)
                }));

            allMods.AddRange(availableMods.Where(mod => !GameSettings.Default.EnabledMods.Contains(mod.IdentifierString)).Select(mod => new ModInfo
            {
                Enabled = false,
                MetaData = mod
            }));

            // Todo: Find subscribed but not installed mods.
            // - Will require lazy loading of mod details.

            //if (AssetManagement.Steam.Steam.SteamAvailable)
            //{
            //    var subscribedCount = Steamworks.SteamUGC.GetNumSubscribedItems();
            //    var subscribedFileIds = new Steamworks.PublishedFileId_t[subscribedCount];
            //    Steamworks.SteamUGC.GetSubscribedItems(subscribedFileIds, subscribedCount);

            //    foreach (var fileId in subscribedFileIds.Where(id => !r.Any(m => m.SteamID == (ulong)id)))
            //    {

            //    }
            //}


            
            top.AddChild(new Gui.Widgets.Button
            {
                Text = "Move Up",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    var select = ModList.SelectedIndex;
                    if (select > 0)
                    {
                        var mod = allMods[ModList.SelectedIndex];
                        allMods.RemoveAt(ModList.SelectedIndex);
                        allMods.Insert(ModList.SelectedIndex - 1, mod);
                        ModList.SelectedIndex -= 1;
                        RebuildItems(allMods);
                        SaveList(allMods);
                        OwnerState.MadeChanges();
                    }
                },
                AutoLayout = AutoLayout.DockLeft
            });

            top.AddChild(new Gui.Widgets.Button
            {
                Text = "Move Down",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    var select = ModList.SelectedIndex;
                    if (select < allMods.Count - 1)
                    {
                        var mod = allMods[ModList.SelectedIndex];
                        allMods.RemoveAt(ModList.SelectedIndex);
                        allMods.Insert(ModList.SelectedIndex + 1, mod);
                        ModList.SelectedIndex += 1;
                        RebuildItems(allMods);
                        SaveList(allMods);
                        OwnerState.MadeChanges();
                    }
                },
                AutoLayout = AutoLayout.DockLeft
            });

            ModList = AddChild(new WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                Border = null,
                SelectedItemBackgroundColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                Padding = new Margin(2, 2, 2, 2)
            }) as WidgetListView;


            RebuildItems(allMods);
        }

        private void BuildLineItem(Widget LineItem, ModInfo Mod)
        {
            if (Mod.MetaData.Source == ModSource.LocalDirectory)
            {
                var upload = LineItem.AddChild(new Button()
                {
                    Text = Mod.MetaData.SteamID == 0 ? "Publish mod to Steam" : "Upload update to Steam",
                    AutoLayout = AutoLayout.DockRight,
                    TextColor = new Vector4(0, 0, 0, 1),
                    OnClick = (sender, args) =>
                    {
                        sender.OnClick = null;

                        Steam.AddTransaction(new UGCTransactionProcessor
                        {
                            Transaction = new UGCUpload(Mod.MetaData),
                            StatusMessageDisplay = sender
                        });
                    }      
                });
            }

            // Todo: Download button for subscribed but not installed mods.

            // Todo: Update button for mods that need updated.

            // Todo: Disable toggle if mod is not installed.
            var toggle = LineItem.AddChild(new CheckBox
            {
                MinimumSize = new Point(128, 32),
                MaximumSize = new Point(128, 32),
                Text = Mod.MetaData.Name,
                Padding = new Margin(2, 2, 2, 2),
                InteriorMargin = new Margin(2, 2, 2, 2),
                ToggleOnTextClick = false,
                AutoLayout = AutoLayout.DockLeft,
                TextColor = new Vector4(0, 0, 0, 1),
            }) as CheckBox;

            LineItem.AddChild(new Widget
            {
                Font = "font8",
                Text = String.Format("{2} Source: {0}\nDirectory: {1}", Mod.MetaData.Source, Mod.MetaData.Directory, Mod.MetaData.IdentifierString),
                AutoLayout = AutoLayout.DockFill,
                TextColor = new Vector4(0, 0, 0, 1)
            });

            toggle.SilentSetCheckState(Mod.Enabled);
            toggle.Tag = Mod;
            toggle.OnCheckStateChange += (sender) =>
            {
                (sender.Tag as ModInfo).Enabled = (sender as CheckBox).CheckState;
                OwnerState.MadeChanges();
            };
        }

        private void CreateLineItem(ModInfo mod)
        {
            if (mod.LineItem == null)
            {
                mod.LineItem = Root.ConstructWidget(new Widget
                {
                    MinimumSize = new Point(1, 32),
                    Background = new TileReference("basic", 0),
                    TextColor = new Vector4(0, 0, 0, 1),
                });

                BuildLineItem(mod.LineItem, mod);
            }

            ModList.AddItem(mod.LineItem);
        }

        private void RebuildItems(List<ModInfo> Mods)
        {
            ModList.ClearItems();
            foreach (var mod in Mods)
                CreateLineItem(mod);
        }

        private void SaveList(List<ModInfo> Mods)
        {
            GameSettings.Default.EnabledMods = Mods.Where(m => m.Enabled).Select(m => m.MetaData.IdentifierString).ToList();
            GameSettings.Save();
        }
    }
}