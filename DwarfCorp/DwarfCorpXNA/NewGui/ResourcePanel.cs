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

                    var icon = existingResourceEntries.FirstOrDefault(w => (w.Tag as String) == resource.Key);
                    if (icon == null)
                    {
                        icon = AddChild(new Gum.Widget
                        {
                            Background = new Gum.TileReference("resources", resourceTemplate.NewGuiSprite),
                            Tooltip = string.Format("{0} - {1}",
                                    resourceTemplate.ResourceName,
                                    resourceTemplate.Description),
                            TextHorizontalAlign = HorizontalAlign.Right,
                            TextVerticalAlign = VerticalAlign.Bottom,
                            TextColor = new Vector4(1,1,1,1)
                        });                        
                    }
                    else
                        AddChild(icon);

                    icon.Text = resource.Value.NumResources.ToString();
                    icon.Invalidate();                    
                }

                Layout();
            };
        }        
    }
}
