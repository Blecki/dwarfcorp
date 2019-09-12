using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json;
using System.Globalization;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    public static class ComponentConsoleCommands
    {
        [ConsoleCommandHandler("FIND")]
        public static string FindEntity(String Name)
        {
            if (Debugger.ConsoleCommandContext is WorldManager World)
            {
                if (UInt32.TryParse(Name, out uint id))
                {
                    var entity = World.ComponentManager.FindComponent(id);
                    if (entity != null)
                    {
                        World.Renderer.Camera.SetZoomTarget(entity.Position);
                        World.UserInterface.BodySelector.Selected(new List<GameComponent> { entity.GetRoot() }, InputManager.MouseButton.Left);
                        return "Selected.";
                    }
                    else
                        return "Could not find entity with that id.";
                }
                else
                    return "Argument must be an integer.";
            }
            else
                return "Command is not valid in current context.";
        }

        [ConsoleCommandHandler("DELETE")]
        public static string DeleteEntity(String Name)
        {
            if (Debugger.ConsoleCommandContext is WorldManager World)
            {
                if (UInt32.TryParse(Name, out uint id))
                {
                    var entity = World.ComponentManager.FindComponent(id);
                    if (entity != null)
                    {
                        entity.GetRoot().Delete();
                        return "Deleted.";
                    }
                    else
                        return "Could not find entity with that id.";
                }
                else
                    return "Argument must be an integer.";
            }
            else
                return "Command is not valid in current context.";
        }
    }
}
