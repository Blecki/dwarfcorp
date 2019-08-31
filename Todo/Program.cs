using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoList
{
    public class Program
    {
        private static CommandLineIterator ParseCommand(Tuple<CommandAttribute, Type> Command, CommandLineIterator Iterator)
        {
            var commandObject = Activator.CreateInstance(Command.Item2) as ICommand;

            try
            {                   
                Iterator = Iterator.Advance(); // Skip the command name.

                var defaultList = new List<Tuple<DefaultSwitchAttribute, System.Reflection.FieldInfo>>();

                System.Reflection.FieldInfo unknownSwitch = null;
                var unknownSwitchCaught = false;

                foreach (var member in commandObject.GetType().GetFields())
                {
                    var defaultAttribute = member.GetCustomAttributes(true).FirstOrDefault(a => a is DefaultSwitchAttribute) as DefaultSwitchAttribute;
                    if (defaultAttribute != null)
                        defaultList.Add(Tuple.Create(defaultAttribute, member));                        

                    if (member.GetCustomAttributes(true).Any(a => a is UnknownSwitchAttribute))
                        unknownSwitch = member;
                }

                defaultList = defaultList.OrderBy(t => t.Item1.Order).ToList();

                while (!Iterator.AtEnd())
                {
                    if (Iterator.Peek().StartsWith("-"))
                    {
                        var memberName = Iterator.Peek().Substring(1);
                        Iterator = Iterator.Advance();
                        bool matchingMemberFound = false;

                        foreach (var member in commandObject.GetType().GetFields())
                            if (member.Name == memberName)
                            {
                                if (member.FieldType == typeof(bool))
                                    member.SetValue(commandObject, true);
                                else if (member.FieldType == typeof(String) && member.GetCustomAttributes(true).Any(a => a is GreedyArgumentAttribute))
                                {
                                    var v = "";
                                    while (!Iterator.AtEnd())
                                    {
                                        v += Iterator.Peek();
                                        Iterator = Iterator.Advance();
                                        if (!Iterator.AtEnd())
                                            v += " ";
                                    }
                                    member.SetValue(commandObject, v);
                                }
                                else
                                {
                                    member.SetValue(commandObject, ConvertArgument(Iterator.Peek(), member.FieldType));
                                    Iterator = Iterator.Advance();
                                }
                                matchingMemberFound = true;
                            }

                        if (!matchingMemberFound)
                        {
                            if (unknownSwitch == null)
                                throw new InvalidOperationException("Unknown switch " + memberName);
                            else
                            {
                                if (unknownSwitchCaught)
                                    throw new InvalidOperationException("Caught multiple unknown switches. " + memberName);

                                unknownSwitchCaught = true;
                                unknownSwitch.SetValue(commandObject, ConvertArgument(memberName, unknownSwitch.FieldType));
                            }
                        }
                    }
                    else
                    {
                        if (defaultList.Count == 0)
                            break;

                        if (defaultList[0].Item2.FieldType == typeof(String) && defaultList[0].Item2.GetCustomAttributes(true).Any(a => a is GreedyArgumentAttribute))
                        {
                            var v = "";
                            while (!Iterator.AtEnd())
                            {
                                v += Iterator.Peek();
                                Iterator = Iterator.Advance();
                                if (!Iterator.AtEnd())
                                    v += " ";
                            }
                            defaultList[0].Item2.SetValue(commandObject, v);
                        }
                        else
                        {
                            defaultList[0].Item2.SetValue(commandObject, ConvertArgument(Iterator.Peek(), defaultList[0].Item2.FieldType));
                            Iterator = Iterator.Advance();
                        }
                        defaultList.RemoveAt(0);
                    }
                }

                commandObject.Invoke();
                return Iterator;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Try 'help -" + Command.Item1.Name + "'");
                if (!String.IsNullOrEmpty(Command.Item1.ErrorText))
                    Console.WriteLine(Command.Item1.ErrorText);
                Environment.Exit(0);
                throw new Exception();
            }
        }

        private static Object ConvertArgument(String Argument, Type DestinationType)
        {
            if (DestinationType == typeof(String))
                return Argument;
            else if (DestinationType == typeof(UInt32))
                return Convert.ToUInt32(Argument, 16);
            else if (DestinationType.IsEnum)
            {
                var value = Enum.Parse(DestinationType, Argument.ToUpperInvariant());
                return Convert.ChangeType(value, DestinationType);
            }
            else
                return Convert.ChangeType(Argument, DestinationType);
        }

        public static void Main(string[] args)
        {
            try
            {
                var commands = new Dictionary<String, Tuple<CommandAttribute, Type>>(); // Todo: Dictionary might be obsolete.
                foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                {
                    var commandAttribute = type.GetCustomAttributes(true).FirstOrDefault(a => a is CommandAttribute) as CommandAttribute;
                    if (commandAttribute != null)
                        commands[commandAttribute.Name] = Tuple.Create(commandAttribute, type);
                }

                var iterator = new CommandLineIterator(args, 0);
                while (!iterator.AtEnd())
                {
                    var commandName = iterator.Peek();
                    var command = commands.Values.FirstOrDefault(c => c.Item1.Name == commandName || c.Item1.Synonyms.Contains(commandName));
                    if (command == null)
                        throw new InvalidOperationException("Unknown command " + iterator.Peek());

                    iterator = ParseCommand(command, iterator);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}