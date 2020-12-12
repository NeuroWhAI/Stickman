using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.IO;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Stickman.Command
{
    public class PowderCommand : BaseCommandModule
    {
        [Command("id")]
        [Description("ID로 TPT 작품을 조회합니다.")]
        public async Task ShowPowderSave(CommandContext ctx,
            [RemainingText, Description("작품 ID.")]
            string id)
        {
            id = id.Trim();
            if (!uint.TryParse(id, out var nId) || nId < 1)
            {
                await ctx.RespondAsync("ID가 유효하지 않습니다.");
                return;
            }

            await ctx.TriggerTypingAsync();

            string title = null;
            string uploader = null;
            string gravatarImg = null;
            string upCnt = null;
            string downCnt = null;
            string desc = null;
            bool descMode = false;

            using (var client = new WebClient())
            {
                string url = $"https://powdertoy.co.uk/Browse/View.html?ID={id}";

                byte[] bytes;
                try
                {
                    bytes = await client.DownloadDataTaskAsync(url);
                }
                catch (WebException)
                {
                    await ctx.RespondAsync("작품을 찾을 수 없습니다.");
                    return;
                }

                using (var stream = new MemoryStream(bytes))
                using (var sr = new StreamReader(stream))
                {
                    while (!sr.EndOfStream)
                    {
                        int end = -1;
                        string line = await sr.ReadLineAsync();

                        if (title == null && line.Contains("page-header"))
                        {
                            end = TryParseData(line, "title=\"", '\"', 0, out title);
                            if (end < 0)
                            {
                                break;
                            }
                        }
                        else if (uploader == null && line.Contains("gravatar") && line.Contains("<img"))
                        {
                            end = TryParseData(line, "alt=\"", '\"', 0, out uploader);
                            if (end < 0)
                            {
                                break;
                            }

                            end = TryParseData(line, "src=\"", '\"', end, out gravatarImg);
                            if (end < 0)
                            {
                                break;
                            }

                            if (!gravatarImg.StartsWith("http"))
                            {
                                gravatarImg = "https://powdertoy.co.uk" + gravatarImg;
                            }
                        }
                        else if (upCnt == null && line.Contains("badge-success"))
                        {
                            end = TryParseData(line, "success\">", '<', 0, out upCnt);
                            if (end < 0 || !int.TryParse(upCnt, out _))
                            {
                                break;
                            }

                            end = TryParseData(line, "important\">", '<', end, out downCnt);
                            if (end < 0 || !int.TryParse(downCnt, out _))
                            {
                                upCnt = null;
                                break;
                            }
                        }
                        else if (desc == null && line.Contains("SaveDescription"))
                        {
                            descMode = true;
                            desc = string.Empty;
                        }
                        else if (desc != null && descMode)
                        {
                            string text = line.TrimStart();
                            if (text.Contains("</div>"))
                            {
                                descMode = false;
                                text = text.Replace("</div>", "");
                            }
                            text = text.TrimEnd();

                            desc += WebUtility.HtmlDecode(text) + ' ';

                            if (!descMode)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            if (title == null)
            {
                await ctx.RespondAsync("ID에 해당하는 작품을 찾을 수 없습니다.");
            }
            else if (string.IsNullOrWhiteSpace(title))
            {
                title = "　";
            }

            string buttons = $"\\[ [🎮](https://neurowhai.github.io/Stickman/ptsave.html?={id})"
                + $" / [📥](https://powdertoy.co.uk/GetSave.util?ID={id}) \\]";

            if (upCnt != null && downCnt != null)
            {
                desc = $"▲{upCnt}/▼{downCnt}　{buttons}\n\n{desc ?? string.Empty}";
            }
            else
            {
                desc = $"{buttons}\n\n{desc ?? string.Empty}";
            }

            var embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor("FDFD97"))
                .WithTitle(title)
                .WithUrl($"https://powdertoy.co.uk/Browse/View.html?ID={id}")
                .WithDescription(string.IsNullOrWhiteSpace(desc) ? "No description provided." : desc)
                .WithImageUrl($"https://static.powdertoy.co.uk/{id}.png");

            if (!string.IsNullOrWhiteSpace(uploader))
            {
                embed = embed.WithFooter(uploader, gravatarImg);
            }

            await ctx.RespondAsync(embed: embed);
        }

        private static int TryParseData(string html, string prefix, char endChar, int offset, out string attr)
        {
            attr = null;

            int begin = html.IndexOf(prefix, offset);
            if (begin < 0)
            {
                return -1;
            }

            int end = html.IndexOf(endChar, begin + prefix.Length);
            if (end < 0)
            {
                return -1;
            }

            attr = html.Substring(begin + prefix.Length, end - (begin + prefix.Length));
            attr = WebUtility.HtmlDecode(attr);

            return end;
        }
    }
}
