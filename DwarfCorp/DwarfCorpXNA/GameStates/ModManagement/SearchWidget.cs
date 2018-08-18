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
using Steamworks;

namespace DwarfCorp.GameStates.ModManagement
{
    public class SearchWidget : Gui.Widget
    {
        private Gui.Widget QueryStatusMessage;
        private Gui.Widgets.WidgetListView List;

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

            var tags = top.AddChild(new Gui.Widgets.EditableTextField
            {
                PromptText = "Enter text to search for",
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(256, 0),
            });

            top.AddChild(new Gui.Widgets.Button
            {
                Text = "Search",
                Font = "font16",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    if (!Steam.HasTransactionOfType<UGCQuery>())
                        Steam.AddTransaction(new UGCTransactionProcessor
                        {
                            Transaction = new UGCQuery
                            {
                                SearchString = tags.Text
                            },
                            StatusMessageDisplay = QueryStatusMessage,
                            OnSuccess = (query) => RefreshList((query.Transaction as UGCQuery).Results)
                        });
                },
                AutoLayout = AutoLayout.DockRight
            });

            QueryStatusMessage = top.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockFill
            });

            List = AddChild(new WidgetListView
            {
                ItemHeight = 32,
                AutoLayout = AutoLayout.DockFill,
                Border = null,
                SelectedItemBackgroundColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                Padding = new Margin(2, 2, 2, 2)
            }) as WidgetListView;
        }

        private void CreateLineItem(WidgetListView list, SteamUGCDetails_t mod)
        {
            var lineItem = Root.ConstructWidget(new Widget
            {
                MinimumSize = new Point(1, 32),
                Background = new TileReference("basic", 0),
                TextColor = new Vector4(0, 0, 0, 1),
            });

            lineItem.AddChild(new Widget
            {
                Font = "font10",
                Text = "Subscribe",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    Steam.AddTransaction(new UGCTransactionProcessor
                    {
                        Transaction = new UGCSubscribe(mod.m_nPublishedFileId),
                        StatusMessageDisplay = sender
                    });
                }
            });

            lineItem.AddChild(new Widget
            {
                Font = "font8",
                Text = String.Format("Name: {0}\nDescription: {1}", mod.m_rgchTitle, mod.m_rgchDescription),
                AutoLayout = AutoLayout.DockFill,
                TextColor = new Vector4(0, 0, 0, 1)
            });

            lineItem.Tag = mod;
            list.AddItem(lineItem);
        }

        private void RefreshList(List<SteamUGCDetails_t> Mods)
        {
            List.ClearItems();
            foreach (var mod in Mods)
                CreateLineItem(List, mod);
        }
    }
}