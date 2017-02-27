using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static class GameObjectCaching
    {
        // Functions, constants and fields related to each cache type are in their own regions.

        #region GameComponentCache class and related fields/methods.
        [JsonObject(IsReference=true)]
        public class GameComponentCache
        {
            public string name;
            public Action<GameComponent> addFunction;
            public Action<GameComponent> removeFunction;

            public GameComponentCache(string name, Action<GameComponent> add, Action<GameComponent> remove)
            {
                this.name = name;
                addFunction = add;
                removeFunction = remove;
            }
        }

        private static Dictionary<string, GameComponentCache> cacheLookup;

        public static GameComponentCache GetCacheByName(string cacheName)
        {
            GameComponentCache cache = null;
            cacheLookup.TryGetValue(cacheName, out cache);
            return cache;
        }
        #endregion


        #region MiniMapIconCache
        public const string MiniMapIconCache = "miniMapCache";

        private static GameComponentCache miniMapIconCache;

        // Used by Minimap prerender to avoid having to call the very expensive GetChildrenOfTypeRecursive<Minimapicon>().
        // That call alone was accounting for 60% of the Minimap rendering time.
        private static List<MinimapIcon> minimapIcons;
        public static List<MinimapIcon> MinimapIcons
        {
            get { return minimapIcons; }
        }


        private static void AddMinimapIcon(GameComponent newIcon)
        {
            System.Diagnostics.Debug.Assert(newIcon is MinimapIcon);

            MinimapIcon mapIcon = newIcon as MinimapIcon;
            lock (minimapIcons)
            {
                if (minimapIcons.Contains(mapIcon)) return;
                minimapIcons.Add(mapIcon);
            }
        }

        private static void RemoveMinimapIcon(GameComponent icon)
        {
            System.Diagnostics.Debug.Assert(icon is MinimapIcon);
            MinimapIcon mapIcon = icon as MinimapIcon;
            lock (minimapIcons)
            {
                if (minimapIcons.Contains(mapIcon))
                    minimapIcons.Remove(mapIcon);
            }
        }

        #endregion

        public static void Initialize()
        {
            cacheLookup = new Dictionary<string, GameComponentCache>();
            minimapIcons = new List<MinimapIcon>();
            miniMapIconCache = new GameComponentCache(MiniMapIconCache, AddMinimapIcon, RemoveMinimapIcon);
            cacheLookup.Add(MiniMapIconCache, miniMapIconCache);

            CreateOwnRenderLookup();
        }

        public static void Reset()
        {
            minimapIcons.Clear();
        }

        #region HasOwnRender
        private static Dictionary<Type, bool> hasOwnRenderLookup;

        /// <summary>
        /// Function to use Reflection to find any GameComponent based objects that actually call Render function.
        /// Used for simple fast setting of a flag on GameComponents that still allows new types to be created
        /// without having to worry about setting that flag manually.
        /// </summary>
        private static void CreateOwnRenderLookup()
        {
            Type[] types = typeof(GameObjectCaching).Assembly.GetExportedTypes();
            Type bodyComponentType = typeof(Body);
            hasOwnRenderLookup = new Dictionary<Type, bool>();

            for(int i = 0; i < types.Length; i++)
            {
                Type type = types[i];

                if (type.Namespace != "DwarfCorp") continue;

                bool isGameComponent = false;
                Type baseType = type.BaseType;
                while(baseType != null)
                {
                    if (baseType.Name == "GameComponent")
                    {
                        isGameComponent = true;
                        break;
                    }
                    baseType = baseType.BaseType;
                }

                if (!isGameComponent) continue;

                bool hasOwnRender = false;
                Type recurseType = type;
                while (recurseType.Name != "GameComponent")
                {
                    System.Reflection.MethodInfo method = recurseType.GetMethod("Render");
                    if (method != null && method.DeclaringType != bodyComponentType)
                    {
                        hasOwnRender = true;
                        break;
                    }
                    recurseType = baseType;
                }
                hasOwnRenderLookup.Add(type, hasOwnRender);
            }
        }

        public static bool HasOwnRender(Type typeTocheck)
        {
            bool ownRender = false;

            hasOwnRenderLookup.TryGetValue(typeTocheck, out ownRender);
            return ownRender;
        }
        #endregion
    }
}
