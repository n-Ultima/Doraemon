using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Doraemon.Modules
{
    [Name("Announcements")]
    [Summary("Provides utilities for making announcements to a guild.")]
    public class AnnouncementModule : ModuleBase<SocketCommandContext>
    {
        [Command("announce")]
        [Summary("Make an announcement")]
        public async Task ShowAnnouncementAsync(
            [Summary("The role to mention")]
                IRole role,
            [Summary("The content to be displayed")]
                [Remainder] string content)
            
        {
            var channel = Context.Channel;
            await channel.SendMessageAsync($"{role.Mention}\n{content}");
        }
    }
}
