using System;
using System.Collections.Generic;
using System.Linq;

namespace Tools
{
    [Command(
        Name: "help",
        ShortDescription: "This is the command you typed to see this.",
        ErrorText: "Are you fucking kidding me?",
        LongHelpText: "You had to use the command correctly to see this, so I won't explain it in detail."
    )]
    internal class Help : ICommand
    {
        [UnknownSwitch] public string topic = null;

        public void Invoke()
        {
            if (String.IsNullOrEmpty(topic))
                foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                {
                    var commandAttribute = type.GetCustomAttributes(true).FirstOrDefault(a => a is CommandAttribute) as CommandAttribute;
                    if (commandAttribute != null)
                    {
                        Console.Write(commandAttribute.Name);
                        foreach (var field in type.GetFields())
                        {
                            Console.Write(" -" + field.Name);
                            if (field.GetCustomAttributes(true).Any(a => a is DefaultSwitchAttribute))
                                Console.Write(" [dflt]");
                        }
                        Console.WriteLine(" : " + commandAttribute.ShortDescription);
                    }
                }
            else
                foreach (var type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
                {
                    var commandAttribute = type.GetCustomAttributes(true).FirstOrDefault(a => a is CommandAttribute) as CommandAttribute;
                    if (commandAttribute != null && commandAttribute.Name == topic)
                    {
                        Console.Write(commandAttribute.Name);
                        foreach (var field in type.GetFields())
                        {
                            Console.Write(" -" + field.Name);
                            if (field.GetCustomAttributes(true).Any(a => a is DefaultSwitchAttribute))
                                Console.Write(" [dflt]");
                        }
                        Console.WriteLine(" : " + commandAttribute.ShortDescription);
                        Console.WriteLine(commandAttribute.LongHelpText);
                    }
                }
        }
    }
}