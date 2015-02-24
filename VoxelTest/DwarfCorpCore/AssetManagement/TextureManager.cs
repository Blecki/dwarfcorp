using System.Collections.Generic;
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This class exists to provide an abstract interface between asset tags and textures. 
    /// Technically, the ContentManager already does this for XNA, but ContentManager is missing a
    /// couple of important functions: modability, and storing the *inverse* lookup between tag
    /// and texture. Additionally, the TextureManager provides an interface to directly load
    /// resources from the disk (rather than going through XNAs content system)
    /// </summary>
    public class TextureManager
    {
        public static Dictionary<string, string> DefaultContent { get; set; }
        public static Dictionary<Texture2D, string> AssetMap { get; set; }
 
        public ContentManager Content { get; set; }
        public GraphicsDevice Graphics { get; set; }
        private static bool staticsInitialized = false;

        public static TextureManager Instance { get; set; }


        public TextureManager(ContentManager content, GraphicsDevice graphics)
        {
            Content = content;
            Graphics = graphics;
            AssetMap = new Dictionary<Texture2D, string>();
            if(!staticsInitialized)
            {
                InitializeStatics();
                Instance = this;
            }
        }

        public static void InitializeStatics()
        {
            DefaultContent = new Dictionary<string, string>();
            DefaultContent["TileSet"] = ProgramData.CreatePath("Terrain", "terrain_tiles");
            DefaultContent["DwarfSheet"] = ProgramData.CreatePath("Entities" ,"Dwarf" , "Sprites", "dwarf_animations");
            DefaultContent["InteriorSheet"] = ProgramData.CreatePath("Entities", "Furniture", "interior_furniture");
            DefaultContent["GUISheet"] = ProgramData.CreatePath("GUI", "gui_widgets");
            DefaultContent["GoblinSheet"] = ProgramData.CreatePath("Entities", "Goblin", "Sprites", "goblin_withsword");
            DefaultContent["IconSheet"] = ContentPaths.GUI.icons;
            DefaultContent["CompanyLogo"] = ContentPaths.Logos.companylogo;
            DefaultContent["CorpLogo"] = ContentPaths.Logos.grebeardlogo;
            DefaultContent["ResourceSheet"] = ProgramData.CreatePath("Entities", "Resources", "resources");
            staticsInitialized = true;
        }




        public static Texture2D GetTexture(string asset)
        {
            Texture2D toReturn = Instance.GetInstanceTexture(asset);
            
            if(!DefaultContent.ContainsKey(asset))
            {
                DefaultContent[asset] = asset;
            }

            AssetMap[toReturn] = DefaultContent[asset];
            return toReturn;
        }

        public static Texture2D LoadTexture(string asset)
        {
            Texture2D toReturn = LoadInstanceTexture(asset);

            if(!DefaultContent.ContainsKey(asset))
            {
                DefaultContent[asset] = asset;
            }

            AssetMap[toReturn] = DefaultContent[asset];
            return toReturn;
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
                case "CorpLogo":
                    return ContentPaths.Logos.grebeardlogo;
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
                default:
                    return Content.Load<Texture2D>(DefaultContent[asset]);
               // default:
               //     return LoadInstanceTexture(assetValue);
            }
        }

        public static Texture2D LoadInstanceTexture(string file)
        {
            Texture2D texture = null;
            FileStream stream = new FileStream(file, FileMode.Open);
            texture = Texture2D.FromStream(GameState.Game.GraphicsDevice, stream);
            stream.Close();

            return texture;
        }
    }

}