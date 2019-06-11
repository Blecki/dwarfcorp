using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.GameStates
{
    public enum LoadType
    {
        CreateNew,
        LoadFromFile
    }

    public class InstanceSettings
    {
        public string ExistingFile = null;
        public LoadType LoadType = LoadType.CreateNew;

        [JsonIgnore] public Embarkment InitalEmbarkment = new Embarkment();
        [JsonIgnore] public Vector2 Origin => new Vector2(Cell.Bounds.X, Cell.Bounds.Y);

        public ColonyCell Cell = null;

        public InstanceSettings()
        {

        }

        public InstanceSettings(ColonyCell Cell)
        {
            this.Cell = Cell;
        }
        
    }
}
