using System.IO;

namespace DwarfCorp
{
    public class LogWriter : StreamWriter
    {
        private Gui.Widgets.DwarfConsole ConsoleLogOutput = null;
        private global::System.Text.StringBuilder PreConsoleLogQueue = new global::System.Text.StringBuilder();
        private TextWriter _mainOut;
        public void SetConsole(Gui.Widgets.DwarfConsole Console)
        {
            this.ConsoleLogOutput = Console;
            Console.AddMessage(PreConsoleLogQueue.ToString());
        }

        public LogWriter(TextWriter mainOut, FileStream Output) : base(Output)
        {
            _mainOut = mainOut;
            AutoFlush = true;
        }

        public override void Write(char value)
        {
            _mainOut.Write(value);
            if (ConsoleLogOutput != null)
                ConsoleLogOutput.Append(value);
            else
                PreConsoleLogQueue.Append(value);

            base.Write(value);
        }

        //public override void Write(string value)
        //{
        //    if (Console != null) Console.AddMessage(value);
        //    base.Write(value);
        //}

        //public override void Write(char[] buffer)
        //{
        //    if (Console != null) foreach (var c in buffer) Console.Append(c);
        //    base.Write(buffer);
        //}

        public override void Write(char[] buffer, int index, int count)
        {
            _mainOut.Write(buffer, index, count);
            if (ConsoleLogOutput != null)
                for (var x = index; x < index + count; ++x)
                    ConsoleLogOutput.Append(buffer[x]);
            else
                PreConsoleLogQueue.Append(buffer, index, count);

            base.Write(buffer, index, count);
        }

        public override void WriteLine(string value)
        {
            foreach (var c in value) Write(c);
            Write('\n');
        }
    }
}
