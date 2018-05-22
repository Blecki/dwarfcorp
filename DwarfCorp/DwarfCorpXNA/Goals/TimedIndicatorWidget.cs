using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwarfCorp.Goals
{
    public class TimedIndicatorWidget : Gui.Widget
    {
        public Timer DeathTimer = new Timer(30.0f, true, Timer.TimerMode.Real);
        public Func<bool> ShouldKeep = null;
        public override void Construct()
        {
            var font = Root.GetTileSheet("font10") as Gui.VariableWidthFont;
            var size = font.MeasureString(Text, 256);
            // TODO (mklingensmith) why do I need this padding?
            size.X = (int)(size.X * 1.25f);
            size.Y = (int)(size.Y * 1.75f);
            Font = "font10";
            TextColor = new Microsoft.Xna.Framework.Vector4(1, 1, 1, 1);
            Border = "border-dark";
            Rect = new Microsoft.Xna.Framework.Rectangle(0, 0, size.X, size.Y);
            TextVerticalAlign = Gui.VerticalAlign.Center;
            TextHorizontalAlign = Gui.HorizontalAlign.Center;
            OnUpdate = (sender, time) =>
            {
                Update(DwarfTime.LastTime);
            };
            if (OnClick == null)
            {
                OnClick = (sender, args) =>
                {
                    Root.DestroyWidget(this);
                };
            }
            HoverTextColor = Microsoft.Xna.Framework.Color.LightGoldenrodYellow.ToVector4();
            ChangeColorOnHover = true;
            Root.RegisterForUpdate(this);
            base.Construct();
        }

        public void Update(DwarfTime time)
        {
            if (ShouldKeep == null)
            {
                DeathTimer.Update(time);
                if (DeathTimer.HasTriggered)
                {
                    Root.DestroyWidget(this);
                }
            }
            else if(!ShouldKeep())
            {
                Root.DestroyWidget(this);
            }
        }
    }
}
