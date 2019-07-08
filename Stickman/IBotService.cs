using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus;

namespace Stickman
{
    public interface IBotService
    {
        void InitService(DiscordClient discord);
        void UpdateService(DiscordClient discord);
        void DisposeService(DiscordClient discord);
    }
}
