// CompanyMakerState.cs
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
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using System.Linq;

namespace DwarfCorp
{
    public class CompanyInformation
    {
        public TileReference LogoBackground = new TileReference("company-logo-background", 0);
        public Vector4 LogoBackgroundColor = Vector4.One;
        public TileReference LogoSymbol = new TileReference("company-logo-symbol", 0);
        public Vector4 LogoSymbolColor = Vector4.One;
        public string Name = "Graybeard & Sons";
        public string Motto = "My beard is in the work!";

        public CompanyInformation()
        {
            Name = GenerateRandomName();
            Motto = GenerateRandomMotto();
            LogoSymbolColor = new Vector4(MathFunctions.Rand(0, 1), MathFunctions.Rand(0, 1), MathFunctions.Rand(0, 1), 1);
            LogoBackgroundColor = new Vector4(MathFunctions.Rand(0, 1), MathFunctions.Rand(0, 1), MathFunctions.Rand(0, 1), 1);
            LogoBackground.Tile = MathFunctions.RandInt(0, 16);
            LogoSymbol.Tile = MathFunctions.RandInt(0, 9);
        }

        public static string GenerateRandomMotto()
        {
            var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.mottos);
            return TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
        }

        public static string GenerateRandomName()
        {
            var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.company_exploration);
            return TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
        }
    }
}