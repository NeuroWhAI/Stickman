using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using Stickman.Command;
using Stickman.BotService;

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

            bot.RegisterCommand<BasicCommand>();
            bot.RegisterCommand<MemoCommand>();

            bot.GuildMemberAdded += Bot_GuildMemberAdded;

            bot.AddService(new BotStatus("BotStatus"));

            GlobalMessenger.Start();

            bot.Start();
        }

        private static async Task Bot_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            // 환영 메세지를 보냅니다.

            var embed = new DiscordEmbedBuilder()
                    .WithColor(new DiscordColor(255, 224, 160))
                    .WithTitle("회원 인증 방법")
                    .WithUrl("https://cafe.naver.com/powdertoy/51728")
                    .WithFooter("TPT&Logisim Cafe")
                    .WithDescription(@"카페 공식 디스코드 서버 입장을 환영합니다!
먼저 별명을 카페 닉네임과 동일하게 바꿔주시고
[여기](https://cafe.naver.com/powdertoy/51728)에서 댓글로 자신을 인증해주세요.
그 후 기다리시면 스탭이 멤버 권한을 부여해드릴 것입니다.");

            await e.Member.SendMessageAsync(embed: embed.Build());
        }
    }
}
