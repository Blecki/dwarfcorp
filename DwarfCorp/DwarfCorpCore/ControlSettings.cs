using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class ControlSettings
    {
        public class KeyMappings
        {
            public Keys Forward = Keys.W;
            public Keys Left = Keys.A;
            public Keys Right = Keys.D;
            public Keys Back = Keys.S;
            public Keys CameraMode = Keys.LeftShift;
            public Keys GodMode = Keys.G;
            public Keys TimeForward = Keys.X;
            public Keys TimeBackward = Keys.Z;
            public Keys SliceUp = Keys.PageUp;
            public Keys SliceDown = Keys.PageDown;
            public Keys Pause = Keys.P;
            public Keys Map = Keys.M;
            public Keys SliceSelected = Keys.Q;
            public Keys Unslice = Keys.E;
            public Keys ToggleGUI = Keys.B;
        }

        public static KeyMappings Mappings { get; set; }

        public static void Reset()
        {
            Mappings = new KeyMappings();
        }

        public static void Save()
        {
            Save(ContentPaths.controls);
        }

        public static void Load()
        {
            Load(ContentPaths.controls);
        }

        public static void Save(string file)
        {
            FileUtils.SaveBasicJson(Mappings, file);
        }

        public static void Load(string file)
        {
            try
            {
                Mappings = FileUtils.LoadJson<KeyMappings>(file, false);
            }
            catch (FileNotFoundException fileLoad)
            {
                Mappings = new KeyMappings();
                Save();
            }
        }
    }
}
