using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Voxels
{
    public static partial class TemplateSolidLibrary
    {
        private static Dictionary<TemplateSolidShapes, Geo.TemplateSolid> SolidTypes = new Dictionary<TemplateSolidShapes, Geo.TemplateSolid>();
        private static bool TemplateSolidLibraryInitialized = false;

        private static void InitializeDecalLibrary()
        {
            if (TemplateSolidLibraryInitialized) return;
            TemplateSolidLibraryInitialized = true;

            SolidTypes.Add(TemplateSolidShapes.SoftCube, Geo.TemplateSolid.MakeCube(true, TemplateFaceShapes.SoftSquare));
            SolidTypes.Add(TemplateSolidShapes.HardCube, Geo.TemplateSolid.MakeCube(false, TemplateFaceShapes.Square));
            SolidTypes.Add(TemplateSolidShapes.LowerSlab, Geo.TemplateSolid.MakeLowerSlab());

            Console.WriteLine("Loaded Template Solid Library.");
        }

        public static Geo.TemplateSolid GetTemplateSolid(TemplateSolidShapes Shape)
        {
            InitializeDecalLibrary();
            return SolidTypes[Shape];
        }
    }
}