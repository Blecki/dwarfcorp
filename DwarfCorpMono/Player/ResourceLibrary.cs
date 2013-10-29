using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{
    public class ResourceLibrary
    {
        public static Dictionary<string, Resource> Resources = new Dictionary<string, Resource>();

        private static Rectangle GetRect(int x, int y)
        {
            int tileSheetWidth = AssetSettings.Default.ResourceSheet_tileWidth;
            int tileSheetHeight = AssetSettings.Default.ResourceSheet_tileHeight;
            return new Rectangle(x * tileSheetWidth, y * tileSheetHeight, tileSheetWidth, tileSheetHeight);
        }

        public ResourceLibrary(Game game)
        {
            Texture2D TileSheet = TextureManager.GetTexture("ResourceSheet");
            int tileSheetWidth = AssetSettings.Default.ResourceSheet_tileWidth;
            int tileSheetHeight = AssetSettings.Default.ResourceSheet_tileHeight;
            Resources = new Dictionary<string, Resource>();
            Resources["Wood"] = new Resource("Wood", 1.0f, "Sometimes hard to come by! Comes from trees.", new ImageFrame(TileSheet, GetRect(3, 1)), "All", "Materials", "Wood");
            Resources["Stone"] = new Resource("Stone", 0.5f, "Dwarf's favorite material! Comes from the earth.", new ImageFrame(TileSheet, GetRect(3, 0)), "All", "Materials", "Stone");
            Resources["Dirt"] = new Resource("Dirt", 0.1f, "Can't get rid of it! Comes from the earth.", new ImageFrame(TileSheet, GetRect(0, 1)), "All", "Materials", "Dirt");
            Resources["Mana"] = new Resource("Mana", 100.0f, "Mysterious properties!", new ImageFrame(TileSheet, GetRect(1, 0)), "All", "Materials", "Mana");
            Resources["Gold"] = new Resource("Gold", 50.0f, "Shiny!", new ImageFrame(TileSheet, GetRect(0, 0)), "All", "Materials", "Gold");
            Resources["Iron"] = new Resource("Iron", 5.0f, "Needed to build things.", new ImageFrame(TileSheet, GetRect(2, 0)), "All", "Materials", "Iron");
            Resources["Apple"] = new Resource("Apple", 0.5f, "Eat it.", new ImageFrame(TileSheet, GetRect(2, 1)), "All", "Foods");
            Resources["Container"] = new Resource("Container", 5.0f, "Not sure why this is a resource...", new ImageFrame(TileSheet, GetRect(0, 2)), "All", "Containers");
        }
    }
}
