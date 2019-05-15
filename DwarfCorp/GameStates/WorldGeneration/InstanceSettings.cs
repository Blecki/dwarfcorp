using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.GameStates
{
    public class InstanceSettings
    {
        public Point3 ColonySize = new Point3(0, 1, 0);
        public Vector2 Origin;
        public string ExistingFile = null;
        public Rectangle SpawnRect; // Todo: Kill?
    }
}
