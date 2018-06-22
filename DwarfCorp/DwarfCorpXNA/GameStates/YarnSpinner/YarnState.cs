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
            ShowingChoices,
            QueuingLines,
            Paused
        }

        private Gui.Root GuiRoot;
        private Gui.Widgets.TextBox _Output;
        private Widget ChoicePanel;

        private Yarn.Dialogue Dialogue;
        private States State = States.Running;
        private IEnumerator<Yarn.Dialogue.RunnerResult> Runner;
        private Yarn.MemoryVariableStore Memory;
        private Dictionary<String, Action<YarnState, Ancora.AstNode, Yarn.MemoryVariableStore>> CommandHandlers = new Dictionary<string, Action<YarnState, Ancora.AstNode, Yarn.MemoryVariableStore>>();
        private Ancora.Grammar CommandGrammar;
        private List<String> QueuedLines = new List<string>();
        private Action<List<String>> QueueEndAction = null;

        public YarnState(
            String ConversationFile,
            String StartNode,
            Yarn.MemoryVariableStore Memory) :
            base(Game, "YarnState", GameState.Game.StateManager)
        {
            this.Memory = Memory;

            CommandGrammar = new YarnCommandGrammar();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(YarnCommandAttribute), typeof(void), new Type[]
            {
                typeof(YarnState),
                typeof(Ancora.AstNode),
                typeof(Yarn.MemoryVariableStore)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is YarnCommandAttribute) as YarnCommandAttribute;
                if (attribute == null) continue;
                CommandHandlers[attribute.CommandName] = (state, args, mem) => method.Invoke(null, new Object[] { state, args, mem });
            }

            Dialogue = new Yarn.Dialogue(Memory);

            Dialogue.LogDebugMessage = delegate (string message) { Console.WriteLine(message); };
            Dialogue.LogErrorMessage = delegate (string message) { Console.WriteLine("Yarn Error: " + message); };
            
            try
            {
                Dialogue.LoadFile(AssetManager.ResolveContentPath(ConversationFile), false, false, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Runner = Dialogue.Run(StartNode).GetEnumerator();
        }

        public void Output(String S)
        {
            if (_Output != null)
                _Output.AppendText(S);
        }

        public void Pause()
        {
            if (State != States.Running)
                throw new InvalidOperationException();
            State = States.Paused;
        }

        public void Unpause()
        {
            if (State != States.Paused)
                throw new InvalidOperationException();
            State = States.Running;
        }

        public void EnterQueueingAction(Action<List<String>> QueueEndAction)
        {
            State = States.QueuingLines;
            this.QueueEndAction = QueueEndAction;
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            GuiRoot.RootItem.Font = "font8";

            int w = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 550);
            int h = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Height - 256, 300);
            int x = GuiRoot.RenderData.VirtualScreen.Width / 2 - w / 2;
            int y = System.Math.Max(GuiRoot.RenderData.VirtualScreen.Height / 2 - h / 2, 280);

            _Output = GuiRoot.RootItem.AddChild(new Gui.Widgets.TextBox
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

            IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
                GuiRoot.HandleInput(@event.Message, @event.Args);

            GuiRoot.Update(gameTime.ToRealTime());

            switch (State)
            {
                case States.Running:

                    if (Runner.MoveNext())
                    {
                        var step = Runner.Current;

                        if (step is Yarn.Dialogue.LineResult line)
                        {
                            Output(line.line.text + "\n");
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
                                        Output("> " + sender.Text + "\n");
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
                            var result = CommandGrammar.ParseString(command.command.text);
                            if (result.ResultType != Ancora.ResultType.Success)
                                Output("Invalid command: " + command.command.text + "\nError: " + result.FailReason + "\n");
                            if (!CommandHandlers.ContainsKey(result.Node.Children[0].Value.ToString()))
                                Output("Unknown command: " + command.command.text + "\n");
                            else
                                CommandHandlers[result.Node.Children[0].Value.ToString()](this, result.Node, Memory);
                        }
                    }
                    else
                    {
                        
                        //Output.AppendText("End of conversation.");
                    }
                    break;

                case States.QueuingLines:

                    if (Runner.MoveNext())
                    {
                        var step = Runner.Current;

                        if (step is Yarn.Dialogue.LineResult line)
                            QueuedLines.Add(line.line.text);
                        else if (step is Yarn.Dialogue.OptionSetResult options)
                            Output("Option encountered while queuing lines.\n");
                        else if (step is Yarn.Dialogue.CommandResult command)
                        {
                            if (command.command.text == "end")
                            {
                                QueueEndAction?.Invoke(QueuedLines);
                                QueuedLines.Clear();
                                State = States.Running;
                            }
                            else
                                Output("Encountered command while queuing lines: " + command.command.text + " (only end is valid in this context)\n");
                        }
                    }
                    else
                    {

                        //Output.AppendText("End of conversation.");
                    }
                    break;
                case States.ShowingChoices:
                    break;
                case States.Paused:
                    break;
            }
        }
    
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }
}
