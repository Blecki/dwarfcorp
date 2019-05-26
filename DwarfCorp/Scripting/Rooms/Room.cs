using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A BuildRoom is a kind of zone which can be built by creatures.
    /// </summary>
    public class Room : Zone
    {
        [JsonIgnore]
        public Gui.Widget GuiTag;

        public List<VoxelHandle> Designations;
        
        public bool IsBuilt;
        private static int Counter = 0;

        public virtual String GetDescriptionString() { return Library.GetString("generic-room-description"); }

        public Room() : base()
        {
            
        }

        public Room(
            RoomData data, 
            WorldManager world, 
            Faction faction) :
            base((Counter + 1) + ". " + data.Name, world, faction, data)
        {
            Designations = new List<VoxelHandle>();
            Counter++;
        }

        public virtual void OnBuilt()
        {
            
        }

        public virtual void Update(DwarfTime Time)
        {
            ZoneBodies.RemoveAll(body => body.IsDead);
        }
    }

}
