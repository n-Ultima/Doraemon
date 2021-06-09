using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Doraemon.Common.Attributes;
using Discord.WebSocket;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Modules
{
    [Name("Roles")]
    [Summary("Utilities for users registering roles to themselves.")]
    [Group("role")]
    [Alias("r", "roles")]
    public class RoleModule : ModuleBase
    {
        public DoraemonContext _doraemonContext;
        public RoleModule(DoraemonContext doraemonContext)
        {
            _doraemonContext = doraemonContext;
        }
        [Command("register")]
        [Summary("Registers a user to a role.")]
        public async Task AddRoleAsync(
           [Summary("The role to be added.")]
                string role)
        {
            var RoleToBeAdded = await _doraemonContext
                .Set<Role>()
                .Where(x => x.Name == role)
                .SingleOrDefaultAsync();
            if(RoleToBeAdded is null)
            {
                throw new ArgumentException("The role provided was not found.");
            }
            var Role = Context.Guild.GetRole(RoleToBeAdded.Id);
            await (Context.User as IGuildUser).AddRoleAsync(Role);
            await ReplyAsync($"Successfully registered {Context.User.Mention} to **{Role.Name}**");
        }
        [Command(RunMode = RunMode.Async)]
        [Alias("list")]
        [Priority(10)]
        [Summary("Lists all roles available to a user.")]
        public async Task ListRolesAsync()
        {
            var builder = new StringBuilder();
            foreach(var role in _doraemonContext.Roles.AsQueryable().OrderBy(x => x.Name))
            {
                builder.Append($"{role.Name}, ");
            }
            var embed = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle("How do I get roles?")
                .WithColor(Color.Blue)
                .WithDescription("You get roles by using the `!role register <RoleName>` command. To remove a role, you simply use `!role unregister <RoleName>` command.\n**Roles available to you:\n**" + builder.ToString())
                .Build();
            await ReplyAsync(embed: embed);
        }
        [Command("create")]
        [RequireGuildOwner]
        [Alias("add")]
        [Summary("Adds a currently-existing role to the list of assignable roles.")]
        public async Task CreateRoleAsync(
            [Summary("The name of the role to be added.")]
                [Remainder]string roleName)
        {
            var futureRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if(futureRole is null)
            {
                throw new ArgumentException("The role provided was not found.");
            }
            _doraemonContext.Roles.Add(new Role
            {
                Id = futureRole.Id,
                Name = futureRole.Name,

            });
            await _doraemonContext.SaveChangesAsync();
            await Context.AddConfirmationAsync();
        }
        [Command("unregister")]
        [Alias("remove")]
        [Summary("Unregisters a user from a role.")]
        public async Task RemoveRoleAsync(
            [Summary("The name of the role to be unregistered from.")]
                string role)
        {
            var RoleToBeRemoved = await _doraemonContext
                .Set<Role>()
                .Where(x => x.Name == role)
                .SingleOrDefaultAsync();
            if(RoleToBeRemoved is null)
            {
                throw new ArgumentException("The role provided was not found.");
            }
            var Role = Context.Guild.GetRole(RoleToBeRemoved.Id);
            await (Context.User as IGuildUser).RemoveRoleAsync(Role);
            await ReplyAsync($"Successfully unregistered {Context.User.Mention} from **{Role.Name}**");
        }
        [Command("delete")]
        [RequireGuildOwner]
        [Alias("remove")]
        [Summary("Removes a role from the list of roles that users can assign themselves.")]
        public async Task DeleteRoleAsync(
            [Summary("The name of the role.")]
                string roleName)
        {
            var RoleToBeRemoved = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if(RoleToBeRemoved is null)
            {
                throw new ArgumentException("The role provided was not found.");
            }
            var rQ = await _doraemonContext
                .Set<Role>()
                .Where(x => x.Id == RoleToBeRemoved.Id)
                .SingleOrDefaultAsync();
            _doraemonContext.Roles.Remove(rQ);
            await _doraemonContext.SaveChangesAsync();
            await Context.AddConfirmationAsync();
        }
    }
}
