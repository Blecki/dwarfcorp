using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{

    public class KeyManager
    {
        public Dictionary<string, Keys> Buttons { get; set; }

        public void SaveConfigSettings()
        {
            ControlSettings.Default.CameraMode = this["Rotate Camera"];
            ControlSettings.Default.Back = this["Back"];
            ControlSettings.Default.Forward = this["Forward"];
            ControlSettings.Default.Left = this["Left"];
            ControlSettings.Default.Right = this["Right"];
            ControlSettings.Default.SliceUp = this["Slice Up"];
            ControlSettings.Default.SliceDown = this["Slice Down"];
            ControlSettings.Default.SliceSelected = this["Goto Slice"];
            ControlSettings.Default.SliceSelectedUp = this["Goto Slice +"];
            ControlSettings.Default.GodMode = this["God Mode"];
            ControlSettings.Default.TimeForward = this["Time +"];
            ControlSettings.Default.TimeBackward = this["Time -"];
            ControlSettings.Default.ToggleGUI = this["Toggle GUI"];
            ControlSettings.Default.OrderScreen = this["Debug Order"];
            ControlSettings.Default.Map = this["Toggle Map"];
            ControlSettings.Default.Pause = this["Pause"];
            ControlSettings.Default.Save();
        }

        public void LoadConfigSettings()
        {
            this["Rotate Camera"] = ControlSettings.Default.CameraMode;
            this["Back"] = ControlSettings.Default.Back;
            this["Forward"] = ControlSettings.Default.Forward;
            this["Left"] = ControlSettings.Default.Left;
            this["Right"] = ControlSettings.Default.Right;
            this["Slice Up"] = ControlSettings.Default.SliceUp;
            this["Slice Down"] = ControlSettings.Default.SliceDown;
            this["Goto Slice"] = ControlSettings.Default.SliceSelected;
            this["Goto Slice +"] = ControlSettings.Default.SliceSelectedUp;
            this["God Mode"] = ControlSettings.Default.GodMode;
            this["Time +"] = ControlSettings.Default.TimeForward;
            this["Time -"] = ControlSettings.Default.TimeBackward;
            this["Toggle GUI"] = ControlSettings.Default.ToggleGUI;
            this["Debug Order"] = ControlSettings.Default.OrderScreen;
            this["Toggle Map"] = ControlSettings.Default.Map;
            this["Pause"] = ControlSettings.Default.Pause;
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
    }

}