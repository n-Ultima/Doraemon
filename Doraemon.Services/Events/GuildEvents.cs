﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Core;
using Discord;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Doraemon.Common;
using Discord.Commands;
using Doraemon.Services.Events.MessageReceivedHandlers;
using Doraemon.Common.Utilities;
using Discord.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Humanizer;
using Doraemon.Data;
using Doraemon.Services.Moderation;
using Doraemon.Services.Core;

namespace Doraemon.Services.Events
{
    public class GuildEvents
    {
        public static DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public InfractionService _infractionService;
        public RoleClaimService _roleClaimService;
        public AutoModeration _autoModeration;
        public static List<DeletedMessage> DeletedMessages = new List<DeletedMessage>(); // Snipe Command setup.
        public GuildEvents(DoraemonContext doraemonContext, DiscordSocketClient client, InfractionService infractionService, RoleClaimService roleClaimService, AutoModeration autoModeration)
        {
            _doraemonContext = doraemonContext;
            _client = client;
            _infractionService = infractionService;
            _roleClaimService = roleClaimService;
            _autoModeration = autoModeration;
        }
        public async Task MessageEdited(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            await _autoModeration.CheckForBlacklistedAttachmentTypesAsync(arg2);
            await _autoModeration.CheckForDiscordInviteLinksAsync(arg2);
            await _autoModeration.CheckForSpamAsync(arg2);
            await _autoModeration.CheckForRestrictedWordsAsync(arg2);
            if (arg2.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }
            var modmail = await _doraemonContext.ModmailTickets
                .Where(x => x.ModmailChannel == arg3.Id)
                .SingleOrDefaultAsync();
            if(modmail is not null)
            {
                return;
            }
            if (arg2.Author.IsBot) return;
            if (!(arg2 is SocketUserMessage message)) return;
            var context = new SocketCommandContext(_client, message);
            
            if (DoraemonConfig.LogConfiguration.MessageLogChannelId == default)
            {
                return;
            }
            if (message.Author.IsWebhook) return;
            var e = new EmbedBuilder()
                .WithAuthor($"{message.Author}(`{message.Author.Id}`)", message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl())
                .WithColor(Color.Gold)
                .WithDescription($"Message edited in <#{message.Channel.Id}>\n**Before:** {arg1.Value.Content}\n**After:** {arg2.Content}")
                .WithTimestamp(DateTimeOffset.Now);
            var messageLog = context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.MessageLogChannelId);
            await messageLog.SendMessageAsync(embed: e.Build());
            return;
        }
        public async Task MessageDeleted(Cacheable<IMessage, ulong> cachedMessage, ISocketMessageChannel channel)
        {
            if (channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }
            if (!cachedMessage.HasValue) return;
            if (cachedMessage.Value.Author == _client as IUser) return;
            var message = cachedMessage.Value;
            if (message.Author.IsWebhook) return;
            if (!(message is SocketUserMessage msg)) return;
            var listedMessage = DeletedMessages
                .Find(x => x.ChannelId == channel.Id);
            if (listedMessage == null) //check if the channel has a deleted message listed
            {
                DeletedMessages.Add(new DeletedMessage { Content = message.Content, UserId = message.Author.Id, ChannelId = channel.Id, Time = message.Timestamp.LocalDateTime, DeleteTime = DateTime.Now + TimeSpan.FromMinutes(30) }); //when the method checks the list again, it will delete messages older than 30 minutes
            }
            else
            {
                DeletedMessages.Remove(listedMessage);
                DeletedMessages.Add(new DeletedMessage { Content = message.Content, UserId = message.Author.Id, ChannelId = channel.Id, Time = message.Timestamp.LocalDateTime, DeleteTime = DateTime.Now + TimeSpan.FromMinutes(30) });
            }
            List<DeletedMessage> Remove = new List<DeletedMessage>(); //filter out the list below
            foreach (var deletedMessage in DeletedMessages)
            {
                if (DateTime.Now > deletedMessage.DeleteTime)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
                SocketGuild messageGuild = null;
                var guilds = _client.Guilds.ToList();
                foreach (var guild in guilds)
                {
                    var currentChannel = guild.TextChannels.ToList()
                        .Find(x => x.Id == deletedMessage.ChannelId);
                    if (currentChannel != null)
                    {
                        messageGuild = _client.GetGuild(currentChannel.Guild.Id);
                    }
                }
                if (messageGuild == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
                var messageContent = deletedMessage.Content;
                if (messageContent == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
                var messageChannel = messageGuild.GetTextChannel(deletedMessage.ChannelId);
                if (messageChannel == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
                var messageUser = messageGuild.GetUser(deletedMessage.UserId);
                if (messageUser == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
            }
            DeletedMessages = DeletedMessages.Except(Remove).ToList();
            string x;
            if (string.IsNullOrEmpty(message.Content))
            {
                return;
            }
            if (message.Embeds.Count != 0)
            {
                return;
            }
            x = message.Content;
            var embed = new EmbedBuilder()
                .WithAuthor($"{message.Author.Username}(`{message.Author.Id}`)", message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl())
                .WithDescription($"Message deleted in <#{message.Channel.Id}>\n**Content:** {x}")
                .WithColor(Color.Red)
                .WithTimestamp(DateTimeOffset.Now);
            var context = new SocketCommandContext(_client, msg);
            var messageLog = context.Guild.GetTextChannel(DoraemonConfig.LogConfiguration.MessageLogChannelId);
            await messageLog.SendMessageAsync(embed: embed.Build());
        }
        // We call this as soon as the client is ready
        public async Task ClientReady()
        {
            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            // Download all the users
            await guild.DownloadUsersAsync();
            var mutedRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            if (mutedRole is null)
            {
                await SetupMuteRoleAsync(guild.Id);
                Log.Logger.Information($"Mute role setup successfully in {guild.Name}");
            }
            var claims = await _doraemonContext.ClaimMaps.AsQueryable().ToListAsync();
            var adminRoles = guild.Roles.Where(x => x.Permissions.Administrator);
            var highestRole = guild.Roles.OrderByDescending(x => x.Position);
            if (!claims.Any())
            {
                // Give any role with Administrator the "AuthorizationManage" claim.
                // Also give the highest role on the role list the AuthorizationManage claim.
                await _roleClaimService.AutoConfigureGuildAsync(adminRoles);
                await _roleClaimService.AddRoleClaimAsync(highestRole.First().Id, _client.CurrentUser.Id, ClaimMapType.AuthorizationManage);
                Log.Logger.Information($"Gave the roles: {adminRoles.Humanize()}, and also {highestRole.First().Name}, the \"AuthorizationManage\" claim.");

            }

            Log.Logger.Information($"The client is ready, and ready to respond to events.");
        }
        public async Task SetupMuteRoleAsync(ulong guild)
        {
            var setupGuild = _client.GetGuild(guild);
            var muteRole = await setupGuild.CreateRoleAsync(muteRoleName, null, null, false, options: new RequestOptions()
            {
                AuditLogReason = "Created MuteRole."
            });
            foreach (var categoryChannel in setupGuild.CategoryChannels)
            {
                if (!categoryChannel.GetPermissionOverwrite(muteRole).HasValue || categoryChannel.GetPermissionOverwrite(muteRole).Value.SendMessages == PermValue.Allow || categoryChannel.GetPermissionOverwrite(muteRole).Value.SendMessages == PermValue.Inherit)
                {
                    await categoryChannel.AddPermissionOverwriteAsync(muteRole, permissions: new OverwritePermissions(sendMessages: PermValue.Deny), options: new RequestOptions()
                    {
                        AuditLogReason = "Muterole can no longer embed links."
                    });
                }
                if (!categoryChannel.GetPermissionOverwrite(muteRole).HasValue || categoryChannel.GetPermissionOverwrite(muteRole).Value.AddReactions == PermValue.Allow || categoryChannel.GetPermissionOverwrite(muteRole).Value.AddReactions == PermValue.Inherit)
                {
                    await categoryChannel.AddPermissionOverwriteAsync(muteRole, permissions: new OverwritePermissions(addReactions: PermValue.Deny), options: new RequestOptions()
                    {
                        AuditLogReason = "Muterole can no longer add reactions."
                    });
                }
                if(!categoryChannel.GetPermissionOverwrite(muteRole).HasValue || categoryChannel.GetPermissionOverwrite(muteRole).Value.EmbedLinks == PermValue.Allow || categoryChannel.GetPermissionOverwrite(muteRole).Value.EmbedLinks == PermValue.Inherit)
                {
                    await categoryChannel.AddPermissionOverwriteAsync(muteRole, permissions: new OverwritePermissions(embedLinks: PermValue.Deny), options: new RequestOptions()
                    {
                        AuditLogReason = "Muterole can no longer embed links."
                    });
                }
            }
            Log.Logger.Information($"Successfully setup the muterole for guild: {setupGuild.Name}");

        }
    }
}
