using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoList
{
    public struct CommandLineIterator
    {   
        public readonly String[] Arguments;
        public readonly int Place;

        public String Peek()
        {
            return Arguments[Place];
        }

        public CommandLineIterator(String[] Arguments, int Place)
        {
            this.Arguments = Arguments;
            this.Place = Place;
        }

        public CommandLineIterator Advance()
        {
            return new CommandLineIterator(Arguments, Place + 1);
        }   

        public bool AtEnd()
        {
            return Place >= Arguments.Length;
        } 
    }

    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    sealed class DefaultSwitchAttribute : System.Attribute
    {
        public int Order = 0;

        public DefaultSwitchAttribute(int Order = 0)
        {
            this.Order = Order;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    sealed class UnknownSwitchAttribute : System.Attribute
    {

    }

    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    sealed class GreedyArgumentAttribute : System.Attribute
    {

    }

    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class CommandAttribute : System.Attribute
    {
        public string Name;
        public string ShortDescription = "";
        public string LongHelpText = "Long help not specified for this command.";
        public string ErrorText = "";

        public CommandAttribute(String Name, String ShortDescription = "", String LongHelpText = "Long help not specified for this command.", String ErrorText = "")
        {
            this.Name = Name;
            this.ShortDescription = ShortDescription;
            this.LongHelpText = LongHelpText;
            this.ErrorText = ErrorText;
        }
    }

    internal interface ICommand
    {
        void Invoke();
    }

    [Command(
        Name: "test",
        LongHelpText: "This command exists only for testing purposes. It doesn't do anything.",
        ShortDescription: "test!",
        ErrorText: "How did you manage to fuck that up?"
    )]
    internal class Test : ICommand
    {
        [DefaultSwitch] public int foo = 2;

        public void Invoke()
        {
            Console.WriteLine(foo);
        }
    }

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
            else
                return Convert.ChangeType(Argument, DestinationType);
        }

        public static void Main(string[] args)
        {
            try
            {
                var commands = new Dictionary<String, Tuple<CommandAttribute, Type>>();
                foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                {
                    var commandAttribute = type.GetCustomAttributes(true).FirstOrDefault(a => a is CommandAttribute) as CommandAttribute;
                    if (commandAttribute != null)
                        commands[commandAttribute.Name] = Tuple.Create(commandAttribute, type);
                }

                var iterator = new CommandLineIterator(args, 0);
                while (!iterator.AtEnd())
                {
                    if (!commands.ContainsKey(iterator.Peek()))
                        throw new InvalidOperationException("Unknown command " + iterator.Peek());

                    iterator = ParseCommand(commands[iterator.Peek()], iterator);
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}