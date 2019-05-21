using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.GameStates
{
    public class InstanceSettings
    {
        [JsonIgnore] public Vector2 Origin => new Vector2(Cell.Bounds.X, Cell.Bounds.Y);
        public string ExistingFile = null;
        public ColonyCell Cell;
    }
}
