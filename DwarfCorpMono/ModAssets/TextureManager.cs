using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{
    public class TextureManager
    {
        private static string DefaultString = "Default";
        public static Dictionary<string, string> DefaultContent { get; set; }
        public ContentManager Content { get; set; }
        public GraphicsDevice Graphics { get; set; }
        private static bool staticsInitialized = false;

        public static TextureManager Instance { get; set; }


        public TextureManager(ContentManager content, GraphicsDevice graphics)
        {
            Content = content;
            Graphics = graphics;
            if (!staticsInitialized)
            {
                InitializeStatics();
                Instance = this;
            }
        }

        public static void InitializeStatics()
        {
            DefaultContent = new Dictionary<string, string>();
            DefaultContent["TileSet"] = "tiles2";
            DefaultContent["DwarfSheet"] = "dorfdorf";
            DefaultContent["InteriorSheet"] = "interior2_wilson";
            DefaultContent["GUISheet"] = "gui_panels";
            DefaultContent["GoblinSheet"] = "gob";
            DefaultContent["IconSheet"] = "icons";
            DefaultContent["CompanyLogo"] = "grebeardlogo";
            DefaultContent["ResourceSheet"] = "resources";
            staticsInitialized = true;
        }

        public static Texture2D GetTexture(string asset)
        {
            return Instance.GetInstanceTexture(asset);
        }

        public static Texture2D LoadTexture(string asset)
        {
            return Instance.LoadInstanceTexture(asset);
        }

        public static void SetStringValue(string asset, string v)
        {
            if (asset == "TileSet")
            {
                AssetSettings.Default.TileSet = v;
            }
            else if (asset == "DwarfSheet")
            {
                AssetSettings.Default.DwarfSheet = v;
            }
            else if (asset == "GoblinSheet")
            {
                AssetSettings.Default.GoblinSheet = v;
            }
            else if (asset == "InteriorSheet")
            {
                AssetSettings.Default.InteriorSheet = v;
            }
            else if (asset == "IconSheet")
            {
                AssetSettings.Default.IconSheet = v;
            }
            else if (asset == "GUISheet")
            {
                AssetSettings.Default.GUISheet = v;
            }
            else if (asset == "CompanyLogo")
            {
                PlayerSettings.Default.CompanyLogo = v;
            }
            else if (asset == "ResourceSheet")
            {
                AssetSettings.Default.ResourceSheet = v;
            }
        }


        public static string GetStringValue(string asset)
        {
            if (asset == "TileSet")
            {
                return AssetSettings.Default.TileSet;
            }
            else if (asset == "DwarfSheet")
            {
                return AssetSettings.Default.DwarfSheet;
            }
            else if (asset == "GoblinSheet")
            {
                return AssetSettings.Default.GoblinSheet;
            }
            else if (asset == "InteriorSheet")
            {
                return AssetSettings.Default.InteriorSheet;
            }
            else if (asset == "IconSheet")
            {
                return AssetSettings.Default.IconSheet;
            }
            else if (asset == "GUISheet")
            {
                return AssetSettings.Default.GUISheet;
            }
            else if (asset == "CompanyLogo")
            {
                return PlayerSettings.Default.CompanyLogo;
            }
            else if (asset == "ResourceSheet")
            {
                return AssetSettings.Default.ResourceSheet;
            }
            else return "";

            return "";
        }

        public Texture2D GetInstanceTexture(string asset)
        {
            string assetValue = GetStringValue(asset);

            if (assetValue == "")
            {
                return Content.Load<Texture2D>(asset);
            }
            else if (assetValue == "Default")
            {
                return Content.Load<Texture2D>(DefaultContent[asset]);
            }
            else
            {
                return LoadInstanceTexture(assetValue);
            }

        }

        public Texture2D LoadInstanceTexture(string file)
        {
            Texture2D texture = null;
            FileStream stream = new FileStream(file, FileMode.Open);
            texture = Texture2D.FromStream(Graphics, stream);
            stream.Close();

            return texture;
        }


      

    
    }
}
