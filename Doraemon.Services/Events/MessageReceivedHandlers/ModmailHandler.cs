using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Doraemon.Data.Models;
using Doraemon.Data;
using Doraemon.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Doraemon.Common;
using Doraemon.Common.Utilities;
using Serilog;

namespace Doraemon.Services.Events.MessageReceivedHandlers
{
    public class ModmailHandler
    {
        public DoraemonContext _doraemonContext;
        public DiscordSocketClient _client;
        public static StringBuilder stringBuilder = new StringBuilder();
        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ModmailHandler(DiscordSocketClient client, DoraemonContext doraemonContext)
        {
            _doraemonContext = doraemonContext;
            _client = client;
        }
        /// <summary>
        /// Message received handler used to handle modmail threads.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task ModmailAsync(SocketMessage arg)
        {
            if (arg.Author.IsBot || arg.Author.IsWebhook) return; // Make sure bot's & webhook's messages aren't being used.
            var User = await _doraemonContext.GuildUsers
                .Where(x => x.Id == arg.Author.Id)
                .SingleOrDefaultAsync();
            if(User is null)
            {
                _doraemonContext.GuildUsers.Add(new Data.Models.Core.GuildUser
                {
                    Id = arg.Author.Id,
                    Username = arg.Author.Username,
                    Discriminator = arg.Author.Discriminator,
                    IsModmailBlocked = false
                });
                await _doraemonContext.SaveChangesAsync();
            }
            // BOO for having to re-query
            var modmailStart = await _doraemonContext.GuildUsers
                .Where(x => x.Id == arg.Author.Id)
                .SingleOrDefaultAsync();
            if (modmailStart.IsModmailBlocked)
            {
                await arg.Channel.SendMessageAsync($"You are not permitted to open modmail threads at this time.");
                return;
            }
            if ((arg.Channel.GetType()) == typeof(SocketDMChannel)) // This is if a message was received from a DM channel, not a modmail thread channel inside of a guild.
            {
                var dmModmail = await _doraemonContext
                    .Set<ModmailTicket>()
                    .Where(x => x.DmChannel == arg.Channel.Id)
                    .Where(x => x.UserId == arg.Author.Id)
                    .SingleOrDefaultAsync(); // Check if a currently existsing modmail thread exists with the user and dm channel.
                var modMailGuild = _client.GetGuild(DoraemonConfig.MainGuildId); // Get the guild defined in config.json
                var modmailLogChannel = modMailGuild.GetTextChannel(DoraemonConfig.LogConfiguration.ModmailLogChannelId);
                var modMailCategory = modMailGuild.GetCategoryChannel(DoraemonConfig.ModmailCategoryId); // Get the modmail category ID defined in config.json
                if (dmModmail is null) // If the check is null, then we go ahead and create a new thread.
                {
                    var ID = DatabaseUtilities.ProduceId();
                    await arg.Channel.SendMessageAsync("Thank you for contacting Modmail! Staff will reply as soon as possible."); // Reply to the DM channel, so that the modmail starter knows that Staff will be with them soon.
                    var textChannel = await modMailGuild.CreateTextChannelAsync(arg.Author.GetFullUsername(), x => x.CategoryId = modMailCategory.Id); // Make a text channel with the users username inside of the modmail category.
                    await textChannel.ModifyAsync(x => x.Topic = $"User ID: {arg.Author.Id}");
                    if (arg.Attachments.Any())
                    {
                        var image = arg.Attachments.ElementAt(0);
                        var firstMessageEmbed = new EmbedBuilder()
                            .WithAuthor(arg.Author.GetFullUsername(), arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                            .WithColor(Color.Gold)
                            .WithDescription(arg.Content)
                            .WithCurrentTimestamp()
                            .WithImageUrl(image.Url)
                            .WithFooter($"Message ID: {arg.Id}")
                            .WithFooter($"Ticket ID: {ID}")
                            .Build(); // This will only be sent once per thread, so we have access to the Ticket ID.
                        await textChannel.SendMessageAsync(embed: firstMessageEmbed);
                        _doraemonContext.ModmailTickets.Add(new ModmailTicket { Id = ID, DmChannel = arg.Channel.Id, ModmailChannel = textChannel.Id, UserId = arg.Author.Id }); // Create the Modmail thread.
                        await _doraemonContext.SaveChangesAsync();
                        await arg.AddConfirmationAsync(); // Add a checkmark to the user's message, just to again show that everything went smoothly.
                        stringBuilder.AppendLine($"User \"{arg.Author.GetFullUsername()}\" opened a modmail ticket with message: {arg.Content}");
                        stringBuilder.AppendLine($"With Image: {image.Url}");
                        stringBuilder.AppendLine($"Ticket ID: {ID}");
                        stringBuilder.AppendLine();
                        return;
                    }
                    var firstMessageEmbedNoImage = new EmbedBuilder()
                        .WithAuthor(arg.Author.GetFullUsername(), arg.Author.GetDefiniteAvatarUrl())
                        .WithColor(Color.Gold)
                        .WithDescription(arg.Content)
                        .WithCurrentTimestamp()
                        .WithFooter($"Message ID: {arg.Id}")
                        .WithFooter($"Ticket ID: `{ID}`")
                        .Build();
                    await textChannel.SendMessageAsync(embed: firstMessageEmbedNoImage);
                    _doraemonContext.ModmailTickets.Add(new ModmailTicket { Id = ID, DmChannel = arg.Channel.Id, ModmailChannel = textChannel.Id, UserId = arg.Author.Id }); // Create the Modmail thread.
                    await _doraemonContext.SaveChangesAsync();
                    
                    await arg.AddConfirmationAsync(); // Add a checkmark to the user's message, just to again show that everything went smoothly.
                    stringBuilder.AppendLine($"User {arg.Author.GetFullUsername()} opened a modmail ticket with message: {arg.Content}");
                    stringBuilder.AppendLine($"Ticket ID: {ID}");
                    stringBuilder.AppendLine();
                    return;
                }
                // This gets fired if the message came from a DM Channel, but it's an already active thread.
                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
                var channelToSend = guild.GetTextChannel(dmModmail.ModmailChannel);
                if(channelToSend is null)
                {
                    return;
                }
                if (arg.Attachments.Any())
                {
                    var image = arg.Attachments.ElementAt(0);
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg.Author.GetFullUsername(), arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                        .WithColor(Color.Gold)
                        .WithDescription(arg.Content)
                        .WithCurrentTimestamp()
                        .WithImageUrl(image.Url)
                        .WithFooter($"Message ID: {arg.Id}")
                        .Build();
                    await channelToSend.SendMessageAsync(embed: embed);
                    await arg.AddConfirmationAsync();
                    stringBuilder.AppendLine($"{arg.Author.GetFullUsername()} - {arg.Content}");
                    stringBuilder.AppendLine($"{image.Url}");
                    stringBuilder.AppendLine();
                    return;
                }
                var embedWithNoAttachments = new EmbedBuilder()
                        .WithAuthor(arg.Author.GetFullUsername(), arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                        .WithColor(Color.Gold)
                        .WithCurrentTimestamp()
                        .WithDescription(arg.Content)
                        .WithFooter($"Message ID: {arg.Id}")
                        .Build();
                stringBuilder.AppendLine($"{arg.Author.GetFullUsername()} - {arg.Content}");
                stringBuilder.AppendLine();
                await channelToSend.SendMessageAsync(embed: embedWithNoAttachments);
                await arg.AddConfirmationAsync();
                return;
            }
            else // Gets fired if the message comes from a modmail channel inside the guild.
            {
                var modmail = await _doraemonContext
                    .Set<ModmailTicket>()
                    .Where(x => x.ModmailChannel == arg.Channel.Id)
                    .SingleOrDefaultAsync();
                if (modmail is null)
                {
                    return;
                }
                // TODO: Make this configurable.
                if (arg.Content.Contains("!")) // Don't wanna have commands being sent.
                {
                    return;
                }
                var user = _client.GetUser(modmail.UserId);
                var dmChannel = await _client.GetDMChannelAsync(modmail.DmChannel);
                if (dmChannel is null)
                {
                    dmChannel = await user.GetOrCreateDMChannelAsync();
                }
                var highestRole = (arg.Author as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First().Name;
                if (highestRole is null)
                {
                    highestRole = "@everyone";
                }

                if (arg.Attachments.Any())
                {
                    var image = arg.Attachments.ElementAt(0);
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg.Author.GetFullUsername(), arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .WithImageUrl(image.Url)
                        .WithDescription(arg.Content)
                        .WithFooter($"{highestRole}")
                        .Build();
                    await dmChannel.SendMessageAsync(embed: embed);
                    await arg.AddConfirmationAsync();
                    stringBuilder.AppendLine($"(Staff){arg.Author.GetFullUsername()} - {arg.Content}");
                    stringBuilder.AppendLine($"{image.Url}");
                    stringBuilder.AppendLine();
                    return;
                }
                var embedNoImage = new EmbedBuilder()
                    .WithAuthor(arg.Author.GetFullUsername(), arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .WithDescription(arg.Content)
                    .WithFooter($"{highestRole}")
                    .Build();
                await dmChannel.SendMessageAsync(embed: embedNoImage);
                await arg.AddConfirmationAsync();
                stringBuilder.AppendLine($"(Staff){arg.Author.GetFullUsername()} - {arg.Content}");
                stringBuilder.AppendLine();
            }
        }

        public async Task HandleEditedModmailMessageAsync(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        {
            if (arg2.Author.IsBot || arg2.Author.IsWebhook) return;

            var modmail = await _doraemonContext.ModmailTickets
                .Where(x => x.ModmailChannel == arg3.Id || x.DmChannel == arg3.Id)
                .SingleOrDefaultAsync();
            if(modmail is null)
            {
                return;
            }
            if(arg3.GetType() == typeof(SocketDMChannel))
            {

                var dmModmail = await _doraemonContext.ModmailTickets
                    .Where(x => x.DmChannel == arg3.Id)
                    .Where(x => x.UserId == arg2.Author.Id)
                    .SingleAsync();
                if(dmModmail is null)
                {
                    return;
                }
                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
                var modmailThreadChannel = guild.GetTextChannel(dmModmail.ModmailChannel);

                var channelMessages = await modmailThreadChannel.GetMessagesAsync(1).FlattenAsync();
                var lastMessage = channelMessages.ElementAt(0) as IUserMessage;
                if (lastMessage.Embeds.Count() != 1) return;

                if (lastMessage.Attachments.Any())
                {
                    var attachment = lastMessage.Attachments.ElementAt(0);
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg2.Author.GetFullUsername(), arg2.Author.GetDefiniteAvatarUrl())
                        .WithTitle($"Message Edited")
                        .WithColor(Color.Gold)
                        .WithImageUrl(attachment.Url)
                        .WithDescription(arg2.Content)
                        .WithFooter($"Message ID: {arg2.Id}")
                        .WithCurrentTimestamp()
                        .Build();
                    await lastMessage.ModifyAsync(x => x.Embed = embed);

                    stringBuilder.AppendLine($"Edited Message By: {arg2.Author.GetFullUsername()}");
                    stringBuilder.AppendLine($"**Before:** \"{arg1.Value.Content}\"");
                    stringBuilder.AppendLine($"**After:** \"{arg2.Content}\"");
                    stringBuilder.AppendLine($"{attachment.Url}");
                    stringBuilder.AppendLine();
                }
                else
                {
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg2.Author.GetFullUsername(), arg2.Author.GetDefiniteAvatarUrl())
                        .WithTitle($"Message Edited")
                        .WithColor(Color.Gold)
                        .WithDescription($"**Before:** {arg1.Value.Content}\n**After:** {arg2.Content}")
                        .WithFooter($"Message ID: {arg2.Id}")
                        .WithCurrentTimestamp()
                        .Build();


                    stringBuilder.AppendLine($"Edited Message By: {arg2.Author.GetFullUsername()}");
                    stringBuilder.AppendLine($"**Before:** \"{arg1.Value.Content}\"");
                    stringBuilder.AppendLine($"**After:** \"{arg2.Content}\"");
                    stringBuilder.AppendLine();
                    await lastMessage.ModifyAsync(x => x.Embed = embed);
                }


            }
            else
            {
                var dmModmail = await _doraemonContext.ModmailTickets
                    .Where(x => x.ModmailChannel == arg3.Id)
                    .Where(x => x.UserId == arg2.Author.Id)
                    .SingleAsync();
                if (dmModmail is null)
                {
                    return;
                }

                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);

                var user = _client.GetUser(dmModmail.UserId);

                // We randomly get a NullRef by fetching the DmChannel via ID, so we make sure to catch that.
                var dmChannel = await _client.GetDMChannelAsync(dmModmail.DmChannel) ?? await user.GetOrCreateDMChannelAsync();

                var dmChannelMessages = await dmChannel.GetMessagesAsync(1).FlattenAsync();

                var lastMessage = dmChannelMessages.ElementAt(0) as IUserMessage;

                var highestRole = (arg2.Author as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First().Name;
                if (highestRole is null)
                {
                    highestRole = "@everyone";
                }

                if (lastMessage.Embeds.Count() != 1) return;
                if (lastMessage.Attachments.Any())
                {
                    var image = lastMessage.Attachments.ElementAt(0);
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg2.Author.GetFullUsername(), arg2.Author.GetDefiniteAvatarUrl())
                        .WithColor(Color.Green)
                        .WithFooter($"{highestRole}")
                        .WithDescription($"{arg2.Content}")
                        .WithCurrentTimestamp()
                        .WithImageUrl(image.Url)
                        .Build();
                    stringBuilder.AppendLine($"Edited Message By: (Staff){arg2.Author.GetFullUsername()}");
                    stringBuilder.AppendLine($"**Before:** \"{arg1.Value.Content}\"");
                    stringBuilder.AppendLine($"**After:** \"{arg2.Content}\"");
                    stringBuilder.AppendLine($"{image.Url}");
                    stringBuilder.AppendLine();
                    await lastMessage.ModifyAsync(x => x.Embed = embed);
                }
                else
                {
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg2.Author.GetFullUsername(), arg2.Author.GetDefiniteAvatarUrl())
                        .WithColor(Color.Green)
                        .WithFooter($"{highestRole}")
                        .WithDescription($"{arg2.Content}")
                        .WithCurrentTimestamp()
                        .Build();
                    stringBuilder.AppendLine($"Edited Message By: {arg2.Author.GetFullUsername()}");
                    stringBuilder.AppendLine($"**Before:** \"{arg1.Value.Content}\"");
                    stringBuilder.AppendLine($"**After:** \"{arg2.Content}\"");
                    stringBuilder.AppendLine();
                    await lastMessage.ModifyAsync(x => x.Embed = embed);
                }
            }
        }
    }
}
