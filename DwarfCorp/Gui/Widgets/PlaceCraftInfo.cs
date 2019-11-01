using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class PlaceCraftInfo : Widget
    {
        public ResourceType Data;
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
                    Text = Data.DisplayName,
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
