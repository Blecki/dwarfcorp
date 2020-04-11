using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp.GameStates
{
    public static class CategoryMenuBuilder
    {
        public struct CategoryMenuCreationResult
        {
            public FlatToolTray.Icon ReturnIcon;
            public Widget Menu;
        }

        public struct SubCategoryMenuCreationResult
        {
            public FlatToolTray.Icon ReturnIcon;
            public FlatToolTray.Icon MenuIcon;
        }

        public static SubCategoryMenuCreationResult CreateCategorySubMenu(
            IEnumerable<ResourceType> Crafts, 
            Func<ResourceType, bool> Filter,
            String Category,
            Func<ResourceType, FlatToolTray.Icon> IconFactory, 
            Action<Widget, InputEventArgs> OnReturnClicked)
        {
            var icons = Crafts.Where(item => item.Category == Category).Select(data =>
            {
                var icon = IconFactory(data);
                icon.Tag = data;
                return icon;
            }).ToList();

            var returnIcon = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = OnReturnClicked
            };

            var menu = new FlatToolTray.Tray
            {
                OnRefresh = (sender) =>
                {
                    (sender as IconTray).ItemSource = (new Widget[] { returnIcon }).Concat(icons.Where(icon => Filter(icon.Tag as ResourceType))).ToList();
                    (sender as IconTray).ResetItemsFromSource();
                }
            };

            var categoryInfo = Library.GetCategoryIcon(Category).HasValue(out var catIcon) ? catIcon : null;
            if (categoryInfo == null)
                categoryInfo = new CategoryIcon
                {
                    Label = Category,
                    DynamicIcon = Crafts.Where(item => item.Category == Category).Select(item => item.Gui_Graphic).Where(icon => icon != null).FirstOrDefault(),
                    Tooltip = "Craft items in the " + Category + " category."
                };


            var menuIcon = new FlatToolTray.Icon
            {
                NewStyleIcon = categoryInfo.DynamicIcon,
                Tooltip = categoryInfo.Tooltip,
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                ReplacementMenu = menu,
                Text = categoryInfo.Label,
                TextVerticalAlign = VerticalAlign.Below,
                TextColor = Color.White.ToVector4(),
            };

            return new SubCategoryMenuCreationResult
            {
                ReturnIcon = returnIcon,
                MenuIcon = menuIcon
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Crafts">The source of items to list.</param>
        /// <param name="Filter">Filter determines which items to include.</param>
        /// <param name="IconFactory"></param>
        /// <param name="OnReturnClicked"></param>
        /// <returns></returns>
        public static CategoryMenuCreationResult CreateCategoryMenu(
            IEnumerable<ResourceType> Crafts, 
            Func<ResourceType, bool> Filter,
            Func<ResourceType, FlatToolTray.Icon> IconFactory,
            Action<Widget, InputEventArgs> OnReturnClicked)
        {
            var icons = Crafts.Select(data =>
            {
                var icon = IconFactory(data);
                icon.Tag = data;
                return icon;
            }).ToList();

            var returnIcon = new FlatToolTray.Icon
            {
                Icon = new TileReference("tool-icons", 11),
                Tooltip = "Go Back",
                Behavior = FlatToolTray.IconBehavior.ShowSubMenu,
                OnClick = OnReturnClicked
            };

            var menu = new FlatToolTray.Tray
            {
                Tag = "craft item"
            };

            menu.OnRefresh = (sender) =>
            {
                var categoryExists = new Dictionary<string, bool>();
                var rootObjects = new List<FlatToolTray.Icon>();

                foreach (var item in icons.Where(data => Filter(data.Tag as ResourceType)))
                    if (item.Tag is ResourceType craft)
                        if (string.IsNullOrEmpty(craft.Category) || !categoryExists.ContainsKey(craft.Category))
                        {
                            rootObjects.Add(item);
                            if (!string.IsNullOrEmpty(craft.Category))
                                categoryExists[craft.Category] = true;
                        }

                (sender as IconTray).ItemSource = (new Widget[] { returnIcon }).Concat(rootObjects.Select(data =>
                {
                    if (data.Tag is ResourceType craft)
                    {
                        if (string.IsNullOrEmpty(craft.Category) || Crafts.Count(c => c.Category == craft.Category) == 1)
                            return data;

                        var r = CreateCategorySubMenu(Crafts, Filter, craft.Category, IconFactory, OnReturnClicked);
                        r.ReturnIcon.ReplacementMenu = menu;

                        return r.MenuIcon;
                    }
                    throw new InvalidOperationException();
                }));

                (sender as IconTray).ResetItemsFromSource();
            };

            return new CategoryMenuCreationResult
            {
                ReturnIcon = returnIcon,
                Menu = menu
            };
        }
    }
}
