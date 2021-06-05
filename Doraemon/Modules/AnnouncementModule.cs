using Discord;
using Discord.Commands;
using System.Threading.Tasks;

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
            [Summary("The Content to be displayed")]
                [Remainder] string content)

        {
            var channel = Context.Channel;
            await channel.SendMessageAsync($"{role.Mention}\n{content}");
        }
    }
}
