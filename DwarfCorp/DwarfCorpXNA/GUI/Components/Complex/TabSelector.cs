// TabSelector.cs
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
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DwarfCorp
{
    public class TabSelector : GUIComponent
    {
        public Dictionary<string, Tab> Tabs { get; set; }
        public GridLayout Layout { get; set; }
        public TabSelector.Tab CurrentTab { get; set; }

        public string CurrentTabName
        {
            get { return CurrentTab == null ? null : CurrentTab.Name; }
            set { SetTab(value); }
        }


        public class Tab : GUIComponent
        {
            public string Name { get; set; }
            public TabSelector Selector { get; set; }
            public int Index { get; set; }
            public Button Button { get; set; }

            public delegate void SelectedDelegate();
            public event SelectedDelegate OnSelected;

            public Tab(string name, TabSelector selector, GUIComponent parent) :
                base(selector.GUI, parent)
            {
                Name = name;
                Selector = selector;
                OnSelected = () => { };
            }

            public void Select()
            {
                OnSelected.Invoke();
            }
        }

        public void SetTab(string tab)
        {
            CurrentTab = Tabs[tab];
            foreach (var pair in Tabs)
            {
                bool isTab = pair.Key == tab;
                pair.Value.IsVisible = isTab;
                pair.Value.Button.IsToggled = isTab;
            }
            CurrentTab.Select();
            Layout.SetComponentPosition(Tabs[tab], 0, 1, Layout.Cols, Layout.Rows - 1);
            Layout.UpdateSizes();
        }

        public TabSelector(DwarfGUI gui, GUIComponent parent, int numTabs) :
            base(gui, parent)
        {
            Tabs = new Dictionary<string, Tab>();
           Layout = new GridLayout(GUI, this, 10, numTabs);
        }

        public Tab AddTab(string name)
        {
            Tabs[name] = new Tab(name, this, Layout)
            {
                Index = Tabs.Count,
            };

            Button tabButton = new Button(GUI, Layout, name, GUI.SmallFont, Button.ButtonMode.TabButton, null)
            {
                DrawFrame = false,
                CanToggle = true
            };
            Tabs[name].Button = tabButton;
            tabButton.OnClicked += () => tabButton_OnClicked(tabButton);

            Layout.SetComponentPosition(tabButton, Tabs.Count - 1, 0, 1, 1);
            Layout.SetComponentPosition(Tabs[name], 0, 1, Layout.Cols, Layout.Rows - 1);
            Layout.UpdateSizes();
            return Tabs[name];
        }

        void tabButton_OnClicked(Button sender)
        {
            SetTab(sender.Text);
        }

    }
}
