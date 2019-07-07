using System;
using System.IO;

namespace Stickman
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                using (var sr = new StreamReader("bot.txt"))
                {
                    string token = sr.ReadLine();

                    args = new string[]
                    {
                        token
                    };
                }
            }


            var bot = new DiscordBot(args[0]);

            bot.RegisterCommand<Command.BasicCommand>();

            bot.Start();
        }
    }
}
