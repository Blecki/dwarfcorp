using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ResourcePanel : GridPanel
    {
        public WorldManager World;
        
        private class AggregatedCategory
        {
            public string Category;
            public int InStockpile = 0;
            public int InBackpacks = 0;
            public List<AggregatedResource> StockpileMembers = new List<AggregatedResource>();
            public AggregatedResource Sample = null;
        }

        private class AggregatedResource
        {
            public Resource Sample;
            public int Count;
        }


        private IEnumerable<Resource> EnumerateStockpileResources()
        {
            foreach (var stockpile in World.EnumerateZones().OfType<Stockpile>())
                foreach (var res in stockpile.Resources.Enumerate())
                    if (res != null)
                        yield return res;
        }

        private IEnumerable<Resource> EnumerateMinionResources()
        {
            foreach (var creature in World.PlayerFaction.Minions)
                foreach (var i in creature.Creature.Inventory.Resources)
                    if (i != null && i.Resource != null)
                        yield return i.Resource;
        }

        private IEnumerable<AggregatedResource> AggregateResourcesByType(IEnumerable<Resource> Source)
        {
            var dict = new Dictionary<String, AggregatedResource>();
            var unstacked = new List<Resource>();
            foreach (var res in Source)
            {
                if (res == null) continue;

                if (dict.ContainsKey(res.DisplayName))
                    dict[res.DisplayName].Count += 1;
                else
                    dict.Add(res.DisplayName, new AggregatedResource { Sample = res, Count = 1 });
            }
            return dict.Values.Concat(unstacked.Select(r => new AggregatedResource { Sample = r, Count = 1 }));
        }

        private AggregatedCategory GetOrAddCategory(Dictionary<String, AggregatedCategory> Dict, String Category)
        {
            if (!Dict.ContainsKey(Category))
                Dict.Add(Category, new AggregatedCategory { Category = Category });

            return Dict[Category];
        }

        private IEnumerable<AggregatedCategory> AggregateResourcesByCategory(IEnumerable<AggregatedResource> Stockpile, IEnumerable<AggregatedResource> Minion)
        {
            var dict = new Dictionary<String, AggregatedCategory>();

            foreach (var res in Stockpile)
            {
                if (res == null) continue;

                var entry = GetOrAddCategory(dict, (String.IsNullOrEmpty(res.Sample.Category) ? res.Sample.TypeName : res.Sample.Category));
                entry.InStockpile += res.Count;
                entry.StockpileMembers.Add(res);
                if (entry.Sample == null)
                    entry.Sample = res;
            }

            foreach (var res in Minion)
            {
                if (res == null) continue;

                var entry = GetOrAddCategory(dict, (String.IsNullOrEmpty(res.Sample.Category) ? res.Sample.TypeName : res.Sample.Category));
                entry.InBackpacks += res.Count;
                if (entry.Sample == null)
                    entry.Sample = res;
            }

            return dict.Values;
        }

        private IEnumerable<AggregatedCategory> AggregateResources()
        {
            return AggregateResourcesByCategory(AggregateResourcesByType(EnumerateStockpileResources()), AggregateResourcesByType(EnumerateMinionResources()));
        }

        private String MakeDescriptionString(AggregatedResource Of)
        {
            return String.Format("{0}x {1} - {2}", Of.Count, Of.Sample.DisplayName, Of.Sample.Description);
        }
      
        public override void Construct()
        {
            EnableScrolling = false;
            OverflowBottom = true;

            Transparent = true;

            base.Construct();

            ItemSize = new Point(32, 64);
            Root.RegisterForUpdate(this);
            Background = new TileReference("basic", 0);
            BackgroundColor = new Vector4(0, 0, 0, 0.5f);

            OnUpdate = (sender, time) =>
            {
                var existingResourceEntries = new List<Widget>(Children);
                Children.Clear();

                var aggregated = AggregateResources();

                foreach (var resource in aggregated)
                {
                    var label = String.Join("\n", resource.StockpileMembers.Select(r => MakeDescriptionString(r)));

                    var icon = existingResourceEntries.FirstOrDefault(w => w is Play.ResourceIcon && w.Tag.ToString() == resource.Category);

                    if (icon == null)
                        icon = AddChild(new Play.ResourceIcon()
                        {
                            Resource = resource.Sample.Sample,
                            Tooltip = label,
                            Tag = resource.Category,
                            OverrideTooltip = false
                        });
                    else
                    {
                        icon.Tooltip = label;
                        if (!Children.Contains(icon))
                            AddChild(icon);
                        existingResourceEntries.Remove(icon);
                    }

                    var text = "S" + resource.InStockpile + "\n";
                    if (resource.InBackpacks > 0)
                        text += "I" + resource.InBackpacks;

                    icon.Text = text;
                    icon.Invalidate();
                }

                var width = Root.RenderData.VirtualScreen.Width - ItemSpacing.X;
                var itemsThatFit = width / (ItemSize.X + ItemSpacing.X);
                var sensibleWidth = (Math.Min(Children.Count, itemsThatFit) * (ItemSize.X + ItemSpacing.X)) + ItemSpacing.X;
                Rect = new Rectangle((Root.RenderData.VirtualScreen.Width / 2 - sensibleWidth / 2), 0, sensibleWidth, 0);
                Layout();
            };
        }        
    }
}
