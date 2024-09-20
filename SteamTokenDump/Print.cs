using System;

namespace TokenDump
{
    internal sealed class Print
    {

        public static void Warning(string s)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[!] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(s);
        }

        public static void Info(string s)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("[?] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(s);
        }


        public static void Success(string s)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[+] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(s);
        }

    }
}
