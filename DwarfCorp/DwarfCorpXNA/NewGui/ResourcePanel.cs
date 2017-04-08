using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class ResourcePanel : GridPanel
    {
        private Widget TopPanel;
        private Widget HoverDisplay;
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

                    var icon = existingResourceEntries.FirstOrDefault(w => (w.Background.Tile) == resourceTemplate.NewGuiSprite);
                    if (icon == null)
                    {
                        icon = AddChild(new Gum.Widget
                        {
                            Background = 
                                resourceTemplate.Tags.Contains(DwarfCorp.Resource.ResourceTags.Craft) ?
                                new TileReference("crafts", resourceTemplate.NewGuiSprite) :
                                new TileReference("resources", resourceTemplate.NewGuiSprite),
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
