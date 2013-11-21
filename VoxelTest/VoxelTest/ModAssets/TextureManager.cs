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
            if(!staticsInitialized)
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
            switch(asset)
            {
                case "TileSet":
                    AssetSettings.Default.TileSet = v;
                    break;
                case "DwarfSheet":
                    AssetSettings.Default.DwarfSheet = v;
                    break;
                case "GoblinSheet":
                    AssetSettings.Default.GoblinSheet = v;
                    break;
                case "InteriorSheet":
                    AssetSettings.Default.InteriorSheet = v;
                    break;
                case "IconSheet":
                    AssetSettings.Default.IconSheet = v;
                    break;
                case "GUISheet":
                    AssetSettings.Default.GUISheet = v;
                    break;
                case "CompanyLogo":
                    PlayerSettings.Default.CompanyLogo = v;
                    break;
                case "ResourceSheet":
                    AssetSettings.Default.ResourceSheet = v;
                    break;
            }
        }


        public static string GetStringValue(string asset)
        {
            switch(asset)
            {
                case "TileSet":
                    return AssetSettings.Default.TileSet;
                case "DwarfSheet":
                    return AssetSettings.Default.DwarfSheet;
                case "GoblinSheet":
                    return AssetSettings.Default.GoblinSheet;
                case "InteriorSheet":
                    return AssetSettings.Default.InteriorSheet;
                case "IconSheet":
                    return AssetSettings.Default.IconSheet;
                case "GUISheet":
                    return AssetSettings.Default.GUISheet;
                case "CompanyLogo":
                    return PlayerSettings.Default.CompanyLogo;
                case "ResourceSheet":
                    return AssetSettings.Default.ResourceSheet;
                default:
                    return "";
            }
        }

        public Texture2D GetInstanceTexture(string asset)
        {
            string assetValue = GetStringValue(asset);

            switch(assetValue)
            {
                case "":
                    return Content.Load<Texture2D>(asset);
                case "Default":
                    return Content.Load<Texture2D>(DefaultContent[asset]);
                default:
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