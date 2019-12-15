using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "test",
        LongHelpText: "This command exists only for testing purposes. It doesn't do anything.",
        ShortDescription: "test!",
        ErrorText: "How did you manage to fuck that up?",
        Synonyms: "syn-test ttt"
    )]
    internal class Test : ICommand
    {
        [DefaultSwitch] [SwitchDocumentation("The value of this switch will be echoed to the console.")] public int foo = 2;

        public void Invoke(Dictionary<String, Object> PipedArguments)
        {
            Console.WriteLine(foo);
        }
    }
}