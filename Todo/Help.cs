using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoList
{
    [Command(
        Name: "help",
        ShortDescription: "This is the command you typed to see this.",
        ErrorText: "Are you fucking kidding me?",
        LongHelpText: "You had to use the command correctly to see this, so I won't explain it in detail."
    )]
    internal class Help : ICommand
    {
        [DefaultSwitch(0)] public string topic = null;

        public void Invoke()
        {
            if (String.IsNullOrEmpty(topic))
            {
                Presentation.FillBar();
                Console.BackgroundColor = ConsoleColor.Black;
                foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                {
                    var commandAttribute = type.GetCustomAttributes(true).FirstOrDefault(a => a is CommandAttribute) as CommandAttribute;
                    if (commandAttribute != null)
                    {
                        Presentation.FillLine();
                        Console.Write(commandAttribute.Name);
                        foreach (var field in type.GetFields())
                        {
                            Console.Write(" -" + field.Name);
                            if (field.GetCustomAttributes(true).Any(a => a is DefaultSwitchAttribute))
                                Console.Write(" [dflt]");
                            if (field.GetCustomAttributes(true).Any(a => a is GreedyArgumentAttribute))
                                Console.Write(" [greedy]");
                        }
                        Console.WriteLine(" : " + commandAttribute.ShortDescription);
                    }
                }
                Presentation.FillBar();
                Console.ResetColor();
            }
            else
                foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                {
                    var commandAttribute = type.GetCustomAttributes(true).FirstOrDefault(a => a is CommandAttribute) as CommandAttribute;
                    if (commandAttribute != null && commandAttribute.Name == topic)
                    {
                        Presentation.FillBar();
                        Console.BackgroundColor = ConsoleColor.Black;
                        Presentation.FillLine();
                        Console.WriteLine(commandAttribute.Name);
                        Presentation.FillLine();
                        Console.WriteLine(commandAttribute.ShortDescription);
                        foreach (var field in type.GetFields())
                        {
                            Presentation.FillLine();
                            Console.Write(" -" + field.Name + " ");
                            if (field.GetCustomAttributes(true).Any(a => a is DefaultSwitchAttribute))
                                Console.Write("[dflt] ");
                            if (field.GetCustomAttributes(true).Any(a => a is GreedyArgumentAttribute))
                                Console.Write("[greedy] ");
                            Console.Write(field.FieldType.ToString() + " ");
                            var doc = field.GetCustomAttributes(true).FirstOrDefault(a => a is SwitchDocumentationAttribute) as SwitchDocumentationAttribute;
                            if (doc != null)
                                Console.Write(doc.Documentation);
                            Console.WriteLine();
                        }
                        Presentation.FillLine();
                        Console.WriteLine("Synonyms: " + String.Join(" ", commandAttribute.Synonyms));
                        Presentation.FillLine();
                        Console.WriteLine(commandAttribute.LongHelpText);
                        Presentation.FillBar();
                        Console.ResetColor();
                    }
                }
        }
    }
}