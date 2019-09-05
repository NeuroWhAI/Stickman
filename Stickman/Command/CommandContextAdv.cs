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
            Argument = args;
        }

        public CommandContext Context { get; }
        public T Argument { get; }
    }

    class CommandContextAdv
    {
        private CommandContextAdv(CommandContext ctx)
        {
            Context = ctx;
        }

        public CommandContext Context { get; }

        public static CommandContextAdv Create(CommandContext ctx)
        {
            return new CommandContextAdv(ctx);
        }

        public static CommandContextAdv<T> Create<T>(CommandContext ctx, T args)
        {
            return new CommandContextAdv<T>(ctx, args);
        }

        public static CommandContextAdv<Tuple<T1, T2>> Create<T1, T2>(CommandContext ctx, T1 arg1, T2 arg2)
        {
            return new CommandContextAdv<Tuple<T1, T2>>(ctx, Tuple.Create(arg1, arg2));
        }

        public static CommandContextAdv<Tuple<T1, T2, T3>> Create<T1, T2, T3>(CommandContext ctx, T1 arg1, T2 arg2, T3 arg3)
        {
            return new CommandContextAdv<Tuple<T1, T2, T3>>(ctx, Tuple.Create(arg1, arg2, arg3));
        }

        public static CommandContextAdv<Tuple<T1, T2, T3, T4>> Create<T1, T2, T3, T4>(CommandContext ctx, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return new CommandContextAdv<Tuple<T1, T2, T3, T4>>(ctx, Tuple.Create(arg1, arg2, arg3, arg4));
        }
    }
}
