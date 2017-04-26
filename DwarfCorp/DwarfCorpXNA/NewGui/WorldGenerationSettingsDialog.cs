using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class WorldGenerationSettingsDialog : Widget
    {
        public GameStates.WorldGenerationSettings Settings;
        private Gum.Widget NameEditBox;

        private string[] LevelStrings = new string[]
        {
            "Very Low",
            "Low",
            "Medium",
            "High",
            "Very High"
        };

        private Widget CreateCombo<T>(String Name, String Tooltip, T[] Values, Action<T> Setter, Func<T> Getter)
        {
            System.Diagnostics.Debug.Assert(Values.Length == LevelStrings.Length);

            var r = Root.ConstructWidget(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Tooltip = Tooltip
            });

            r.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 0),
                Text = Name
            });

            var combo = r.AddChild(new Gum.Widgets.ComboBox
            {
               AutoLayout = AutoLayout.DockFill, 
                Items = new List<String>(LevelStrings),
                OnSelectedIndexChanged = (sender) =>
                {
                    Setter(Values[(sender as Gum.Widgets.ComboBox).SelectedIndex]);
                }
            }) as Gum.Widgets.ComboBox;

            var index = (new List<T>(Values)).IndexOf(Getter());
            if (index == -1)
                combo.SelectedIndex = 2;
            else
                combo.SelectedIndex = index;

            return r;
        }
        
        public override void Construct()
        {
            PopupDestructionType = PopupDestructionType.Keep;
            Padding = new Margin(2, 2, 2, 2);
            //Set size and center on screen.
            Rect = new Rectangle(0, 0, 400, 400);
            Rect.X = (Root.VirtualScreen.Width / 2) - 200;
            Rect.Y = (Root.VirtualScreen.Height / 2) - 200;

            Border = "border-fancy";

            AddChild(new Gum.Widgets.Button
            {
                Text = "Okay",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) => this.Close(),
                AutoLayout = AutoLayout.FloatBottomRight
            });

            var topRow = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            topRow.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockLeft,
                MinimumSize = new Point(64, 0),
                Text = "Name"
            });

            topRow.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockRight,
                Border = "border-button",
                Text = "Random",
                OnClick = (sender, args) =>
                {
                    Settings.Name = TextGenerator.GenerateRandom(TextGenerator.GetAtoms(ContentPaths.Text.Templates.worlds));
                    NameEditBox.Text = Settings.Name;
                }
            });

            NameEditBox = topRow.AddChild(new Gum.Widgets.EditableTextField
            {
                AutoLayout = AutoLayout.DockFill,
                Text = Settings.Name,
                OnTextChange = (sender) =>
                {
                    Settings.Name = sender.Text;
                }
            });


            AddChild(CreateCombo<int>("World Size", "Size of the world to generate",
                new int[] { 256, 384, 512, 1024, 2048 }, (i) =>
                {
                    Settings.Width = i;
                    Settings.Height = i;
                }, () => Settings.Width));

            AddChild(CreateCombo<int>("Natives", "Number of native civilizations",
                new int[] { 0, 2, 4, 8, 16 }, (i) => Settings.NumCivilizations = 1,
                () => Settings.NumCivilizations));


            AddChild(CreateCombo<int>("Faults", "Number of straights, seas, etc.",
                new int[] { 0, 1, 3, 5, 10 }, (i) => Settings.NumFaults = i, () => Settings.NumFaults));

            AddChild(CreateCombo<float>("Rainfall", "Amount of moisture in the world.",
                new float[] { 0.0f, 0.5f, 1.0f, 1.5f, 2.0f }, (f) => Settings.RainfallScale = f,
                () => Settings.RainfallScale));

            AddChild(CreateCombo<int>("Erosion", "How eroded is the landscape.",
                new int[] { 50, 1000, 8000, 20000, 50000 }, (i) => Settings.NumRains = i,
                () => Settings.NumRains));

            AddChild(CreateCombo<float>("Sea Level", "Height of the sea.",
                new float[] { 0.05f, 0.1f, 0.17f, 0.25f, 0.3f }, (f) => Settings.SeaLevel = f,
                () => Settings.SeaLevel));
            
            AddChild(CreateCombo<float>("Temperature", "Average temperature.",
                new float[] { 0.0f, 0.5f, 1.0f, 1.5f, 2.0f }, (f) => Settings.TemperatureScale = f,
                () => Settings.TemperatureScale));
            
            Layout();
        }
    }
}
