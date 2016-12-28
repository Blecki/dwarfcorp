// Announcement.cs
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

using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Announcements are little messages sent to the player on events.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Announcement
    {
        /// <summary>
        ///     Delegate called whenever the player clicks on the announcement.
        /// </summary>
        public delegate void Clicked();

        /// <summary>
        ///     Unique name of the announcement.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     The message to display to the player.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     Color of the default text to display.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        ///     Icon to dispaly next tot he text.
        /// </summary>
        public ImageFrame Icon { get; set; }

        public event Clicked OnClicked;

        protected virtual void OnOnClicked()
        {
            Clicked handler = OnClicked;
            if (handler != null) handler();
        }

        public void ActivateClick()
        {
            if (OnClicked != null)
            {
                OnClicked.Invoke();
            }
        }
    }
}