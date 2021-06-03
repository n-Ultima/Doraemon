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
    [Summary("Utilities for users registering pingroles to themselves.")]
    [Group("pingrole")]
    [Alias("pr", "pingroles")]
    public class RoleModule : ModuleBase
    {
        public DoraemonContext _doraemonContext;
        public RoleModule(DoraemonContext doraemonContext)
        {
            _doraemonContext = doraemonContext;
        }
        [Command("register")]
        [Summary("Registers a user to a pingrole.")]
        public async Task AddPingRoleAsync(
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
        [Summary("Lists all pingroles available to a user.")]
        public async Task ListPingrolesAsync()
        {
            var builder = new StringBuilder();
            foreach(var role in _doraemonContext.Roles.AsQueryable().OrderBy(x => x.Name))
            {
                builder.AppendLine($"<@{role.Id}> ~ {role.Description}");
            }
            var embed = new EmbedBuilder()
                .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
                .WithTitle($"Pingroles ({_doraemonContext.Roles.Count()})")
                .WithDescription(builder.ToString())
                .WithFooter("Use \"!pingrole register <RoleName>\" to register to a pingrole.")
                .Build();
            await ReplyAsync(embed: embed);
        }
        [Command("create")]
        [RequireGuildOwner]
        [Alias("add")]
        [Summary("Adds a currently-existing role to the list of pingroles.")]
        public async Task CreatePingroleAsync(
            [Summary("The name of the role to be added.")]
                string roleName,
            [Summary("The description of the role to be displayed.")]
                string roleDescription)
        {
            var futurePingRole = Context.Guild.Roles.FirstOrDefault(x => x.Name == roleName);
            if(futurePingRole is null)
            {
                throw new ArgumentException("The role provided was not found.");
            }
            _doraemonContext.Roles.Add(new Role
            {
                Id = futurePingRole.Id,
                Description = roleDescription,
                Name = futurePingRole.Name,

            });
            await _doraemonContext.SaveChangesAsync();
            await Context.AddConfirmationAsync();
        }
        [Command("unregister")]
        [Summary("Unregisters a user from a pingrole.")]
        public async Task RemovePingRoleAsync(
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
        [Summary("Removes a role from the list of pingroles.")]
        public async Task DeletePingroleAsync(
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
