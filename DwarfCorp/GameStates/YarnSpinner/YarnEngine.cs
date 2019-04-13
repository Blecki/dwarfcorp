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
    public class YarnEngine
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

        private Yarn.Dialogue Dialogue;
        private States State = States.Running;
        private IEnumerator<Yarn.Dialogue.RunnerResult> Runner;
        private Yarn.MemoryVariableStore Memory;

        private class CommandHandler
        {
            public Action<YarnEngine, List<Ancora.AstNode>, Yarn.MemoryVariableStore> Action;
            public YarnCommandAttribute Settings;
        }

        private Dictionary<String, CommandHandler> CommandHandlers = new Dictionary<string, CommandHandler>();
        private Ancora.Grammar CommandGrammar;
        private List<String> QueuedLines = new List<string>();
        private Action<List<String>> QueueEndAction = null;

        public IYarnPlayerInterface PlayerInterface;

        public bool SkipNextLine = false;

        public YarnEngine(
            String ConversationFile,
            String StartNode,
            Yarn.MemoryVariableStore Memory,
            IYarnPlayerInterface PlayerInterface)
        {
            this.PlayerInterface = PlayerInterface;
            this.Memory = Memory;

            CommandGrammar = new YarnCommandGrammar();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(YarnCommandAttribute), typeof(void), new Type[]
            {
                typeof(YarnEngine),
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
            
            //try
            {
                Dialogue.LoadFile(AssetManager.ResolveContentPath(ConversationFile), false, false, null);
            }
            //catch (Exception e)
            {
              //  Console.Error.WriteLine(e.ToString());
            }

            Runner = Dialogue.Run(StartNode).GetEnumerator();
        }

        public void CancelSpeech()
        {
            PlayerInterface.CancelSpeech();
            State = States.Paused;
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

        public void EnterSpeakState()
        {
            State = States.Speaking;
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

        public void Update(DwarfTime gameTime)
        {
            switch (State)
            {
                case States.Running:

                    if (Runner.MoveNext())
                    {
                        var step = Runner.Current;

                        if (step is Yarn.Dialogue.LineResult line)
                        {
                            PlayerInterface.Speak(line.line.text + "\n");
                        }
                        else if (step is Yarn.Dialogue.OptionSetResult options)
                        {
                            PlayerInterface.ClearChoices();
                            var index = 0;
                            foreach (var option in options.options.options)
                            {
                                var indexLambda = index;
                                PlayerInterface.AddChoice(option, () =>
                                {
                                    options.setSelectedOptionDelegate(indexLambda);
                                    State = States.Running;
                                });
                                index += 1;
                            }
                            PlayerInterface.DoneAddingChoices();
                            State = States.ShowingChoices;
                        }
                        else if (step is Yarn.Dialogue.CommandResult command)
                        {
                            var result = CommandGrammar.ParseString(command.command.text);
                            if (result.ResultType != Ancora.ResultType.Success)
                                PlayerInterface.Output("Invalid command: " + command.command.text + "\nError: " + result.FailReason + "\n");
                            if (!CommandHandlers.ContainsKey(result.Node.Children[0].Value.ToString()))
                                PlayerInterface.Output("Unknown command: " + command.command.text + "\n");
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
                                        PlayerInterface.Output(String.Format("Passed {0} arguments to {1}; expected {2}\n", result.Node.Children.Count, handler.Settings.CommandName, handler.Settings.ArgumentTypes.Count));
                                        errorFound = true;
                                    }

                                    for (var i = 0; !errorFound && i < result.Node.Children.Count; ++i)
                                    {
                                        var expectedType = i >= handler.Settings.ArgumentTypes.Count ? handler.Settings.ArgumentTypes.Last() : handler.Settings.ArgumentTypes[i];

                                        if (result.Node.Children[i].NodeType != expectedType)
                                        {
                                            PlayerInterface.Output(String.Format("Wrong argument type passed to {0}. Expected {1}, got {2}.\n", handler.Settings.CommandName, expectedType, result.Node.Children[i].NodeType));
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
                            PlayerInterface.Output("Option encountered while queuing lines.\n");
                        else if (step is Yarn.Dialogue.CommandResult command)
                        {
                            if (command.command.text == "end")
                            {
                                State = States.Running;
                                QueueEndAction?.Invoke(QueuedLines);
                                QueuedLines.Clear();
                            }
                            else
                                PlayerInterface.Output("Encountered command while queuing lines: " + command.command.text + " (only end is valid in this context)\n");
                            // Todo: Allow commands in pick - runs one at random.
                        }
                    }
                    else
                    {
                        State = States.ConversationOver;
                    }
                    break;
                case States.Speaking:
                    if (!PlayerInterface.AdvanceSpeech(gameTime))
                        State = States.Running;
                    break;
                case States.ShowingChoices:
                    break;
                case States.Paused:
                    break;
                case States.ConversationOver:
                    PlayerInterface.ClearChoices();
                    PlayerInterface.AddChoice("End conversation.", () => PlayerInterface.EndConversation());
                    PlayerInterface.DoneAddingChoices();
                    State = States.ShowingChoices;
                    break;
            }
        }
    }
}
