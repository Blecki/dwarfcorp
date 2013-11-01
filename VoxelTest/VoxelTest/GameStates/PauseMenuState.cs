using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{
    public class PauseMenuState
    {
        public SillyGUI GUI { get; set; }
        public InputManager Input { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public string SaveDirectory = "Saves";
    }
}
