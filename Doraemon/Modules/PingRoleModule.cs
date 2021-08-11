using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Rest;
using Doraemon.Common.Extensions;
using Doraemon.Data;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Qmmands;

namespace Doraemon.Modules
{
    [Name("PingRoles")]
    [Description("Utilities for users registering roles to themselves.")]
    [Group("pingrole", "pr", "pingroles")]
    public class PingRoleModule : DiscordGuildModuleBase
    {
        private readonly PingRoleService _pingRoleService;

        public PingRoleModule(PingRoleService pingRoleService)
        {
            _pingRoleService = pingRoleService;
        }

        [Command("register")]
        [Description("Registers a user to a ping role.")]
        public async Task AddRoleAsync(
            [Description("The role to be added.")] [Remainder]
                string roleName)
        {
            var roleToBeAdded = await _pingRoleService.FetchPingRoleAsync(roleName);
            if (roleToBeAdded is null) throw new ArgumentNullException("The role provided was not found.");
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Value.Id == roleToBeAdded.Id).Value;
            await Context.Author.GrantRoleAsync(role.Id);
            await Context.Channel.SendMessageAsync(new LocalMessage().WithContent($"Successfully registered {Context.Author.Mention} to **{role.Name}**"));
        }

        [Command("", "list")]
        [Priority(10)]
        [Description("Lists all roles available to a user.")]
        public async Task ListRolesAsync()
        {
            var builder = new StringBuilder();
            foreach (var role in await _pingRoleService.FetchAllPingRolesAsync()) builder.Append($"{role.Name}, ");
            var embed = new LocalEmbed()
                .WithAuthor(Context.Guild.Name, Context.Guild.GetIconUrl())
                .WithTitle("How do I get roles?")
                .WithColor(DColor.Blue)
                .WithDescription(
                    "You get roles by using the `!pingrole register <RoleName>` command. To remove a role, you simply use `!pingrole unregister <RoleName>` command.\n**PingRoles available to you:\n**" +
                    builder);
            await Context.Channel.SendMessageAsync(new LocalMessage()
                .WithEmbeds(embed));
        }

        [Command("create", "add")]
        [RequireClaims(ClaimMapType.GuildManage)]
        [Description("Adds a currently-existing role to the list of assignable roles.")]
        public async Task CreatePingRoleAsync(
            [Description("The name of the role to be added.")] [Remainder]
                string roleName)
        {
            var futureRole = Context.Guild.Roles.FirstOrDefault(x => x.Value.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)).Value;
            if (futureRole is null) throw new ArgumentException("The role provided was not found in the guild.");
            await _pingRoleService.AddPingRoleAsync(futureRole.Id, roleName);
            await Context.AddConfirmationAsync();
        }

        [Command("unregister", "remove")]
        [Description("Unregisters a user from a role.")]
        public async Task RemoveRoleAsync(
            [Description("The name of the role to be unregistered from.")] [Remainder]
                string roleName)
        {
            var roleToBeRemoved = await _pingRoleService.FetchPingRoleAsync(roleName);
            if (roleToBeRemoved is null) throw new ArgumentException("The role provided was not found.");
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Value.Id == roleToBeRemoved.Id).Value;
            await Context.Author.RevokeRoleAsync(role.Id);
            await Context.Channel.SendMessageAsync(new LocalMessage().WithContent($"Successfully unregistered {Context.Author.Mention} from **{role.Name}**"));
        }

        [Command("delete")]
        [RequireClaims(ClaimMapType.GuildManage)]
        [Description("Removes a role from the list of roles that users can assign themselves.")]
        public async Task DeleteRoleAsync(
            [Description("The name of the role.")] 
                string roleName)
        {
            var roleToBeRemoved = Context.Guild.Roles.FirstOrDefault(x => x.Value.Name == roleName).Value;
            if (roleToBeRemoved is null) throw new ArgumentException("The role provided was not found.");
            var pingRole = await _pingRoleService.FetchPingRoleAsync(roleName);

            if (pingRole is null) throw new InvalidOperationException("The role provided is not a pingrole.");
            await _pingRoleService.RemovePingRoleAsync(pingRole.Id);
            await Context.AddConfirmationAsync();
        }
    }
}