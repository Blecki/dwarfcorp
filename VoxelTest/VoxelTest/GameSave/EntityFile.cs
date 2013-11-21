using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{

    public class EntityFile : SaveData
    {
        public string Type { get; set; }
        public Vector3 Position { get; set; }
        public float Scale { get; set; }
        public uint ID { get; set; }

        public new static string Extension = "ent";
        public new static string CompressedExtension = "zent";

        public EntityFile()
        {
        }

        public EntityFile(string path, bool isCompressed)
        {
            ReadFile(path, isCompressed);
        }

        public EntityFile(uint id, string type, Matrix transform, float scale)
        {
            ID = id;
            Type = type;
            Position = transform.Translation;
            Scale = scale;
        }

        public void CopyFrom(EntityFile other)
        {
            Type = other.Type;
            Position = other.Position;
            Scale = other.Scale;
            ID = other.ID;
        }

        public override sealed bool ReadFile(string filePath, bool isCompressed)
        {
            EntityFile file = FileUtils.LoadJson<EntityFile>(filePath, isCompressed);

            if(file == null)
            {
                return false;
            }
            else
            {
                CopyFrom(file);
                return true;
            }
        }

        public override bool WriteFile(string filePath, bool compress)
        {
            return FileUtils.SaveJSon<EntityFile>(this, filePath, compress);
        }


        public virtual GameComponent CreateComponent(ComponentManager components, GraphicsDevice graphics, Microsoft.Xna.Framework.Content.ContentManager content, ChunkManager chunks, GameMaster master, Camera camera)
        {
            GameComponent toReturn = EntityFactory.GenerateComponent(Type, Position, components, content, graphics, chunks, master, camera);

            if(toReturn != null)
            {
                toReturn.GlobalID = ID;
            }

            return toReturn;
        }
    }

}