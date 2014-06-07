using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
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
            public Tab(string name, TabSelector selector, GUIComponent parent) :
                base(selector.GUI, parent)
            {
                Name = name;
                Selector = selector;
            }
        }

        public void SetTab(string tab)
        {
            CurrentTab = Tabs[tab];
            foreach (var pair in Tabs)
            {
                pair.Value.IsVisible = pair.Key == tab;
            }
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
                Index = Tabs.Count
            };

            Button tabButton = new Button(GUI, Layout, name, GUI.SmallFont, Button.ButtonMode.PushButton, null);
            tabButton.OnClicked += () => tabButton_OnClicked(tabButton);

            Layout.SetComponentPosition(tabButton, Tabs.Count - 1, 0, 1, 1);
            Layout.SetComponentPosition(Tabs[name], 0, 1, Layout.Cols, Layout.Rows - 1);
            return Tabs[name];
        }

        void tabButton_OnClicked(Button sender)
        {
            SetTab(sender.Text);
        }

    }
}
