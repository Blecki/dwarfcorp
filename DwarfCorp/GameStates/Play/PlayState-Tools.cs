using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp.GameStates
{
    public partial class PlayState : GameState
    {
        private Dictionary<String, PlayerTool> Tools;
        private PlayerTool CurrentTool { get { return Tools[CurrentToolMode]; } }
        private String CurrentToolMode = "SelectUnits";

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

            if (Tools.ContainsKey(Mode))
            {
                CurrentTool.OnEnd();
                CurrentToolMode = Mode;
                CurrentTool.OnBegin(Arguments);
            }
        }
    }
}
