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

using System.IO;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class ControlSettings
    {
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

        public class KeyMappings
        {
            /// <summary>
            /// Moves the camer backwards.
            /// </summary>
            public Keys Back = Keys.S;
            /// <summary>
            /// Rotates the camera.
            /// </summary>
            public Keys CameraMode = Keys.LeftShift;
            /// <summary>
            /// Moves the camera forward.
            /// </summary>
            public Keys Forward = Keys.W;
            /// <summary>
            /// Opens up cheat mode.
            /// </summary>
            public Keys GodMode = Keys.G;
            /// <summary>
            /// Moves the camera left.
            /// </summary>
            public Keys Left = Keys.A;
            /// <summary>
            /// Toggles the minimap.
            /// </summary>
            public Keys Map = Keys.M;
            /// <summary>
            /// Toggles Game Paused state.
            /// </summary>
            public Keys Pause = Keys.P;
            /// <summary>
            /// Moves the camera to the right.
            /// </summary>
            public Keys Right = Keys.D;
            /// <summary>
            /// Decrements the vertical chunk slice.
            /// </summary>
            public Keys SliceDown = Keys.PageDown;
            /// <summary>
            /// Set the vertical slice to the selected voxel.
            /// </summary>
            public Keys SliceSelected = Keys.Q;
            /// <summary>
            /// Increment the vertical chunk slice.
            /// </summary>
            public Keys SliceUp = Keys.PageUp;
            /// <summary>
            /// Make time go backwards.
            /// </summary>
            public Keys TimeBackward = Keys.Z;
            /// <summary>
            /// Make time go fowards.
            /// </summary>
            public Keys TimeForward = Keys.X;
            /// <summary>
            /// Turn the GUI on and off.
            /// </summary>
            public Keys ToggleGUI = Keys.B;
            /// <summary>
            /// Reset the current slice level.
            /// </summary>
            public Keys Unslice = Keys.E;
        }
    }
}