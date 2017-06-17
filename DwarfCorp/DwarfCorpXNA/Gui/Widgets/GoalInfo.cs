using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class GoalInfo : Widget
    {
        private Goals.Goal _goal;
        public Goals.Goal Goal
        {
            get { return _goal; }
            set { _goal = value; _goal.CreateGUI(Description); Invalidate(); }
        }

        private Widget Description;
        private Widget ActivateButton;
        private Widget BottomBar;
        public Action<Widget> OnActivateClicked;
        public WorldManager World;

        public override void Construct()
        {
            BottomBar = AddChild(new Gui.Widget
            {
                MinimumSize = new Point(0, 32),
                AutoLayout = AutoLayout.DockBottom,
                Font = "font-hires"
            });

            Description = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font-hires"
            });

            ActivateButton = BottomBar.AddChild(new Widget
            {
                Text = "Activate!",
                Font = "font-hires",
                Border = "border-button",
                AutoLayout = AutoLayout.FloatBottomRight,
                ChangeColorOnHover = true,
                OnClick = (sender, args) =>
                {
                    Root.SafeCall(OnActivateClicked, this);
                }
            });

            base.Construct();
        }

        protected override Gui.Mesh Redraw()
        {
            if (Goal != null)
            {
                if (Goal.State != Goals.GoalState.Available)
                {
                    ActivateButton.Hidden = true;
                    BottomBar.Hidden = true;
                }
                else
                {
                    BottomBar.Hidden = false;
                    var checkActivate = Goal.CanActivate(World);
                    if (checkActivate.Succeeded == false)
                    {
                        ActivateButton.Hidden = true;
                        BottomBar.Text = checkActivate.ErrorMessage;
                    }
                    else
                    {
                        ActivateButton.Hidden = false;
                        BottomBar.Text = "";
                    }
                }

                Goal.CreateGUI(Description);
            } 
            
            return base.Redraw();
        }        
    }
}
