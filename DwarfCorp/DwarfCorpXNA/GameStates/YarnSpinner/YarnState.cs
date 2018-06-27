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
        private Timer SpeakerAnimationTimer = new Timer(0, true, Timer.TimerMode.Real);
        private Gui.Mesh SpeakerRectangle = Gui.Mesh.Quad().Scale(256, 256).Translate(32, 32);
        private SpeechSynthesizer Language;
        private IEnumerator<String> CurrentSpeach;

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

        public void Output(String S, bool Speak)
        {
            if (_Output != null)
                _Output.AppendText(S);
            if (Speak)
            {
                SpeakerAnimationTimer.Reset(S.Length / 10);
                if (SpeakerAnimationPlayer != null)
                    SpeakerAnimationPlayer.Play();
                // Instead of using a timer, use the utterance.
                if (Language != null)
                {
                    CurrentSpeach = Language.Say(S).GetEnumerator();
                    State = States.Speaking;
                }
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
                Speed = Speed,
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

        public void PlayPortraitAnimation(float Time)
        {
            SpeakerAnimationTimer.Reset(Time);
        }

        public override void OnEnter()
        {
            // Geez - do we really want to rebuild the GUI each time the state is entered? It deletes the queue!

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
                Rect = new Rectangle(x, y - 260, w, 260),
                TextSize = 2
            }) as Gui.Widgets.TextBox;

            ChoicePanel = GuiRoot.RootItem.AddChild(new Widget
            {
                Rect = new Rectangle(x, y, w, h),
                Border = "border-fancy",

            });

            IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            SoundManager.Update(gameTime, null, null);

            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
                GuiRoot.HandleInput(@event.Message, @event.Args);

            if (SpeakerAnimationPlayer != null)
            {
                SpeakerAnimationTimer.Update(gameTime);

                if (SpeakerAnimationTimer.HasTriggered)
                    SpeakerAnimationPlayer.Stop();
                else
                    SpeakerAnimationPlayer.Update(gameTime, false, Timer.TimerMode.Real);
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
                            Output(line.line.text + "\n", true);
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
                                    TextSize = 2,
                                    MinimumSize = new Point(0, 20),
                                    AutoLayout = AutoLayout.DockTop,
                                    ChangeColorOnHover = true,
                                    WrapText = true,
                                    OnClick = (sender, args) =>
                                    {
                                        Output("> " + sender.Text + "\n", false);
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
                                Output("Invalid command: " + command.command.text + "\nError: " + result.FailReason + "\n", false);
                            if (!CommandHandlers.ContainsKey(result.Node.Children[0].Value.ToString()))
                                Output("Unknown command: " + command.command.text + "\n", false);
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
                                        Output(String.Format("Passed {0} arguments to {1}; expected {2}\n", result.Node.Children.Count, handler.Settings.CommandName, handler.Settings.ArgumentTypes.Count), false);
                                        errorFound = true;
                                    }

                                    for (var i = 0; !errorFound && i < result.Node.Children.Count; ++i)
                                    {
                                        var expectedType = i >= handler.Settings.ArgumentTypes.Count ? handler.Settings.ArgumentTypes.Last() : handler.Settings.ArgumentTypes[i];

                                        if (result.Node.Children[i].NodeType != expectedType)
                                        {
                                            Output(String.Format("Wrong argument type passed to {0}. Expected {1}, got {2}.\n", handler.Settings.CommandName, expectedType, result.Node.Children[i].NodeType), false);
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
                            Output("Option encountered while queuing lines.\n", false);
                        else if (step is Yarn.Dialogue.CommandResult command)
                        {
                            if (command.command.text == "end")
                            {
                                QueueEndAction?.Invoke(QueuedLines);
                                QueuedLines.Clear();
                                State = States.Running;
                            }
                            else
                                Output("Encountered command while queuing lines: " + command.command.text + " (only end is valid in this context)\n", false);
                            // Todo: Allow commands in pick - runs one at random.
                        }
                    }
                    else
                    {
                        State = States.ConversationOver;
                    }
                    break;
                case States.Speaking:
                    if (CurrentSpeach.MoveNext())
                    {
                        _Output.AppendText(CurrentSpeach.Current);
                    }
                    else
                    {
                        State = States.Running;
                    }
                    break;
                case States.ShowingChoices:
                    break;
                case States.Paused:
                    break;
                case States.ConversationOver:
                    ChoicePanel.Clear();
                    ChoicePanel.AddChild(new Widget
                    {
                        Text = "End conversation.",
                        MinimumSize = new Point(0, 20),
                        TextSize = 2,
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
