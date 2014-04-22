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
    public class TextureLoader
    {
        public string Folder { get; set; }
        public string FileTypes { get; set; }
        public GraphicsDevice Graphics { get; set; }

        public class TextureFile
        {
            public Texture2D Texture;
            public string File;
            public TextureFile(Texture2D texture, string file)
            {
                Texture = texture;
                File = file;
            }
        }

        public TextureLoader(string folder, GraphicsDevice graphics)
        {
            string assemblyLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DwarfCorp";
            string relativePath = assemblyLocation + "\\" + folder;

            FileTypes = "*.png";

            Folder = relativePath;
            Graphics = graphics;

        }

        public List<TextureFile> GetTextures()
        {
            DirectoryInfo di = new DirectoryInfo(Folder);

            if (!di.Exists)
            {
                di.Create();
            }
            FileInfo[] files = di.GetFiles(FileTypes, SearchOption.TopDirectoryOnly);

            List<TextureFile> toReturn = new List<TextureFile>();
            foreach (FileInfo file in files)
            {
                Texture2D texture = null;
                FileStream stream = new FileStream(file.FullName, FileMode.Open);
                try
                {
                    texture = Texture2D.FromStream(Graphics, stream);
                }
                catch (IOException e)
                {
                    stream.Close();
                    continue;
                }

                if (texture != null)
                {
                    toReturn.Add(new TextureFile(texture, file.FullName));
                }
                stream.Close();
            }

            return toReturn;
        }
    }
}
