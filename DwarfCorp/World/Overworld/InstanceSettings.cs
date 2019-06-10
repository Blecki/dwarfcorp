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

        [JsonIgnore] public Embarkment InitalEmbarkment = null;
        [JsonIgnore] public Vector2 Origin => new Vector2(Cell.Bounds.X, Cell.Bounds.Y);

        public ColonyCell Cell = new ColonyCell { Bounds = new Rectangle(16, 0, 8, 8) };
    }
}
