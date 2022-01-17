using System;

namespace Registration_Program
{
    public static class Output
    {
        public static void OutputMessage(string arg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(arg);
            Console.ResetColor();
        }
        
        public static void OutputWarning(string arg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(arg);
            Console.ResetColor();
        }
        
        public static void OutputError(string arg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(arg);
            Console.ResetColor();
        }
    }
}