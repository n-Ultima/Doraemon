using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models.Core;
using Discord;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Doraemon.Data.Services;
using Doraemon.Common;
using Discord.Commands;
using Doraemon.Data.Events.MessageReceivedHandlers;
using Doraemon.Common.Utilities;
using Discord.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Data.Events
{
    public class GuildEvents
    {
        public static DoraemonConfiguration Configuration { get; private set; } = new();
        public const string muteRoleName = "Doraemon_Moderation_Mute";
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public InfractionService _infractionService;
        public static List<DeletedMessage> DeletedMessages = new List<DeletedMessage>(); // Snipe Command setup.
        public GuildEvents(DoraemonContext doraemonContext, DiscordSocketClient client, InfractionService infractionService)
        {
            _doraemonContext = doraemonContext;
            _client = client;
            _infractionService = infractionService;
        }
        public async Task MessageEdited(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            if (arg2.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }
            if (!(arg2 is SocketUserMessage message)) return;
            var context = new SocketCommandContext(_client, message);
            string[] badWord = AutoModeration.RestrictedWords();
            var caseId = await DatabaseUtilities.ProduceIdAsync();
            ulong autoModId = _client.CurrentUser.Id;
            foreach (string word in AutoModeration.RestrictedWords())
            {
                // If the message contains the word, it will perform the actions. However, by writing it like this, we prevent some cases like the word "ass" being detected in "class".
                if (message.Content.ToLower().Split(" ").Intersect(badWord).Any())
                {
                    if (!context.User.IsStaff())
                    {
                        // Deletes the message and warns the user.
                        await message.DeleteAsync();
                        await context.Channel.SendMessageAsync($"<a:animeBonk:829808377357140048>{message.Author.Mention}, You aren't allowed to use offensive language here. Continuing to do this will result in a mute.");
                        var muteRole = context.Guild.GetRole(811797534631788574);
                        var infraction = ((IQueryable<Infraction>)_doraemonContext.Infractions)
                            .Where(x => x.SubjectId == message.Author.Id)
                            .ToList();
                        await _infractionService.CreateInfractionAsync(message.Author.Id, _client.CurrentUser.Id, context.Guild.Id, InfractionType.Warn, "Sending messages that contain prohibited words.", null);
                    }
                    return;
                }
            }
            if (Regex.Match(message.Content, @"(https?://)?(www.)?(discord.(gg|io|me|li)|discordapp.com/invite)/.+[a-z]").Success)
            {
                // Before deletion, we check if the user is a moderator.
                var lowestRoleToSendLinks = context.Guild.Roles.FirstOrDefault(x => x.Name == "Well Known");// Put the lowest role allowed to send links here. For me, I put my general "Staff" role.
                if ((context.Message.Author as SocketGuildUser).Hierarchy > lowestRoleToSendLinks.Position)
                {
                    Console.WriteLine("Nothin to see here.");
                }
                // If the author's hierarchy is lower than the role provided's position, then we delete the message, and warn the user.
                else
                {
                    await message.DeleteAsync();
                    await context.Channel.SendMessageAsync($"<a:animeBonk:829808377357140048> {context.Message.Author.Mention}, you aren't allowed to send links here. Continuing to do this will result in a mute.");
                }
            }
            if (Configuration.LogConfiguration.MessageLogChannelId == default)
            {
                return;
            }
            if (message.Author.IsWebhook) return;
            var e = new EmbedBuilder()
                .WithAuthor($"{message.Author}(`{message.Author.Id}`)", message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl())
                .WithColor(Color.Gold)
                .WithDescription($"Message edited in <#{message.Channel.Id}>\n**Before:** {arg1.Value.Content}\n**After:** {arg2.Content}")
                .WithTimestamp(DateTimeOffset.Now);
            var messageLog = context.Guild.GetTextChannel(Configuration.LogConfiguration.MessageLogChannelId);
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
            var messageLog = context.Guild.GetTextChannel(Configuration.LogConfiguration.MessageLogChannelId);
            await messageLog.SendMessageAsync(embed: embed.Build());
        }
        // We call this as soon as the client is ready
        public async Task ClientReady()
        {
            var guild = _client.GetGuild(Configuration.MainGuildId);
            // Download all the users
            await guild.DownloadUsersAsync();
            var mutedRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
            var currentMutes = await _doraemonContext
                .Set<Infraction>()
                .Where(x => x.Type == InfractionType.Mute)
                .Where(x => x.Duration != null)
                .ToListAsync();
            lock (CommandHandler.Mutes)
            {
                foreach (var mute in currentMutes)
                {
                    CommandHandler.Mutes.Add(new Mute { End = (DateTime)(DateTime.Now + mute.Duration), Guild = guild, Role = mutedRole, User = guild.GetUser(mute.SubjectId) });
                }
            }
            if (mutedRole is null)
            {
                await SetupMuteRoleAsync(guild.Id);
            }
        }
        public async Task SetupMuteRoleAsync(ulong guild)
        {
            var setupGuild = _client.GetGuild(guild);
            var muteRole = await setupGuild.CreateRoleAsync(muteRoleName, null, null, false, options: new RequestOptions()
            {
                AuditLogReason = "Created MuteRole."
            });
            foreach (var textChannel in setupGuild.CategoryChannels)
            {
                if (!textChannel.GetPermissionOverwrite(muteRole).HasValue || textChannel.GetPermissionOverwrite(muteRole).Value.SendMessages == PermValue.Allow || textChannel.GetPermissionOverwrite(muteRole).Value.SendMessages == PermValue.Inherit)
                {
                    await textChannel.AddPermissionOverwriteAsync(muteRole, permissions: new OverwritePermissions(sendMessages: PermValue.Deny), options: new RequestOptions()
                    {
                        AuditLogReason = "Setup MuteRole for all text channels.."
                    });
                }
                if (!textChannel.GetPermissionOverwrite(muteRole).HasValue || textChannel.GetPermissionOverwrite(muteRole).Value.AddReactions == PermValue.Allow || textChannel.GetPermissionOverwrite(muteRole).Value.AddReactions == PermValue.Inherit)
                {
                    await textChannel.AddPermissionOverwriteAsync(muteRole, permissions: new OverwritePermissions(addReactions: PermValue.Deny), options: new RequestOptions()
                    {
                        AuditLogReason = "Muterole can no longer add reactions."
                    });
                }
            }

        }
    }
}
