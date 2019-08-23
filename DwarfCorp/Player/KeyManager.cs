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
        public static Point TrueMousePos = new Point(0, 0);

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
            ControlSettings.Mappings.RotateObjectLeft = this["Rotate Object Left"];
            ControlSettings.Mappings.RotateObjectRight = this["Rotate Object Right"];
            ControlSettings.Mappings.SelectAllDwarves = this["Select All Dwarves"];
            ControlSettings.Mappings.Fly = this["Fly"];
            ControlSettings.Mappings.Xray = this["Xray"];
            ControlSettings.Mappings.SelectNextEmployee = this["Select Next Dwarf"];
            ControlSettings.Mappings.SelectPreviousEmployee = this["Select Previous Dwarf"];
            ControlSettings.Mappings.Employees = this["Toggle Employee List"];
            ControlSettings.Mappings.Tasks = this["Toggle Task List"];
            ControlSettings.Mappings.Zones = this["Toggle Zone List"];
            ControlSettings.Mappings.Marks = this["Toggle Mark Filter"];
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
            this["Toggle Employee List"] = ControlSettings.Mappings.Employees;
            this["Toggle Task List"] = ControlSettings.Mappings.Tasks;
            this["Toggle Zone List"] = ControlSettings.Mappings.Zones;
            this["Toggle Mark Filter"] = ControlSettings.Mappings.Marks;
            this["Pause"] = ControlSettings.Mappings.Pause;
            this["Jump"] = ControlSettings.Mappings.Jump;
            this["Rotate Object Left"] = ControlSettings.Mappings.RotateObjectLeft;
            this["Rotate Object Right"] = ControlSettings.Mappings.RotateObjectRight;
            this["Select All Dwarves"] = ControlSettings.Mappings.SelectAllDwarves;
            this["Select Next Dwarf"] = ControlSettings.Mappings.SelectNextEmployee;
            this["Select Previous Dwarf"] = ControlSettings.Mappings.SelectPreviousEmployee;
            this["Fly"] = ControlSettings.Mappings.Fly;
            this["Xray"] = ControlSettings.Mappings.Xray;
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

        public static bool RotationEnabled(OrbitCamera camera)
        {
            KeyboardState keys = Keyboard.GetState();
            bool shiftPressed = keys.IsKeyDown(ControlSettings.Mappings.CameraMode) || 
                                       keys.IsKeyDown(Keys.RightShift) || Mouse.GetState().MiddleButton == ButtonState.Pressed;

            if (camera.Control == OrbitCamera.ControlType.Walk)
            {
                return !camera.IsMouseActiveInWalk();
            }
            else
            {
                return shiftPressed;
            }
        }

        public bool IsMapped(Keys keys)
        {
            return Buttons.Any(keyPair => keyPair.Value == keys);
        }
    }

}