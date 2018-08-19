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

        private Gui.Widgets.WidgetListView ModListGUI;
        private List<LineItem> ModList;

        private class LineItem
        {
            public bool Enabled = false;
            public Widget GUI;
            public ModMetaData MetaData;
        }

        public override void Construct()
        {
            base.Construct();

            OwnerState.OnSystemChanges += () =>
            {
                RefreshModList();
                RebuildModListGUI();
            };

            this.Padding = new Margin(4, 4, 4, 4);
            this.Font = "font10";

            var top = AddChild(new Widget
            {
                Transparent = true,
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(2, 2, 2, 2)
            });

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
                    var select = ModListGUI.SelectedIndex;
                    if (select > 0)
                    {
                        var mod = ModList[ModListGUI.SelectedIndex];
                        ModList.RemoveAt(ModListGUI.SelectedIndex);
                        ModList.Insert(ModListGUI.SelectedIndex - 1, mod);
                        ModListGUI.SelectedIndex -= 1;
                        SaveEnabledList();
                        OwnerState.MadeSystemChanges();
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
                    var select = ModListGUI.SelectedIndex;
                    if (select < ModList.Count - 1)
                    {
                        var mod = ModList[ModListGUI.SelectedIndex];
                        ModList.RemoveAt(ModListGUI.SelectedIndex);
                        ModList.Insert(ModListGUI.SelectedIndex + 1, mod);
                        ModListGUI.SelectedIndex += 1;
                        SaveEnabledList();
                        OwnerState.MadeSystemChanges();
                    }
                },
                AutoLayout = AutoLayout.DockLeft
            });

            ModListGUI = AddChild(new WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                Border = null,
                SelectedItemBackgroundColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                Padding = new Margin(2, 2, 2, 2)
            }) as WidgetListView;

            RefreshModList();
            RebuildModListGUI();
        }

        private Gui.Widget BuildLineItemGUI(LineItem Mod)
        {
            var gui = Root.ConstructWidget(new Widget
            {
                MinimumSize = new Point(1, 32),
                Background = new TileReference("basic", 0),
                TextColor = new Vector4(0, 0, 0, 1),
            });

            if (Mod.MetaData.Source == ModSource.LocalDirectory)
            {
                var upload = gui.AddChild(new Button()
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
            var toggle = gui.AddChild(new CheckBox
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

            gui.AddChild(new Widget
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
                (sender.Tag as LineItem).Enabled = (sender as CheckBox).CheckState;
                SaveEnabledList();
                OwnerState.MadeSystemChanges();
            };

            return gui;
        }

        private void RebuildModListGUI()
        {
            ModListGUI.ClearItems();
            foreach (var mod in ModList)
            {
                if (mod.GUI == null)
                    mod.GUI = BuildLineItemGUI(mod);
                ModListGUI.AddItem(mod.GUI);
            }
        }

        private void SaveEnabledList()
        {
            GameSettings.Default.EnabledMods = ModList.Where(m => m.Enabled).Select(m => m.MetaData.IdentifierString).ToList();
            GameSettings.Save();
        }

        private void RefreshModList()
        {
            var availableMods = AssetManager.DiscoverMods().ToList();

            var allMods = new List<LineItem>();

            foreach (var mod_id in GameSettings.Default.EnabledMods)
            {
                var metaData = availableMods.FirstOrDefault(mod => mod.IdentifierString == mod_id);
                if (metaData == null) continue; // It's in the enabled list, but must have been uninstalled.

                allMods.Add(CreateModLineItem(metaData, true));
            }

            foreach (var mod in availableMods)
            {
                if (allMods.Any(m => m.MetaData.IdentifierString == mod.IdentifierString)) continue; // Avoid duplicating entries from the enabled list.

                allMods.Add(CreateModLineItem(mod, false));
            }

            ModList = allMods;
        }

        private LineItem CreateModLineItem(ModMetaData MetaData, bool Enabled)
        {
            LineItem newEntry = null;

            // Check if this mod already has a line item - if so, we need to recycle the object because there might already be GUI updates happening to it.
            if (ModList != null)
                newEntry = ModList.FirstOrDefault(mod => mod.MetaData.IdentifierString == MetaData.IdentifierString);
            if (newEntry == null)
                newEntry = new LineItem();

            newEntry.Enabled = Enabled;
            newEntry.MetaData = MetaData;

            return newEntry;
        }
    }
}