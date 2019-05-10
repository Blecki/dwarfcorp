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

        private static string GenerateRandomMotto()
        {
            var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.mottos);
            return TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
        }

        private static string GenerateRandomName()
        {
            var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.company);
            return TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
        }
    }
}