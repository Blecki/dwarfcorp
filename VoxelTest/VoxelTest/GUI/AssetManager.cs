using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class AssetManager : SillyGUIComponent
    {
        public TextureLoadDialog LoadDialog { get; set; }
        public ComboBox AssetSelector { get; set; }
        public string CurrentAsset { get; set; }
        public Label AssetLabel { get; set; }

        public AssetManager(SillyGUI gui, SillyGUIComponent parent, List<string> assets) :
            base(gui, parent)
        {
            GridLayout Layout = new GridLayout(GUI, this, 6, 3);
            AssetSelector = new ComboBox(GUI, Layout);

            CurrentAsset = assets[0];
            foreach (string s in assets)
            {
                AssetSelector.AddValue(s);
            }

            AssetSelector.CurrentValue = CurrentAsset;

            AssetSelector.OnSelectionModified += new ComboBoxSelector.Modified(AssetSelector_OnSelectionModified);

            AssetLabel = new Label(GUI, Layout, CurrentAsset + " : " + TextureManager.GetStringValue(CurrentAsset), GUI.DefaultFont);

            Layout.SetComponentPosition(AssetSelector, 0, 0, 1, 1);
            Layout.SetComponentPosition(AssetLabel, 0, 1, 3, 1);

            LoadDialog = new TextureLoadDialog(GUI, Layout, CurrentAsset, TextureManager.GetTexture(CurrentAsset));

            Layout.SetComponentPosition(LoadDialog, 0, 2, 3, 4);

            LoadDialog.OnTextureSelected += new TextureLoadDialog.TextureSelected(LoadDialog_OnTextureSelected);

        }

        void LoadDialog_OnTextureSelected(TextureLoader.TextureFile arg)
        {
            TextureManager.SetStringValue(CurrentAsset, arg.File);
            AssetLabel.Text = CurrentAsset + " : " + TextureManager.GetStringValue(CurrentAsset);
        }



        void AssetSelector_OnSelectionModified(string arg)
        {
            CurrentAsset = arg;
            AssetLabel.Text = CurrentAsset + " : " + TextureManager.GetStringValue(CurrentAsset);
            LoadDialog.Initialize(TextureManager.GetTexture(CurrentAsset), CurrentAsset);
            LoadDialog.OnTextureSelected += new TextureLoadDialog.TextureSelected(LoadDialog_OnTextureSelected);
        }

    }
}
