using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class GameMaster
    {
        [JsonIgnore]
        public List<GameComponent> SelectedObjects = new List<GameComponent>();

        [JsonIgnore]
        public WorldManager World { get; set; }

        public GameMaster()
        {
        }

        // Todo: Clean up construction
        public GameMaster(WorldManager World)
        {
            this.World = World;
            
            World.Master = this;
            World.Time.NewDay += Time_NewDay;
        }


        public void Destroy()
        {
            World.Time.NewDay -= Time_NewDay;
        }

        void Time_NewDay(DateTime time)
        {
            World.PlayerFaction.PayEmployees();
        }

        #region input

        public bool IsCameraRotationModeActive()
        {
            return KeyManager.RotationEnabled(World.Renderer.Camera);

        }

        #endregion
    }
}
