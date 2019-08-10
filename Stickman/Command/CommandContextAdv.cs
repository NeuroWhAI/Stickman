using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.CommandsNext;

namespace Stickman.Command
{
    class CommandContextAdv<T>
    {
        public CommandContextAdv(CommandContext ctx, T args)
        {
            Context = ctx;
            Arguments = args;
        }

        public CommandContext Context { get; }
        public T Arguments { get; }
    }
}
