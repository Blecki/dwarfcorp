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
            Paused,
            ConversationOver,
            Speaking,
        }

        private Gui.Root GuiRoot;
        private Gui.Widgets.TextBox _Output;
        private Widget ChoicePanel;

        private Yarn.Dialogue Dialogue;
        private States State = States.Running;
        private IEnumerator<Yarn.Dialogue.RunnerResult> Runner;
        private Yarn.MemoryVariableStore Memory;

        private class CommandHandler
        {
            public Action<YarnState, List<Ancora.AstNode>, Yarn.MemoryVariableStore> Action;
            public YarnCommandAttribute Settings;
        }

        private Dictionary<String, CommandHandler> CommandHandlers = new Dictionary<string, CommandHandler>();
        private Ancora.Grammar CommandGrammar;
        private List<String> QueuedLines = new List<string>();
        private Action<List<String>> QueueEndAction = null;

        private AnimationPlayer SpeakerAnimationPlayer;
        private Animation SpeakerAnimation;
        private bool SpeakerVisible = false;
        private Gui.Mesh SpeakerRectangle = null;
        private SpeechSynthesizer Language;
        private IEnumerator<String> CurrentSpeach;
        public bool SkipNextLine = false;

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
                typeof(List<Ancora.AstNode>),
                typeof(Yarn.MemoryVariableStore)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is YarnCommandAttribute) as YarnCommandAttribute;
                if (attribute == null) continue;
                CommandHandlers[attribute.CommandName] = new CommandHandler
                {
                    Action = (state, args, mem) => method.Invoke(null, new Object[] { state, args, mem }),
                    Settings = attribute
                };
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
        public void Speak(String S)
        {
            SpeakerAnimationPlayer?.Play();

            if (Language != null)
            {
                CurrentSpeach = Language.Say(S).GetEnumerator();
                State = States.Speaking;
            }
            else
            {
                _Output?.AppendText(S);
            }
        }

        public void SetLanguage(Language Language)
        {
            // Todo: Remove the reference to Language entirely
            this.Language = new SpeechSynthesizer(Language);
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

        public void EndConversation()
        {
            State = States.ConversationOver;
        }

        public void EnterQueueingAction(Action<List<String>> QueueEndAction)
        {
            State = States.QueuingLines;
            this.QueueEndAction = QueueEndAction;
        }

        public void SetPortrait(String Gfx, int FrameWidth, int FrameHeight, float Speed, List<int> Frames)
        {
            SpeakerAnimation = AnimationLibrary.CreateAnimation(new Animation.SimpleDescriptor
            {
                AssetName = Gfx,
                Speed = 1.0f/Speed,
                Frames = Frames,
                Width = FrameWidth,
                Height = FrameHeight
            });

            SpeakerAnimation.Loops = true;

            SpeakerAnimationPlayer = new AnimationPlayer(SpeakerAnimation);
            SpeakerAnimationPlayer.Play();
        }

        public void ShowPortrait()
        {
            SpeakerVisible = true;
        }

        public void HidePortrait()
        {
            SpeakerVisible = false;
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            if (GuiRoot == null)
            {
                GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
                GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
                GuiRoot.RootItem.Font = "font8";

                int w = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 550);
                int h = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Height - 256, 300);
                int x = GuiRoot.RenderData.VirtualScreen.Width / 2 - w / 2;
                int y = System.Math.Max(GuiRoot.RenderData.VirtualScreen.Height / 2 - h / 2, 280);

                _Output = GuiRoot.RootItem.AddChild(new Gui.Widgets.TextBox
                {
                    Border = "speech-bubble-reverse",
                    Rect = new Rectangle(x, y - 260, w, 260),
                    TextSize = 1,
                    Font = "font10"
                }) as Gui.Widgets.TextBox;
                SpeakerRectangle = Gui.Mesh.Quad().Scale(256, 256).Translate(x - w/2, y - 260);
                ChoicePanel = GuiRoot.RootItem.AddChild(new Widget
                {
                    Rect = new Rectangle(x, y, w, h),
                    Border = null,
                    TextSize = 1,
                    Font = "font16"
                });
                int inset = 32;
                var border = GuiRoot.RootItem.AddChild(new Widget
                {
                    Border = "border-dark",
                    Rect = new Rectangle(x - w / 2 + inset/2, y - 260 + inset, 256 - inset, 256 - inset)
                });
            }

            IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            SoundManager.Update(gameTime, null, null);

            SkipNextLine = false;
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (@event.Message == InputEvents.KeyUp || @event.Message == InputEvents.MouseClick)
                {
                    SkipNextLine = true;
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
                            Speak(line.line.text + "\n");
                        }
                        else if (step is Yarn.Dialogue.OptionSetResult options)
                        {
                            ChoicePanel.Clear();
                            var index = 0;
                            foreach (var option in options.options.options)
                            {
                                var indexLambda = index;
                                ChoicePanel.AddChild(new Gui.Widgets.Button()
                                {
                                    Text = option,
                                    MinimumSize = new Point(0, 20),
                                    AutoLayout = AutoLayout.DockTop,
                                    ChangeColorOnHover = true,
                                    WrapText = true,
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
                            {
                                var handler = CommandHandlers[result.Node.Children[0].Value.ToString()];
                                result.Node.Children.RemoveAt(0);
                                var errorFound = false;

                                if (handler.Settings.ArgumentTypeBehavior != YarnCommandAttribute.ArgumentTypeBehaviors.Unchecked)
                                {
                                    if (handler.Settings.ArgumentTypeBehavior == YarnCommandAttribute.ArgumentTypeBehaviors.Strict &&
                                        handler.Settings.ArgumentTypes.Count != result.Node.Children.Count)
                                    {
                                        Output(String.Format("Passed {0} arguments to {1}; expected {2}\n", result.Node.Children.Count, handler.Settings.CommandName, handler.Settings.ArgumentTypes.Count));
                                        errorFound = true;
                                    }

                                    for (var i = 0; !errorFound && i < result.Node.Children.Count; ++i)
                                    {
                                        var expectedType = i >= handler.Settings.ArgumentTypes.Count ? handler.Settings.ArgumentTypes.Last() : handler.Settings.ArgumentTypes[i];

                                        if (result.Node.Children[i].NodeType != expectedType)
                                        {
                                            Output(String.Format("Wrong argument type passed to {0}. Expected {1}, got {2}.\n", handler.Settings.CommandName, expectedType, result.Node.Children[i].NodeType));
                                            errorFound = true;
                                        }
                                    }
                                }

                                if (!errorFound)
                                    handler.Action(this, result.Node.Children, Memory);
                            }
                        }
                    }
                    else
                    {
                        State = States.ConversationOver;
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
                                State = States.Running;
                                QueueEndAction?.Invoke(QueuedLines);
                                QueuedLines.Clear();
                            }
                            else
                                Output("Encountered command while queuing lines: " + command.command.text + " (only end is valid in this context)\n");
                            // Todo: Allow commands in pick - runs one at random.
                        }
                    }
                    else
                    {
                        State = States.ConversationOver;
                    }
                    break;
                case States.Speaking:
                    if (!SkipNextLine)
                    {
                        if (CurrentSpeach.MoveNext())
                        {
                            SpeakerAnimationPlayer?.Update(gameTime, false, Timer.TimerMode.Real);
                            Output(CurrentSpeach.Current);
                        }
                        else
                        {
                            State = States.Running;
                            SpeakerAnimationPlayer?.Stop();
                        }
                    }
                    else
                    {
                        Language.IsSkipping = true;
                        while(CurrentSpeach.MoveNext())
                        {
                            Output(CurrentSpeach.Current);
                        }
                        State = States.Running;
                        SpeakerAnimationPlayer?.Stop();
                        SkipNextLine = false;
                    }
                    break;
                case States.ShowingChoices:
                    break;
                case States.Paused:
                    break;
                case States.ConversationOver:
                    ChoicePanel.Clear();
                    ChoicePanel.AddChild(new Gui.Widgets.Button()
                    {
                        Text = "End conversation.",
                        MinimumSize = new Point(0, 20),
                        AutoLayout = AutoLayout.DockTop,
                        ChangeColorOnHover = true,
                        OnClick = (sender, args) =>
                        {
                            StateManager.PopState();
                        }
                    });

                    ChoicePanel.Layout();
                    State = States.ShowingChoices;
                    break;
            }
        }
    
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            if (SpeakerVisible && SpeakerAnimationPlayer != null)
            {
                var sheet = SpeakerAnimationPlayer.GetCurrentAnimation().SpriteSheet;
                var frame = SpeakerAnimationPlayer.GetCurrentAnimation().Frames[SpeakerAnimationPlayer.CurrentFrame];
                SpeakerRectangle.ResetQuadTexture();
                SpeakerRectangle.Texture(sheet.TileMatrix(frame.X, frame.Y));
                GuiRoot.DrawMesh(SpeakerRectangle, sheet.GetTexture());
            }

            base.Render(gameTime);
        }
    }
}
