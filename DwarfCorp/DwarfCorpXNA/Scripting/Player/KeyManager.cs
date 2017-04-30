using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// This class describes the keyboard settings used to control the game.
    /// </summary>
    public class KeyManager
    {
        public Dictionary<string, Keys> Buttons { get; set; }

        public void SaveConfigSettings()
        {
            ControlSettings.Reset();
            ControlSettings.Mappings.CameraMode = this["Rotate Camera"];
            ControlSettings.Mappings.Back = this["Back"];
            ControlSettings.Mappings.Forward = this["Forward"];
            ControlSettings.Mappings.Left = this["Left"];
            ControlSettings.Mappings.Right = this["Right"];
            ControlSettings.Mappings.SliceUp = this["Slice Up"];
            ControlSettings.Mappings.SliceDown = this["Slice Down"];
            ControlSettings.Mappings.SliceSelected = this["Goto Slice"];
            ControlSettings.Mappings.Unslice = this["Un-Slice"];
            ControlSettings.Mappings.GodMode = this["God Mode"];
            ControlSettings.Mappings.TimeForward = this["Time +"];
            ControlSettings.Mappings.TimeBackward = this["Time -"];
            ControlSettings.Mappings.ToggleGUI = this["Toggle GUI"];
            ControlSettings.Mappings.Map = this["Toggle Map"];
            ControlSettings.Mappings.Pause = this["Pause"];
            ControlSettings.Mappings.Jump = this["Jump"];
            ControlSettings.Save();
        }

        public void LoadConfigSettings()
        {
            if (ControlSettings.Mappings == null)
            {
                ControlSettings.Load();
            }

            this["Rotate Camera"] = ControlSettings.Mappings.CameraMode;
            this["Back"] = ControlSettings.Mappings.Back;
            this["Forward"] = ControlSettings.Mappings.Forward;
            this["Left"] = ControlSettings.Mappings.Left;
            this["Right"] = ControlSettings.Mappings.Right;
            this["Slice Up"] = ControlSettings.Mappings.SliceUp;
            this["Slice Down"] = ControlSettings.Mappings.SliceDown;
            this["Goto Slice"] = ControlSettings.Mappings.SliceSelected;
            this["Un-Slice"] = ControlSettings.Mappings.Unslice;
            this["God Mode"] = ControlSettings.Mappings.GodMode;
            this["Time +"] = ControlSettings.Mappings.TimeForward;
            this["Time -"] = ControlSettings.Mappings.TimeBackward;
            this["Toggle GUI"] = ControlSettings.Mappings.ToggleGUI;
            this["Toggle Map"] = ControlSettings.Mappings.Map;
            this["Pause"] = ControlSettings.Mappings.Pause;
            this["Jump"] = ControlSettings.Mappings.Jump;
        }

        public KeyManager()
        {
            Buttons = new Dictionary<string, Keys>();
            LoadConfigSettings();
        }

        public Keys GetKey(string name)
        {
            if(!Buttons.ContainsKey(name))
            {
                throw new KeyNotFoundException();
            }

            return Buttons[name];
        }


        public Keys this[string key]
        {
            get { return GetKey(key); }
            set { Buttons[key] = value; }
        }

        public static bool RotationEnabled()
        {
            KeyboardState keys = Keyboard.GetState();
            return keys.IsKeyDown(ControlSettings.Mappings.CameraMode) || keys.IsKeyDown(Keys.RightShift) || Mouse.GetState().MiddleButton == ButtonState.Pressed;
        }

        public bool IsMapped(Keys keys)
        {
            return Buttons.Any(keyPair => keyPair.Value == keys);
        }
    }

}