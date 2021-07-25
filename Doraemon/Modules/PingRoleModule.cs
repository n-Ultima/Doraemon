using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Common.Attributes;
using Doraemon.Common.Extensions;
using Doraemon.Services.Core;

namespace Doraemon.Modules
{
    [Name("PingRoles")]
    [Summary("Utilities for users registering roles to themselves.")]
    [Group("pingrole")]
    [Alias("pr", "pingroles")]
    public class PingRoleModule : ModuleBase
    {
        private readonly PingRoleService _pingRoleService;

        public PingRoleModule(PingRoleService pingRoleService)
        {
            _pingRoleService = pingRoleService;
        }

        [Command("register")]
        [Summary("Registers a user to a ping role.")]
        public async Task AddRoleAsync(
            [Summary("The role to be added.")] [Remainder]
                string roleName)
        {
            var roleToBeAdded = await _pingRoleService.FetchPingRoleAsync(roleName);
            if (roleToBeAdded is null) throw new ArgumentNullException("The role provided was not found.");
            var role = Context.Guild.GetRole(roleToBeAdded.Id);
            await (Context.User as IGuildUser).AddRoleAsync(role);
            await ReplyAsync($"Successfully registered {Context.User.Mention} to **{role.Name}**");
        }

        [Command(RunMode = RunMode.Async)]
        [Alias("list")]
        [Priority(10)]
        [Summary("Lists all roles available to a user.")]
        public async Task ListRolesAsync()
        {
            var builder = new StringBuilder();
            foreach (var role in await _pingRoleService.FetchAllPingRolesAsync()) builder.Append($"{role.Name}, ");
            var embed = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("How do I get roles?")
                .WithColor(Color.Blue)
                .WithDescription(
                    "You get roles by using the `!pingrole register <RoleName>` command. To remove a role, you simply use `!pingrole unregister <RoleName>` command.\n**PingRoles available to you:\n**" +
                    builder)
                .Build();
            await ReplyAsync(embed: embed);
        }

        [Command("create")]
        [RequireGuildOwner]
        [Alias("add")]
        [Summary("Adds a currently-existing role to the list of assignable roles.")]
        public async Task CreatePingRoleAsync(
            [Summary("The name of the role to be added.")] [Remainder]
                string roleName)
        {
            var futureRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if (futureRole is null) throw new ArgumentException("The role provided was not found in the guild.");
            await _pingRoleService.AddPingRoleAsync(futureRole.Id, roleName);
            await Context.AddConfirmationAsync();
        }

        [Command("unregister")]
        [Alias("remove")]
        [Summary("Unregisters a user from a role.")]
        public async Task RemoveRoleAsync(
            [Summary("The name of the role to be unregistered from.")] [Remainder]
                string roleName)
        {
            var roleToBeRemoved = await _pingRoleService.FetchPingRoleAsync(roleName);
            if (roleToBeRemoved is null) throw new ArgumentNullException("The role provided was not found.");
            var role = Context.Guild.GetRole(roleToBeRemoved.Id);
            await (Context.User as IGuildUser).RemoveRoleAsync(role);
            await ReplyAsync($"Successfully unregistered {Context.User.Mention} from **{role.Name}**");
        }

        [Command("delete")]
        [Alias("remove")]
        [Summary("Removes a role from the list of roles that users can assign themselves.")]
        public async Task DeleteRoleAsync(
            [Summary("The name of the role.")] 
                string roleName)
        {
            var roleToBeRemoved = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if (roleToBeRemoved is null) throw new ArgumentNullException("The role provided was not found.");
            var pingRole = await _pingRoleService.FetchPingRoleAsync(roleName);

            if (pingRole is null) throw new InvalidOperationException("The role provided is not a pingrole.");
            await _pingRoleService.RemovePingRoleAsync(pingRole.Id);
            await Context.AddConfirmationAsync();
        }
    }
}