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
            public Keys Pause = Keys.Space;
            public Keys Map = Keys.M;
            public Keys Employees = Keys.OemComma;
            public Keys Tasks = Keys.OemPeriod;
            public Keys Zones = Keys.N;
            public Keys Marks = Keys.V;
            public Keys SliceSelected = Keys.Q;
            public Keys Unslice = Keys.E;
            public Keys ToggleGUI = Keys.B;
            public Keys Jump = Keys.C;
            public Keys Fly = Keys.K;
            public Keys SelectAllDwarves = Keys.F;

            // Toggles a variable in GamePerformance that can be used with if-blocks to quickly change between old and new code for comparison purposes.
            // Likely should be removed for a true release as there should be no toggles left in.
            public Keys DebugToggle2 = Keys.F8;
            public Keys DebugToggle1 = Keys.F9;
            // Toggles FPS and other stat showing via GamePerformance.
            public Keys TogglePerformanceOverlay = Keys.F10;
            public Keys RotateObjectLeft = Keys.R;
            public Keys RotateObjectRight = Keys.T;

            public Keys Xray = Keys.Tab;

            // Todo: Seriously?
            public IEnumerable<Keys> GetKeys()
            {
                yield return SliceSelected;
                yield return Forward;
                yield return Left;
                yield return Right;
                yield return Back;
                yield return CameraMode;
                yield return GodMode;
                yield return TimeForward;
                yield return TimeBackward;
                yield return SliceUp;
                yield return SliceDown;
                yield return Pause;
                yield return Map;
                yield return Employees;
                yield return Tasks;
                yield return Zones;
                yield return Marks;
                yield return Unslice;
                yield return ToggleGUI;
                yield return Jump;
                yield return Fly;
                yield return SelectAllDwarves;
                yield return DebugToggle1;
                yield return DebugToggle2;
                yield return TogglePerformanceOverlay;
                yield return RotateObjectLeft;
                yield return RotateObjectRight;
                yield return Xray;
            }

            public bool Contains(Keys key)
            {
                return GetKeys().Any(k => k == key);
            }
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
            FileUtils.SaveJSON(Mappings, file);
        }

        public static void Load(string file)
        {
            try
            {
                Mappings = FileUtils.LoadJsonFromAbsolutePath<KeyMappings>(file);
            }
            catch (DirectoryNotFoundException)
            {
                Mappings = new KeyMappings();
                Save();
            }
            catch (FileNotFoundException)
            {
                Mappings = new KeyMappings();
                Save();
            }
            catch (Exception)
            {
                Mappings = new KeyMappings();
                // Don't save in this case because who the fuck knows what went wrong.
            }
        }
    }
}
