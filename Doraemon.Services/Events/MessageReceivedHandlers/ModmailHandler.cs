using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Common.Utilities;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;

namespace Doraemon.Services.Events.MessageReceivedHandlers
{
    [DoraemonService]
    public class ModmailHandler
    {
        public DiscordSocketClient _client;
        private readonly AuthorizationService _authorizationService;
        public GuildUserService _guildUserService;
        public ModmailTicketService _modmailTicketService;

        public ModmailHandler(DiscordSocketClient client, ModmailTicketService modmailTicketService,
            GuildUserService guildUserService, AuthorizationService authorizationService)
        {
            _client = client;
            _modmailTicketService = modmailTicketService;
            _guildUserService = guildUserService;
            _authorizationService = authorizationService;
        }

        public DoraemonConfiguration DoraemonConfig { get; } = new();

        /// <summary>
        ///     Message received handler used to handle modmail threads.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        public async Task ModmailAsync(SocketMessage arg)
        {
            
            if (arg.Author.IsBot || arg.Author.IsWebhook)
                return; // Make sure bot's & webhook's messages aren't being used.
            var User = await _guildUserService.FetchGuildUserAsync(arg.Author.Id);
            if (User is null)
                await _guildUserService.CreateGuildUserAsync(arg.Author.Id, arg.Author.Username,
                    arg.Author.Discriminator, false);
            
            if (arg.Channel.GetType() ==
                typeof(SocketDMChannel)) // This is if a message was received from a DM channel, not a modmail thread channel inside of a guild.
            {
                var modmailStart = await _guildUserService.FetchGuildUserAsync(arg.Author.Id);
                if (modmailStart.IsModmailBlocked)
                {
                    await arg.Channel.SendMessageAsync("You are not permitted to open modmail threads at this time.");
                    return;
                }
                var dmModmail = await _modmailTicketService.FetchModmailTicketAsync(arg.Author.Id);
                var modMailGuild = _client.GetGuild(DoraemonConfig.MainGuildId); // Get the guild defined in config.json
                var modmailLogChannel =
                    modMailGuild.GetTextChannel(DoraemonConfig.LogConfiguration.ModmailLogChannelId);
                var modMailCategory =
                    modMailGuild.GetCategoryChannel(DoraemonConfig
                        .ModmailCategoryId);
                if (dmModmail is null) // If the check is null, then we go ahead and create a new thread.
                {
                    var ID = DatabaseUtilities.ProduceId();
                    await arg.Channel.SendMessageAsync(
                        "Thank you for contacting Modmail! Staff will reply as soon as possible."); // Reply to the DM channel, so that the modmail starter knows that Staff will be with them soon.
                    var textChannel = await modMailGuild.CreateTextChannelAsync(arg.Author.GetFullUsername(),
                        x => x.CategoryId =
                            modMailCategory
                                .Id); // Make a text channel with the users username inside of the modmail category.
                    await textChannel.ModifyAsync(x => x.Topic = $"User ID: {arg.Author.Id}");
                    if (arg.Attachments.Any())
                    {
                        var image = arg.Attachments.ElementAt(0);
                        var firstMessageEmbed = new EmbedBuilder()
                            .WithAuthor(arg.Author.GetFullUsername(),
                                arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                            .WithColor(Color.Gold)
                            .WithDescription(arg.Content)
                            .WithCurrentTimestamp()
                            .WithImageUrl(image.Url)
                            .WithFooter($"Message ID: {arg.Id}")
                            .WithFooter($"Ticket ID: {ID}")
                            .Build(); // This will only be sent once per thread, so we have access to the Ticket ID.
                        await textChannel.SendMessageAsync(embed: firstMessageEmbed);
                        await _modmailTicketService.CreateModmailTicketAsync(ID, arg.Author.Id, arg.Channel.Id,
                            textChannel.Id);
                        await arg.AddConfirmationAsync(); // Add a checkmark to the user's message, just to again show that everything went smoothly.
                        await _modmailTicketService.AddMessageToModmailTicketAsync(ID, _client.CurrentUser.Id, $@"(SYSTEM)User {arg.Author.GetFullUsername()} opened a modmail ticket with message: {arg.Content}
With Image URL: {image.Url}
Ticket ID: {ID}" + "\n");
                        
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
                    await _modmailTicketService.CreateModmailTicketAsync(ID, arg.Author.Id, arg.Channel.Id,
                        textChannel.Id);

                    await arg.AddConfirmationAsync(); // Add a checkmark to the user's message, just to again show that everything went smoothly.
                    await _modmailTicketService.AddMessageToModmailTicketAsync(ID, _client.CurrentUser.Id, $@"(SYSTEM)User {arg.Author.GetFullUsername()} opened a modmail ticket with message: {arg.Content}
Ticket ID: {ID}" + "\n");
                    return;
                }

                // This gets fired if the message came from a DM Channel, but it's an already active thread.
                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
                var channelToSend = guild.GetTextChannel(dmModmail.ModmailChannelId);
                var modmail = await _modmailTicketService.FetchModmailTicketAsync(arg.Author.Id);
                if (channelToSend is null) return;
                if (arg.Attachments.Any())
                {
                    var image = arg.Attachments.ElementAt(0);
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg.Author.GetFullUsername(),
                            arg.Author.GetDefiniteAvatarUrl())
                        .WithColor(Color.Gold)
                        .WithDescription(arg.Content)
                        .WithCurrentTimestamp()
                        .WithImageUrl(image.Url)
                        .WithFooter($"Message ID: {arg.Id}")
                        .Build();
                    await channelToSend.SendMessageAsync(embed: embed);
                    await arg.AddConfirmationAsync();

                    await _modmailTicketService.AddMessageToModmailTicketAsync(modmail.Id, arg.Author.Id, $@"{arg.Author.GetFullUsername()} - {arg.Content}
With Image URL: {image.Url}" + "\n");                    
                    return;
                }

                var embedWithNoAttachments = new EmbedBuilder()
                    .WithAuthor(arg.Author.GetFullUsername(),
                        arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                    .WithColor(Color.Gold)
                    .WithCurrentTimestamp()
                    .WithDescription(arg.Content)
                    .WithFooter($"Message ID: {arg.Id}")
                    .Build();
                await _modmailTicketService.AddMessageToModmailTicketAsync(modmail.Id, arg.Author.Id, $@"{arg.Author.GetFullUsername()} - {arg.Content}");
                await channelToSend.SendMessageAsync(embed: embedWithNoAttachments);
                await arg.AddConfirmationAsync();
            }
            else // Gets fired if the message comes from a modmail channel inside the guild.
            {
                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
                var modmailCategory = guild.GetCategoryChannel(DoraemonConfig.ModmailCategoryId);
                if (!modmailCategory.Channels.Any(x => x.Id == arg.Channel.Id))
                {
                    return;
                }
                var modmail = await _modmailTicketService.FetchModmailTicketByModmailChannelIdAsync(arg.Channel.Id);
                if (modmail is null) return;
                // TODO: Make this configurable.
                if (arg is not SocketUserMessage message) return;
                var argPos = 0;
                if (message.HasStringPrefix(DoraemonConfig.Prefix, ref argPos)) return; // don't want commands being sent.
                var user = _client.GetUser(modmail.UserId);
                var dmChannel = await _client.GetDMChannelAsync(modmail.DmChannelId);
                if (dmChannel is null) dmChannel = await user.GetOrCreateDMChannelAsync();
                var highestRole = (arg.Author as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First().Name;
                if (highestRole is null) highestRole = "@everyone";

                if (arg.Attachments.Any())
                {
                    var image = arg.Attachments.ElementAt(0);
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg.Author.GetFullUsername(),
                            arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                        .WithColor(Color.Green)
                        .WithCurrentTimestamp()
                        .WithImageUrl(image.Url)
                        .WithDescription(arg.Content)
                        .WithFooter($"{highestRole}")
                        .Build();
                    await dmChannel.SendMessageAsync(embed: embed);
                    await arg.AddConfirmationAsync();
                    
                    await _modmailTicketService.AddMessageToModmailTicketAsync(modmail.Id, arg.Author.Id, $@"(Staff){arg.Author.GetFullUsername()} - {arg.Content}
With Image URl: {image.Url}" + "\n");
                    return;
                }

                var embedNoImage = new EmbedBuilder()
                    .WithAuthor(arg.Author.GetFullUsername(),
                        arg.Author.GetAvatarUrl() ?? arg.Author.GetDefaultAvatarUrl())
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .WithDescription(arg.Content)
                    .WithFooter($"{highestRole}")
                    .Build();
                await dmChannel.SendMessageAsync(embed: embedNoImage);
                await arg.AddConfirmationAsync();
                await _modmailTicketService.AddMessageToModmailTicketAsync(modmail.Id, arg.Author.Id, $@"(Staff){arg.Author.GetFullUsername()} - {arg.Content}");
            }
        }

        public async Task HandleEditedModmailMessageAsync(Cacheable<IMessage, ulong> arg1, SocketMessage arg2,
            ISocketMessageChannel arg3)
        {
            if (arg2.Author.IsBot || arg2.Author.IsWebhook) return;

            var modmail = await _modmailTicketService.FetchModmailTicketByModmailChannelIdAsync(arg3.Id);
            if (modmail is null) modmail = await _modmailTicketService.FetchModmailTicketByDmChannelIdAsync(arg3.Id);
            // If the message was edited in a DM channel.
            if (arg3.GetType() == typeof(SocketDMChannel))
            {
                var dmModmail = await _modmailTicketService.FetchModmailTicketByDmChannelIdAsync(arg3.Id);
                if (dmModmail is null) return;
                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
                var modmailThreadChannel = guild.GetTextChannel(dmModmail.ModmailChannelId);

                var channelMessages = await modmailThreadChannel.GetMessagesAsync(1).FlattenAsync();
                var lastMessage = channelMessages.ElementAt(0) as IUserMessage;
                if (lastMessage.Embeds.Count() != 1) return;

                if (lastMessage.Attachments.Any())
                {
                    var attachment = lastMessage.Attachments.ElementAt(0);
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg2.Author.GetFullUsername(), arg2.Author.GetDefiniteAvatarUrl())
                        .WithTitle("Message Edited")
                        .WithColor(Color.Gold)
                        .WithImageUrl(attachment.Url)
                        .WithDescription(arg2.Content)
                        .WithFooter($"Message ID: {arg2.Id}")
                        .WithCurrentTimestamp()
                        .Build();
                    await lastMessage.ModifyAsync(x => x.Embed = embed);

                    await _modmailTicketService.AddMessageToModmailTicketAsync(dmModmail.Id, _client.CurrentUser.Id, $@"(SYSTEM)Edited Message By: {arg2.Author.GetFullUsername()}
**Before:** {arg1.Value.Content}
**After:** {arg2.Content}
Included Image: {attachment.Url}" + "\n");
                }
                else
                {
                    var embed = new EmbedBuilder()
                        .WithAuthor(arg2.Author.GetFullUsername(), arg2.Author.GetDefiniteAvatarUrl())
                        .WithTitle("Message Edited")
                        .WithColor(Color.Gold)
                        .WithDescription($"**Before:** {arg1.Value.Content}\n**After:** {arg2.Content}")
                        .WithFooter($"Message ID: {arg2.Id}")
                        .WithCurrentTimestamp()
                        .Build();


                    await _modmailTicketService.AddMessageToModmailTicketAsync(dmModmail.Id, _client.CurrentUser.Id, $@"(SYSTEM)Edited Message By: {arg2.Author.GetFullUsername()}
**Before:** {arg1.Value.Content}
**After:** {arg2.Content}" + "\n");
                    await lastMessage.ModifyAsync(x => x.Embed = embed);
                }
            }
            // If a Staff member edited their message.
            else
            {
                var dmModmail = await _modmailTicketService.FetchModmailTicketByModmailChannelIdAsync(arg3.Id);
                if (dmModmail is null) return;

                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);

                var user = _client.GetUser(dmModmail.UserId);

                // We randomly get a NullRef by fetching the DmChannel via ID, so we make sure to catch that.
                var dmChannel = await _client.GetDMChannelAsync(dmModmail.DmChannelId) ??
                                await user.GetOrCreateDMChannelAsync();

                var dmChannelMessages = await dmChannel.GetMessagesAsync(1).FlattenAsync();

                var lastMessage = dmChannelMessages.ElementAt(0) as IUserMessage;

                var highestRole = (arg2.Author as SocketGuildUser).Roles.OrderByDescending(x => x.Position).First()
                    .Name;
                if (highestRole is null) highestRole = "@everyone";

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
                    
                    await _modmailTicketService.AddMessageToModmailTicketAsync(dmModmail.Id, _client.CurrentUser.Id, $@"(SYSTEM)Edited Message By: (Staff){arg2.Author.GetFullUsername()}
**Before:** {arg1.Value.Content}
**After:** {arg2.Content}
Included Image: {image.Url}" + "\n");
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

                    await _modmailTicketService.AddMessageToModmailTicketAsync(dmModmail.Id, _client.CurrentUser.Id, $@"(SYSTEM)Edited Message By: (Staff){arg2.Author.GetFullUsername()}
**Before:** {arg1.Value.Content}
**After:** {arg2.Content}" + "\n");
                    await lastMessage.ModifyAsync(x => x.Embed = embed);
                }
            }
        }
    }
}