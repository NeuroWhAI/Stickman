using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DSharpPlus.Entities;
using Stickman.Utility;

namespace Stickman.MemoService
{
    public class Memo
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public int Revision { get; set; }

        public Memo Clone()
        {
            return new Memo
            {
                Title = Title,
                LastModifiedTime = LastModifiedTime,
                Revision = Revision,
                Content = Content,
            };
        }

        public void Load(string filename)
        {
            LastModifiedTime = File.GetLastWriteTimeUtc(filename);

            using (var sr = new StreamReader(filename))
            {
                Title = sr.ReadLine();
                Revision = int.Parse(sr.ReadLine());
                Content = sr.ReadToEnd();

                sr.Close();
            }
        }

        public void Save(string filename)
        {
            using (var sw = new StreamWriter(filename))
            {
                sw.WriteLine(Title);
                sw.WriteLine(Revision);
                sw.Write(Content);

                sw.Close();
            }
        }

        public bool BuildCommand(string cmdline, DiscordEmbedBuilder embed)
        {
            int splitIndex = cmdline.IndexOf(' ');

            if (splitIndex >= 0)
            {
                string cmd = cmdline.Substring(0, splitIndex).Trim().ToLower();
                string arg = cmdline.Substring(splitIndex + 1).Trim();

                if (cmd == "url")
                {
                    embed.Url = arg;
                }
                else if (cmd == "img")
                {
                    embed.ImageUrl = arg;
                }
                else if (cmd == "thumbnail")
                {
                    embed.ThumbnailUrl = arg;
                }
                else if (cmd == "footer")
                {
                    embed.Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = string.IsNullOrWhiteSpace(arg) ? $"(rev{Revision})" : $"{arg} (rev{Revision})",
                        IconUrl = embed.Footer?.IconUrl,
                    };
                }
                else if (cmd == "footer_icon")
                {
                    embed.Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = embed.Footer?.Text,
                        IconUrl = arg,
                    };
                }
                else
                {
                    return false;
                }

                return true;
            }


            return false;
        }

        public string BuildDescription(string desc, DiscordEmbedBuilder embed)
        {
            var buffer = new StringBuilder();


            int offset = 0;

            while (offset < desc.Length)
            {
                int index = desc.IndexOf('!', offset);

                if (index < 0)
                {
                    buffer.Append(desc.Substring(offset));

                    break;
                }


                if (index == 0 || desc[index - 1] == '\n')
                {
                    int endIndex = desc.IndexOf('\n', index + 1);

                    string cmdline = string.Empty;

                    if (endIndex < 0)
                    {
                        cmdline = desc.Substring(index + 1);
                    }
                    else
                    {
                        cmdline = desc.Substring(index + 1, endIndex - index - 1);
                    }

                    if (BuildCommand(BuildJs(cmdline), embed))
                    {
                        if (index > offset)
                        {
                            buffer.Append(desc.Substring(offset, index - offset));
                        }

                        if (endIndex < 0)
                        {
                            break;
                        }
                        else
                        {
                            offset = endIndex + 1;
                        }
                    }
                    else
                    {
                        buffer.Append(desc.Substring(offset, index - offset + 1));

                        offset = index + 1;
                    }
                }
                else
                {
                    buffer.Append(desc.Substring(offset, index - offset + 1));

                    offset = index + 1;
                }
            }


            return buffer.ToString();
        }

        public string BuildJs(string content)
        {
            var buffer = new StringBuilder();


            int offset = 0;

            while (offset < content.Length)
            {
                int beginIndex = content.IndexOf('$', offset);

                if (beginIndex < 0)
                {
                    buffer.Append(content.Substring(offset, content.Length - offset));
                    break;
                }


                if (beginIndex > 0 && content[beginIndex - 1] == '\\')
                {
                    if (beginIndex > offset + 1)
                    {
                        buffer.Append(content.Substring(offset, beginIndex - offset - 1));
                    }

                    buffer.Append('$');

                    offset = beginIndex + 1;

                    continue;
                }


                int delimiterEnd = content.IndexOf('{', beginIndex + 1);

                if (delimiterEnd < beginIndex + 1)
                {
                    buffer.Append(content.Substring(offset, content.Length - offset));
                    break;
                }

                string delimiter = content.Substring(beginIndex + 1, delimiterEnd - beginIndex - 1);


                int endIndex = content.IndexOf("}" + delimiter, delimiterEnd + 1);

                if (endIndex < 0)
                {
                    buffer.Append(content.Substring(offset, delimiterEnd - offset + 1));

                    offset = delimiterEnd + 1;

                    continue;
                }


                string code = content.Substring(delimiterEnd + 1, endIndex - delimiterEnd - 1);

                string result = JsEngine.Evaluate(code, TimeSpan.FromSeconds(3));

                buffer.Append(content.Substring(offset, beginIndex - offset));
                buffer.Append(result);

                offset = endIndex + delimiter.Length + 1;
            }


            return buffer.ToString();
        }

        public DiscordEmbed Build()
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.SpringGreen)
                .WithTitle(Title)
                .WithTimestamp(LastModifiedTime)
                .WithFooter($"(rev{Revision})");


            string subTitle = string.Empty;
            var buffer = new StringBuilder();
            int offset = 0;

            while (offset <= Content.Length)
            {
                int index = -1;

                if (offset < Content.Length)
                {
                    index = Content.IndexOf('#', offset);
                }

                if (index <= 0 || Content[index - 1] == '\n')
                {
                    if (index < 0)
                    {
                        buffer.Append(Content.Substring(offset));
                    }
                    else
                    {
                        buffer.Append(Content.Substring(offset, index - offset));
                    }

                    if (string.IsNullOrEmpty(subTitle))
                    {
                        string desc = BuildDescription(buffer.ToString().Trim(), embed);
                        desc = BuildJs(desc);

                        if (string.IsNullOrWhiteSpace(desc))
                        {
                            desc = "\u200B";
                        }

                        embed.Description = desc;
                    }
                    else
                    {
                        string subContent = buffer.ToString().Trim();
                        subContent = BuildJs(subContent);

                        if (subContent.Length <= 0)
                        {
                            subContent = "\u200B";
                        }

                        embed.AddField(subTitle, subContent);
                    }

                    buffer.Clear();


                    if (index >= 0)
                    {
                        int endIndex = Content.IndexOf('\n', index + 1);

                        if (endIndex >= 0)
                        {
                            subTitle = Content.Substring(index + 1, endIndex - index - 1).Trim();
                            subTitle = BuildJs(subTitle);

                            if (subTitle.Length <= 0)
                            {
                                subTitle = "\u200B";
                            }

                            offset = endIndex + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    buffer.Append(Content.Substring(offset, index - offset + 1));

                    offset = index + 1;
                }
            }


            return embed.Build();
        }
    }
}
