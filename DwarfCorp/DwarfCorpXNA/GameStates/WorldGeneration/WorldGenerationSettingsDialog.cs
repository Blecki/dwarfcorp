using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class WorldGenerationSettingsDialog : Widget
    {
        public GameStates.WorldGenerationSettings Settings;
        private Gui.Widget NameEditBox;

        public static string[] LevelStrings = new string[]
        {
            "Very Low",
            "Low",
            "Medium",
            "High",
            "Very High"
        };

        public enum DialogResult
        {
            Okay,
            Cancel
        }

        public DialogResult Result = DialogResult.Okay;

        public static Widget CreateCombo<T>(Gui.Root Root, String Name, String Tooltip, T[] Values, Action<T> Setter, Func<T> Getter)
        {
            global::System.Diagnostics.Debug.Assert(Values.Length == LevelStrings.Length);

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

            var combo = r.AddChild(new Gui.Widgets.ComboBox
            {
                AutoLayout = AutoLayout.DockFill, 
                Items = new List<String>(LevelStrings),
                OnSelectedIndexChanged = (sender) =>
                {
                    var box = sender as ComboBox;
                    if (box.SelectedIndex >= 0 && box.SelectedIndex < Values.Length)
                        Setter(Values[box.SelectedIndex]);
                }
            }) as Gui.Widgets.ComboBox;

            var index = (new List<T>(Values)).IndexOf(Getter());
            if (index == -1)
                combo.SelectedIndex = 2;
            else
                combo.SelectedIndex = index;

            return r;
        }
        
        public override void Construct()
        {
            Settings.Seed = Settings.Name.GetHashCode();
            PopupDestructionType = PopupDestructionType.Keep;
            Padding = new Margin(2, 2, 2, 2);
            //Set size and center on screen.
            Rect = new Rectangle(0, 0, 400, 400);
            Rect.X = (Root.RenderData.VirtualScreen.Width / 2) - 200;
            Rect.Y = (Root.RenderData.VirtualScreen.Height / 2) - 200;

            Border = "border-fancy";

            var okayButton = AddChild(new Gui.Widgets.Button
            {
                Text = "Okay",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    Result = DialogResult.Okay;
                    this.Close();
                },
                AutoLayout = AutoLayout.FloatBottomRight
            });

            AddChild(new Button
            {
                Text = "Cancel",
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Border = "border-button",
                OnClick = (sender, args) =>
                {
                    Result = DialogResult.Cancel;
                    this.Close();
                },
                AutoLayout = AutoLayout.FloatBottomRight,
                OnLayout = (sender) => sender.Rect.X -= okayButton.Rect.Width + 4
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
                    Settings.Seed = Settings.Name.GetHashCode();
                }
            });

            NameEditBox = topRow.AddChild(new Gui.Widgets.EditableTextField
            {
                AutoLayout = AutoLayout.DockFill,
                Text = Settings.Name,
                OnTextChange = (sender) =>
                {
                    Settings.Name = sender.Text;
                    Settings.Seed = Settings.Name.GetHashCode();
                }
            });


            AddChild(CreateCombo<int>(Root, "World Size", "Size of the world to generate",
                new int[] { 256, 384, 512, 1024, 2048 }, (i) =>
                {
                    Settings.Width = i;
                    Settings.Height = i;
                }, () => Settings.Width));

            AddChild(CreateCombo<int>(Root, "Natives", "Number of native civilizations",
                new int[] { 0, 2, 4, 8, 16 }, (i) => Settings.NumCivilizations = i,
                () => Settings.NumCivilizations));


            AddChild(CreateCombo<int>(Root, "Faults", "Number of straights, seas, etc.",
                new int[] { 0, 1, 3, 5, 10 }, (i) => Settings.NumFaults = i, () => Settings.NumFaults));

            AddChild(CreateCombo<float>(Root, "Rainfall", "Amount of moisture in the world.",
                new float[] { 0.0f, 0.5f, 1.0f, 1.5f, 2.0f }, (f) => Settings.RainfallScale = f,
                () => Settings.RainfallScale));

            AddChild(CreateCombo<int>(Root, "Erosion", "How eroded is the landscape.",
                new int[] { 50, 1000, 8000, 20000, 50000 }, (i) => Settings.NumRains = i,
                () => Settings.NumRains));

            AddChild(CreateCombo<float>(Root, "Sea Level", "Height of the sea.",
                new float[] { 0.05f, 0.1f, 0.17f, 0.25f, 0.3f }, (f) => Settings.SeaLevel = f,
                () => Settings.SeaLevel));
            
            AddChild(CreateCombo<float>(Root, "Temperature", "Average temperature.",
                new float[] { 0.0f, 0.5f, 1.0f, 1.5f, 2.0f }, (f) => Settings.TemperatureScale = f,
                () => Settings.TemperatureScale));

            Layout();
        }
    }
}
