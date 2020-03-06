using System.IO;
using System.Net.Mime;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp.GameStates
{
    public partial class PlayState : GameState
    {
        public Dictionary<String, PlayerTool> Tools; // Todo: Lock Down
        public PlayerTool CurrentTool { get { return Tools[CurrentToolMode]; } } // Todo: Lock Down
        public String CurrentToolMode = "SelectUnits";

        private void DiscoverPlayerTools()
        {
            // Setup tool list.
            Tools = new Dictionary<String, PlayerTool>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(ToolFactoryAttribute), typeof(PlayerTool), new Type[] { typeof(WorldManager) }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is ToolFactoryAttribute) as ToolFactoryAttribute;
                if (attribute == null) continue;
                Tools[attribute.Name] = method.Invoke(null, new Object[] { World }) as PlayerTool;
            }
        }

        public void ChangeTool(String Mode, Object Arguments = null)
        {
            if (MultiContextMenu != null)
            {
                MultiContextMenu.Close();
                MultiContextMenu = null;
            }

            if (Mode != "SelectUnits")
                SelectedObjects = new List<GameComponent>();

            CurrentTool.OnEnd();
            CurrentToolMode = Mode;
            CurrentTool.OnBegin(Arguments);
        }
    }
}
