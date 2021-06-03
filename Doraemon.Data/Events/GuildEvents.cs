using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Doraemon.Data.Models.Core;
using Discord;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Doraemon.Data.Services;
using Discord.Commands;
using Doraemon.Data.Events.MessageReceivedHandlers;
using Doraemon.Common.Utilities;
using Discord.Net;
using System.Text.RegularExpressions;

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
                            .Where(x => x.subjectId == message.Author.Id)
                            .ToList();
                        if (!infraction.Any())
                        {
                            _doraemonContext.Infractions.Add(new Infraction { Id = caseId, moderatorId = autoModId, Reason = "Using offensive language and/or using prohibited words.", subjectId = message.Author.Id });
                            await _doraemonContext.SaveChangesAsync();
                        }
                        else
                        {
                            _doraemonContext.Infractions.Add(new Infraction { Id = caseId, moderatorId = autoModId, Reason = "Using offensive language and/or using prohibited words.", subjectId = message.Author.Id });
                            await _doraemonContext.SaveChangesAsync();
                            if (infraction.Count >= 3)
                            {
                                await _infractionService.CheckForMultipleInfractionsAsync(message.Author.Id, context.Guild.Id);
                                await message.Author.GetOrCreateDMChannelAsync();
                                var embed = new EmbedBuilder()
                                      .WithTitle("You were muted")
                                      .WithDescription($"You were muted in **{context.Guild.Name}**\nYou were muted for reason: User has incurred 3 or more infractions.\nMute Timer- 6h")
                                      .WithColor(Discord.Color.Red);
                                try
                                {
                                    await message.Author.SendMessageAsync(embed: embed.Build());
                                }
                                catch (HttpException)
                                {
                                    Console.WriteLine("Unable to DM user.");
                                }
                                await _doraemonContext.SaveChangesAsync();
                            }
                        }
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
            if (Configuration.MessageLogChannelId == default)
            {
                return;
            }
            if (message.Author.IsWebhook) return;
            var e = new EmbedBuilder()
                .WithAuthor($"{message.Author}(`{message.Author.Id}`)", message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl())
                .WithColor(Color.Gold)
                .WithDescription($"Message edited in <#{message.Channel.Id}>\n**Before:** {arg1.Value.Content}\n**After:** {arg2.Content}")
                .WithTimestamp(DateTimeOffset.Now);
            var messageLog = context.Guild.GetTextChannel(Configuration.MessageLogChannelId);
            await messageLog.SendMessageAsync(embed: e.Build());
            return;
        }
        public async Task MessageDeleted(Cacheable<IMessage, ulong> cachedMessage, ISocketMessageChannel channel)
        {
            if (!cachedMessage.HasValue) return;
            if (cachedMessage.Value.Author == _client as IUser) return;
            var message = cachedMessage.Value;
            if (message.Author.IsWebhook) return;
            if (!(message is SocketUserMessage msg)) return;
            var listedMessage = DeletedMessages
                .Find(x => x.channelid == channel.Id);
            if (listedMessage == null) //check if the channel has a deleted message listed
            {
                DeletedMessages.Add(new DeletedMessage { content = message.Content, userid = message.Author.Id, channelid = channel.Id, time = message.Timestamp.LocalDateTime, deleteTime = DateTime.Now + TimeSpan.FromMinutes(30) }); //when the method checks the list again, it will delete messages older than 30 minutes
            }
            else
            {
                DeletedMessages.Remove(listedMessage);
                DeletedMessages.Add(new DeletedMessage { content = message.Content, userid = message.Author.Id, channelid = channel.Id, time = message.Timestamp.LocalDateTime, deleteTime = DateTime.Now + TimeSpan.FromMinutes(30) });
            }
            List<DeletedMessage> Remove = new List<DeletedMessage>(); //filter out the list below
            foreach (var deletedMessage in DeletedMessages)
            {
                if (DateTime.Now > deletedMessage.deleteTime)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
                SocketGuild messageGuild = null;
                var guilds = _client.Guilds.ToList();
                foreach (var guild in guilds)
                {
                    var currentChannel = guild.TextChannels.ToList()
                        .Find(x => x.Id == deletedMessage.channelid);
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
                var messageContent = deletedMessage.content;
                if (messageContent == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
                var messageChannel = messageGuild.GetTextChannel(deletedMessage.channelid);
                if (messageChannel == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
                var messageUser = messageGuild.GetUser(deletedMessage.userid);
                if (messageUser == null)
                {
                    Remove.Add(deletedMessage);
                    continue;
                }
            }
            DeletedMessages = DeletedMessages.Except(Remove).ToList();
            if (Configuration.MessageLogChannelId == default)
            {
                return;
            }
            string x;
            if (string.IsNullOrEmpty(message.Content))
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
            var messageLog = context.Guild.GetTextChannel(Configuration.MessageLogChannelId);
            await messageLog.SendMessageAsync(embed: embed.Build());
        }
        // We call this as soon as the client is ready
        public async Task ClientReady()
        {
            // Get the only guild that the bot is in: Moment's Reprieve
            var guild = _client.GetGuild(Configuration.MainGuildId); // Replace the ID with your Guild.
            // Download all the users
            await guild.DownloadUsersAsync();
            var mutedRole = guild.Roles.FirstOrDefault(x => x.Name == muteRoleName);
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
            }
        }
    }
}
