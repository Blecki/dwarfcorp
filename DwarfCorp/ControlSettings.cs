// ControlSettings.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
            FileUtils.SaveBasicJson(Mappings, file);
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
