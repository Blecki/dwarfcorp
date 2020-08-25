using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace DwarfCorp
{
    public class PropertyPanel : Widget
    {
        public GameComponent SelectedComponent;
        public ScrollingTextBox TextBox;

        public override void Construct()
        {
            Border = "border-one";
            Font = "font10";
            OnConstruct = (sender) =>
            {
                sender.Root.RegisterForUpdate(sender);
            };

            TextBox = AddChild(new ScrollingTextBox
            {
                AutoLayout = AutoLayout.DockFill,
                ScrollOnAppend = false
            }) as ScrollingTextBox;

            OnUpdate = (sender, time) =>
            {
                if (sender.Hidden)
                {
                    if (SelectedComponent != null)
                    {
                        SelectedComponent = null;
                        TextBox.ClearText();
                    }
                    return;
                }
            };

            base.Construct();
        }

        protected override Mesh Redraw()
        {
            if (SelectedComponent == null)
                TextBox.ClearText();
            else
            {
                TextBox.ClearText();
                foreach (var field in SelectedComponent.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    var value = field.GetValue(SelectedComponent);
                    TextBox.AppendText(String.Format("F {0} {1}: {2}\n", field.FieldType.ToString(), field.Name, value == null ? "NULL" : value.ToString()));
                }

                foreach (var property in SelectedComponent.GetType().GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    var value = property.GetValue(SelectedComponent, null);
                    TextBox.AppendText(String.Format("P {0} {1}: {2}\n", property.PropertyType.ToString(), property.Name, value == null ? "NULL" : value.ToString()));
                }
            }

            return base.Redraw();
        }
    }
}
