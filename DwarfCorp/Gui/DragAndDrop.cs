using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp.Gui
{
    public static class DragAndDrop
    {
        public class DraggedItem : Widget
        {
            public Action<Widget, Widget> OnDropped = null;
            public Action<Widget> OnDropCancelled = null;
            public Func<Widget, Widget, bool> CanDropHere = null;

            public override void Construct()
            {
                Root.RegisterForUpdate(this);

                OnUpdate = (sender, time) =>
                {
                    Rect.X = Root.MousePosition.X - (Rect.Width / 2);
                    Rect.Y = Root.MousePosition.Y - (Rect.Height / 2);
                    Invalidate();

                    if (Mouse.GetState().LeftButton == ButtonState.Released)
                    {
                        bool successfullyDropped = false;

                        if (CanDropHere != null)
                        {
                            var target = Root.RootItem.EnumerateWidgetsAt(Root.MousePosition.X, Root.MousePosition.Y)
                                .Where(w => CanDropHere(this, w))
                                .FirstOrDefault();

                            if (target != null)
                            {
                                if (OnDropped != null) OnDropped(this, target);
                                successfullyDropped = true;
                            }
                        }

                        Root.Dragging = false;
                        Root.DestroyWidget(this);

                        if (!successfullyDropped && OnDropCancelled != null)
                            OnDropCancelled(this);
                    }
                };

                base.Construct();
            }
        }

        public static void BeginDrag(Root GuiRoot, DraggedItem Item)
        {
            if (Item != null)
            {
                GuiRoot.ShowMinorPopup(Item);
                GuiRoot.Dragging = true;
            }
        }
    }
}