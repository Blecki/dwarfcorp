using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class PlaceCraftInfo : Widget
    {
        public CraftItem Data;
        public WorldManager World;

        public override void Construct()
        {
            Border = "border-one";
            Font = "font10";
            TextColor = new Vector4(0, 0, 0, 1);

            OnShown += (sender) =>
            {
                Clear();

                var titleBar = AddChild(new Gui.Widget()
                {
                    AutoLayout = Gui.AutoLayout.DockTop,
                    MinimumSize = new Point(0, 34),
                });

                titleBar.AddChild(new Gui.Widget
                {
                    MinimumSize = new Point(32, 32),
                    MaximumSize = new Point(32, 32),
                    Background = Data.Icon,
                    AutoLayout = Gui.AutoLayout.DockLeft,
                    Text = Data.CraftedResultsCount.ToString(),
                    Font = "font10-outline-numsonly",
                    TextHorizontalAlign = HorizontalAlign.Right,
                    TextVerticalAlign = VerticalAlign.Bottom,
                    TextColor = Color.White.ToVector4()

                });
                titleBar.AddChild(new Gui.Widget
                {
                    Text = " " + Data.Name,
                    Font = "font16",
                    AutoLayout = Gui.AutoLayout.DockLeft,
                    TextVerticalAlign = VerticalAlign.Center,
                    MinimumSize = new Point(0, 34),
                    Padding = new Margin(0, 0, 16, 0)
                });

                AddChild(new Widget
                {
                    Text = Data.Description,
                    AutoLayout = AutoLayout.DockTop,
                    AutoResizeToTextHeight = true
                });

                Layout();
            };
        }

        public bool CanBuild()
        {
            return true;
        }
    }
}
