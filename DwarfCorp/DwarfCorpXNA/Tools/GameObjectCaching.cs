using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;

namespace DwarfCorp
{
    public static class GameObjectCaching
    {
        // Used by Minimap prerender to avoid having to call the very expensive GetChildrenOfTypeRecursive<Minimapicon>().
        // That call alone was accounting for 60% of the Minimap rendering time.
        private static List<MinimapIcon> minimapIcons;

        public static void Initialize()
        {
            minimapIcons = new List<MinimapIcon>();
        }

        public static void AddMinimapIcon(MinimapIcon newIcon)
        {
            lock (minimapIcons)
            {
                if (minimapIcons.Contains(newIcon)) return;
                minimapIcons.Add(newIcon);
            }
        }

        public static void RemoveMinimapIcon(MinimapIcon icon)
        {
            lock (minimapIcons)
            {
                if (minimapIcons.Contains(icon))
                    minimapIcons.Remove(icon);
            }
        }

        public static List<MinimapIcon> MinimapIcons
        {
            get { return minimapIcons; }
        }

        public static void Reset()
        {
            minimapIcons.Clear();
        }
    }
}
