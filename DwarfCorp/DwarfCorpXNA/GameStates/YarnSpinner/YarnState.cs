using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using System;

namespace DwarfCorp
{
    public class YarnState : GameState
    {
        private enum States
        {
            Running,
            ShowingChoices
        }

        private Gui.Root GuiRoot;
        private Gui.Widgets.TextBox Output;
        private Widget ChoicePanel;
        private Yarn.Dialogue Dialogue;
        private States State = States.Running;
        private IEnumerator<Yarn.Dialogue.RunnerResult> Runner;

        public String ConversationFile = "test.conv";
        public String StartNode = "Start";

        public YarnState(
            DwarfGame Game,
            GameStateManager StateManager,
            TradeEnvoy Envoy,
            Faction PlayerFaction,
            WorldManager World) :
            base(Game, "YarnState", StateManager)
        {
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            GuiRoot.RootItem.Font = "font8";

            int w = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 550);
            int h = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Height - 256, 300);
            int x = GuiRoot.RenderData.VirtualScreen.Width / 2 - w / 2;
            int y = System.Math.Max(GuiRoot.RenderData.VirtualScreen.Height / 2 - h / 2, 280);

            Output = GuiRoot.RootItem.AddChild(new Gui.Widgets.TextBox
            {
                Border = "border-fancy",
                Rect = new Rectangle(x, y - 258, w, 258)
            }) as Gui.Widgets.TextBox;

            ChoicePanel = GuiRoot.RootItem.AddChild(new Widget
            {
                Rect = new Rectangle(x, y, w, h),
                Border = "border-fancy",
                AutoLayout = AutoLayout.DockFill
            });

            var memory = new Yarn.MemoryVariableStore();
            Dialogue = CreateDialogue(memory, ConversationFile);
            Runner = Dialogue.Run(StartNode).GetEnumerator();

            IsInitialized = true;
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    if (@event.Args.KeyValue > 0)
                    {
                        //DialogueContext.Skip();
                    }
                    // Pass event to game...
                }
            }

            GuiRoot.Update(gameTime.ToRealTime());

            switch (State)
            {
                case States.Running:

                    if (Runner.MoveNext())
                    {
                        var step = Runner.Current;

                        if (step is Yarn.Dialogue.LineResult line)
                        {
                            Output.AppendText(line.line.text + "\n");
                        }
                        else if (step is Yarn.Dialogue.OptionSetResult options)
                        {
                            ChoicePanel.Clear();
                            var index = 0;
                            foreach (var option in options.options.options)
                            {
                                var indexLambda = index;
                                ChoicePanel.AddChild(new Widget
                                {
                                    Text = option,
                                    MinimumSize = new Point(0, 20),
                                    AutoLayout = AutoLayout.DockTop,
                                    ChangeColorOnHover = true,
                                    OnClick = (sender, args) =>
                                    {
                                        Output.AppendText("> " + sender.Text + "\n");
                                        options.setSelectedOptionDelegate(indexLambda);
                                        ChoicePanel.Clear();
                                        ChoicePanel.Invalidate();
                                        State = States.Running;
                                    }
                                });
                                index += 1;
                            }
                            ChoicePanel.Layout();
                            State = States.ShowingChoices;
                        }
                        else if (step is Yarn.Dialogue.CommandResult command)
                        {

                        }
                    }
                    else
                    {
                        //Output.AppendText("End of conversation.");
                    }
                    break;
                case States.ShowingChoices:
                    break;
            }
        }
    
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }

        static internal Yarn.Dialogue CreateDialogue(Yarn.MemoryVariableStore Memory, String ConversationFile)
        {
            // Load nodes
            var dialogue = new Yarn.Dialogue(Memory);

            dialogue.LogDebugMessage = delegate (string message) { };

            dialogue.LogErrorMessage = delegate (string message)
            {
                Console.WriteLine("Yarn Error: " + message);
            };


            try
            {
                dialogue.LoadFile(AssetManager.ResolveContentPath(ConversationFile), false, false, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return dialogue;
        }
    }
}
