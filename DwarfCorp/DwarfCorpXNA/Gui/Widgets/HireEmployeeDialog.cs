using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class HireEmployeeDialog : Widget
    {
        public Faction Faction;
        public CompanyInformation Company;
        private Button HireButton;
        public Applicant GenerateApplicant(CompanyInformation info, JobLibrary.JobType type)
        {
            Applicant applicant = new Applicant();
            applicant.GenerateRandom(JobLibrary.Classes[type], 0, info);
            return applicant;
        }

        public HireEmployeeDialog(CompanyInformation _Company)
        {
            Company = _Company;
        }

        public override void Construct()
        {
            Border = "border-fancy";

            int w = Math.Min(Math.Max(2*(Root.RenderData.VirtualScreen.Width/3), 400), 600);
            int h = Math.Min(Math.Max(2*(Root.RenderData.VirtualScreen.Height/3), 600), 700);
            Rect = new Rectangle(Root.RenderData.VirtualScreen.Center.X - w / 2, Root.RenderData.VirtualScreen.Center.Y - h/2, w, h);

            var left = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(5, 5, 32, 32),
                MinimumSize = new Point(32 * 2 * JobLibrary.Classes.Count, 48 * 2 + 40)
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
                Font = "font-hires"
            });


            foreach (var job in JobLibrary.Classes)
            {
                var newJob = job.Key;
                var frame = left.AddChild(new Widget()
                {
                    MinimumSize = new Point(32*2, 48*2 + 15),
                    AutoLayout = AutoLayout.DockLeft
                });
                var idx = EmployeePanel.GetIconIndex(job.Value.Name);
                frame.AddChild(new ImageButton()
                {
                    Tooltip = "Click to review applications for " + job.Value.Name,
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Bottom,
                    OnClick = (sender, args) =>
                    {
                        applicantInfo.Hidden = false;
                        HireButton.Hidden = false;
                        HireButton.Invalidate();
                        applicantInfo.Applicant = GenerateApplicant(Company, newJob);
                    },
                    Background = idx > 0 ? new TileReference("dwarves", idx) : null,
                    MinimumSize = new Point(32 * 2, 48 * 2),
                    MaximumSize = new Point(32 * 2, 48 * 2)
                });
                frame.AddChild(new Widget()
                {
                    Text = job.Value.Name,
                    MinimumSize = new Point(0, 15),
                    TextColor = Color.Black.ToVector4(),
                    Font = "font",
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Center
                });
            }

            buttonRow.AddChild(new Widget
            {
                Text = "Back",
                Border = "border-button",
                AutoLayout = AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                {
                    this.Close();
                }
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
                        if (applicant.Level.Pay * 4 > Faction.Economy.CurrentMoney)
                        {
                            Root.ShowModalPopup(Root.ConstructWidget(new Gui.Widgets.Popup
                            {
                                Text = "We can't afford the signing bonus!",
                            }));
                        }
                        else if (!Faction.GetRooms().Any(r => r.RoomData.Name == "BalloonPort"))
                        {
                            Root.ShowModalPopup(Root.ConstructWidget(new Gui.Widgets.Popup
                            {
                                Text = "We need a balloon port to hire someone.",
                            }));
                        }
                        else
                        {
                            Faction.Hire(applicant);
                            SoundManager.PlaySound(ContentPaths.Audio.cash, 0.5f);
                            applicantInfo.Hidden = true;
                            HireButton.Hidden = true;
                            Root.ShowModalPopup(new Gui.Widgets.Popup()
                            {
                                Text = String.Format("We hired {0}, paying a signing bonus of {1}.",
                                applicant.Name,
                                applicant.Class.Levels[0].Pay * 4),
                            });
 
                        }
                    }
                },
                Hidden = true
            }) as Button;
            this.Layout();
        }
    }
}
