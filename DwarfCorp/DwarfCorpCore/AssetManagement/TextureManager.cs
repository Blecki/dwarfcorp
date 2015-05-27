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
            staticsInitialized = true;
        }

        public static Texture2D GetTexture(string asset)
        {
            Texture2D toReturn = Instance.GetInstanceTexture(asset);
            return toReturn;
        }

        public static Texture2D LoadTexture(string asset)
        {
            Texture2D toReturn = LoadInstanceTexture(asset);
            return toReturn;
        }

        public Texture2D GetInstanceTexture(string asset)
        {
            Texture2D toReturn =  Content.Load<Texture2D>(asset);
            AssetMap[toReturn] = asset;
            return toReturn;
        }

        public static Texture2D LoadInstanceTexture(string file)
        {
            Texture2D texture = null;
            FileStream stream = new FileStream(file, FileMode.Open);
            texture = Texture2D.FromStream(GameState.Game.GraphicsDevice, stream);
            stream.Close();
            AssetMap[texture] = file;
            return texture;
        }
    }

}