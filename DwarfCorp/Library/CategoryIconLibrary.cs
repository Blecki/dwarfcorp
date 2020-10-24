using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static List<GameStates.CategoryIcon> CategoryIcons;
        private static bool CategoryIconsInitialized = false;

        private static void InitializeCategoryIcons()
        {
            if (CategoryIconsInitialized)
                return;
            CategoryIconsInitialized = true;

            CategoryIcons = FileUtils.LoadJsonListFromMultipleSources<GameStates.CategoryIcon>("category-icons.json", null, (i) => String.Join(".", i.Category));

            Console.WriteLine("Loaded Category Icon Library.");
        }

        public static MaybeNull<GameStates.CategoryIcon> GetCategoryIcon(String Name)
        {
            InitializeCategoryIcons();
            return CategoryIcons.FirstOrDefault(b => String.Join(".", b.Category) == Name);
        }

        public static IEnumerable<GameStates.CategoryIcon> EnumerateCategoryIcons()
        {
            InitializeCategoryIcons();
            return CategoryIcons;
        }
    }
}