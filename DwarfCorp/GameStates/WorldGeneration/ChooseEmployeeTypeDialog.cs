using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{
    public class ChooseEmployeeTypeDialog : Widget
    {
        class GeneratedApplicant
        {
            public EmployeePortrait Portrait;
            public Applicant Applicant;
        }

        public Overworld Settings;

        public WorldManager World;
        private Button HireButton;
        private Dictionary<String, GeneratedApplicant> Applicants = new Dictionary<string, GeneratedApplicant>();

        public Applicant GenerateApplicant(MaybeNull<Loadout> Loadout)
        {
            Applicant applicant = new Applicant();
            applicant.GenerateRandom(Loadout, 0, Settings.Company);
            return applicant;
        }

        public override void Construct()
        {
            Root.RegisterForUpdate(this);

            Border = "border-fancy";

            int w = Root.RenderData.VirtualScreen.Width;
            int h = Root.RenderData.VirtualScreen.Height;
            Rect = new Rectangle(Root.RenderData.VirtualScreen.Center.X - w / 2, Root.RenderData.VirtualScreen.Center.Y - h/2, w, h);

            var playerClasses = Library.EnumerateLoadouts().ToList();
            foreach (var job in playerClasses)
                Applicants.Add(job.Name, new GeneratedApplicant { Applicant = GenerateApplicant(job) });

            var left = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(5, 5, 32, 32),
                MinimumSize = new Point(48 * 2 * playerClasses.Count, 40 * 2 + 40)
            });

            var right = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockFill
            });

            var buttonRow = right.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30)
            });

            var jobDescription = right.AddChild(new Widget
            {
                MinimumSize = new Point((int)(w * 0.4f), 0),
                InteriorMargin = new Margin(16, 16, 16, 16),
                WrapWithinWords = false,
                WrapText = true,
                AutoLayout = AutoLayout.DockLeft,
                Font = "font10"
            });

            var applicantInfo = right.AddChild(new ApplicantInfo
            {
                AutoLayout = AutoLayout.DockFill
            }) as ApplicantInfo;
            
            applicantInfo.Hidden = true;
            left.AddChild(new Widget
            {
                Text = "Applicants",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 20),
                Font = "font16"
            });
            
            foreach (var job in playerClasses)
            {
                var frame = left.AddChild(new Widget()
                {
                    MinimumSize = new Point(48*2, 40*2 + 15),
                    AutoLayout = AutoLayout.DockLeft
                });

                var applicant = Applicants[job.Name];
                var layers = applicant.Applicant.GetLayers();

                applicant.Portrait = frame.AddChild(new EmployeePortrait()
                {
                    Tooltip = "Click to review applications for " + job.Name,
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Bottom,
                    OnClick = (sender, args) =>
                    {
                        jobDescription.Text = "\n\n" + (applicant.Applicant.Loadout.HasValue(out var loadout) ? loadout.Description : "");
                        jobDescription.Invalidate();
                        applicantInfo.Hidden = false;
                        HireButton.Hidden = false;
                        HireButton.Invalidate();
                        applicantInfo.Applicant = applicant.Applicant;
                    },
                    MinimumSize = new Point(48 * 2, 40 * 2),
                    MaximumSize = new Point(48 * 2, 40 * 2),
                    Sprite = layers,
                    AnimationPlayer = applicant.Applicant.GetAnimationPlayer(layers, "WalkingFORWARD")
                }) as EmployeePortrait;

                frame.AddChild(new Widget()
                {
                    Text = job.Name,
                    MinimumSize = new Point(0, 15),
                    TextColor = Color.Black.ToVector4(),
                    Font = "font8",
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Center
                });
            }

            buttonRow.AddChild(new Widget
            {
                Text = "Back",
                Border = "border-button",
                AutoLayout = AutoLayout.DockLeft,
                Font = "font16",
                OnClick = (sender, args) =>
                {
                    this.Close();
                },
                ChangeColorOnHover = true,
            });

            HireButton = buttonRow.AddChild(new Button
            {
                Text = "Hire",
                Border = "border-button",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    var applicant = applicantInfo.Applicant;
                    if (applicant != null)
                    {
                        Settings.InstanceSettings.InitalEmbarkment.Employees.Add(applicant);

                        applicantInfo.Hidden = true;
                        HireButton.Hidden = true;
                        this.Invalidate();
                        applicantInfo.Invalidate();
                        HireButton.Invalidate();

                        if (applicant.Loadout.HasValue(out var loadout))
                        {
                            var newApplicant = GenerateApplicant(applicant.Loadout);
                            Applicants[loadout.Name].Applicant = newApplicant;
                            Applicants[loadout.Name].Portrait.Sprite = newApplicant.GetLayers();
                            Applicants[loadout.Name].Portrait.AnimationPlayer = newApplicant.GetAnimationPlayer(Applicants[loadout.Name].Portrait.Sprite, "WalkingFORWARD");
                        }

                    }
                },
                Hidden = true,
                Font = "font16",
                ChangeColorOnHover = true,
            }) as Button;

            this.Layout();

            OnUpdate += (sender, time) =>
            {
                foreach (var applicant in Applicants)
                {
                    applicant.Value.Portrait.AnimationPlayer.Update(new DwarfTime(time), false, Timer.TimerMode.Real);
                    applicant.Value.Portrait.Invalidate();
                    applicant.Value.Portrait.Sprite.Update(GameStates.GameState.Game.GraphicsDevice);
                }
            };
        }
    }
}
