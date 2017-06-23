using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ResourcePanel : GridPanel
    {
        public GameMaster Master;
        
        public override void Construct()
        {
            ItemSize = new Point(32, 32);
            Root.RegisterForUpdate(this);

            OnUpdate = (sender, time) =>
            {
                var existingResourceEntries = new List<Widget>(Children);
                Children.Clear();

                foreach (var resource in Master.Faction.ListResources().Where(p => p.Value.NumResources > 0))
                {
                    var resourceTemplate = ResourceLibrary.GetResourceByName(resource.Key);

                    // Don't display resources with no value (a hack, yes!). This is to prevent "special" resources from getting traded.
                    if (resourceTemplate.MoneyValue == 0.0m)
                    {
                        continue;
                    }

                    var icon = existingResourceEntries.FirstOrDefault(w => (w.Background.Tile) == resourceTemplate.GuiSprite);

                    if (icon == null)
                    {
                        icon = AddChild(new Gui.Widget
                        {
                            Background = 
                                resourceTemplate.Tags.Contains(DwarfCorp.Resource.ResourceTags.Craft) ?
                                new TileReference("crafts", resourceTemplate.GuiSprite) :
                                new TileReference("resources", resourceTemplate.GuiSprite),
                            Tooltip = string.Format("{0} - {1}",
                                    resourceTemplate.ResourceName,
                                    resourceTemplate.Description),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            TextColor = new Vector4(1,1,1,1)
                        });                        
                    }
                    else if (!Children.Contains(icon))
                    {
                        AddChild(icon);
                    }

                    icon.Text = resource.Value.NumResources.ToString();
                    icon.Invalidate();                    
                }

                Layout();
            };
        }        
    }
}
