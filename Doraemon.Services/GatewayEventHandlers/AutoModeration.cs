using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure.Design;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Doraemon.Common;
using Doraemon.Common.Extensions;
using Doraemon.Data.Models;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Doraemon.Data.Models.Core;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;


namespace Doraemon.Services.GatewayEventHandlers
{
    public class AutoModeration : DoraemonEventService
    {
        public override int Priority => int.MaxValue - 2;

        public static readonly IReadOnlyCollection<string> BlacklistedExtensions = new[]
        {
            ".exe",
            ".dll",
            ".application",
            ".msc",
            ".bat",
            ".pdb",
            ".sh",
            ".com",
            ".scr",
            ".msi",
            ".cmd",
            ".vbs",
            ".js",
            ".reg",
            ".pif",
            ".msp",
            ".hta",
            ".cpl",
            ".jar",
            ".vbe",
            ".ws",
            ".wsf",
            ".wsc",
            ".wsh",
            ".ps1",
            ".ps1xml",
            ".ps2",
            ".ps2xml",
            ".psc1",
            ".pasc2",
            ".msh",
            ".msh1",
            ".msh2",
            ".mshxml",
            ".msh1xml",
            ".msh2xml",
            ".scf",
            ".lnk",
            ".inf",
            ".doc",
            ".xls",
            ".ppt",
            ".docm",
            ".dotm",
            ".xlsm",
            ".xltm",
            ".xlam",
            ".pptm",
            ".potm",
            ".ppam",
            ".ppsm",
            ".sldn",
            ".sb"
        };

        public DoraemonConfiguration DoraemonConfig { get; private set; } = new();
        public ModerationConfiguration ModerationConfig { get; private set; } = new();
        private readonly HttpClient _httpClient;
        private readonly GuildManagementService _guildManagementService;

        public AutoModeration(AuthorizationService authorizationService, InfractionService infractionService, HttpClient httpClient, GuildManagementService guildManagementService)
            : base(authorizationService, infractionService)
        {
            _httpClient = httpClient;
            _guildManagementService = guildManagementService;
        }

        /// <summary>
        /// This method is used for checking all messages. We ignore users with the <see cref="ClaimMapType.BypassAutoModeration"/> claim. The only check we don't perform here is the dynamic spam check.
        /// </summary>
        /// <param name="eventArgs"></param>
        protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs eventArgs)
        {
            if (eventArgs.Channel == null) return;
            if (AuthorizationService.CurrentClaims.Contains(ClaimMapType.BypassAutoModeration)) return;
            if (eventArgs.Message is not IUserMessage message) return;
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            if (message.Author.IsBot) return;
            var messageChannel = await message.FetchChannelAsync();
            if (message.Author.Id == guild.OwnerId) return;
            if (message.Attachments.Any())
            {
                var blackListedFileNames = message.Attachments
                    .Select(attachment => attachment.FileName.ToLower())
                    .Where(filename => BlacklistedExtensions
                        .Any(extension => filename.EndsWith(extension)))
                    .ToArray();
                if (!blackListedFileNames.Any())
                {
                    // If I just "return;" here, we will completely skip the discord link, and the restricted words check
                    // This way, people can't just upload an empty.txt file and bypass the rest of the automoderation.
                    goto DiscordAutoMod;
                }

                await message.DeleteAsync();
                await messageChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"Your message had potentially harmful files attached, {Mention.User(message.Author)}: {string.Join(", ", blackListedFileNames)}\nFor posting this, a warn has also been applied to your moderation record. Please refrain from posting files that aren't allowed."));
                await InfractionService.CreateInfractionAsync(message.Author.Id, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "Posting suspicious files.", false, null);
            }

            DiscordAutoMod:
            var match = Regex.Match(message.Content, @"(https?://)?(www.)?(discord.(gg|com|io|me|li)|discordapp.com/invite)/([a-z]+)");
            if (match.Success)
            {
                var group = match.Groups[5];
                if (!await IsGuildWhiteListed(group.ToString()))
                {
                    await message.DeleteAsync();
                    await messageChannel.SendMessageAsync(new LocalMessage()
                        .WithContent($"{Mention.User(message.Author)}, you can't post Discord Invite Links here that haven't been whitelisted. Please refrain from doing so again."));
                    await InfractionService.CreateInfractionAsync(message.Author.Id.RawValue, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "Advertising via Discord Invite Link.", false, null);
                }
            }

            var restrictedWords = ModerationConfig.RestrictedWords;
            var splitMessage = message.Content.ToLower().Split(" ");
            if (splitMessage.Intersect(restrictedWords).Any())
            {
                await message.DeleteAsync();
                await messageChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"{Mention.User(message.Author)}, using that language is prohibited. Please refrain from doing so again."));
                await InfractionService.CreateInfractionAsync(message.Author.Id, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "NSFW language", false, null);
            }

            var capsLockMatch = Regex.Match(message.Content, @"\p{Lu}{10,}");
            if (capsLockMatch.Success)
            {
                await message.DeleteAsync();
                await messageChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"{Mention.User(message.Author)}, spamming caps isn't allowed. Please refrain from doing so again."));
                await InfractionService.CreateInfractionAsync(message.Author.Id, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "Spamming caps.", false, null);
            }
        }

        protected override async ValueTask OnMessageUpdated(MessageUpdatedEventArgs eventArgs)
        {
            if (eventArgs.NewMessage == null) return;
            if (eventArgs.NewMessage.GetChannel() == null) return;
            if (eventArgs.NewMessage.GetChannel().CategoryId == DoraemonConfig.ModmailCategoryId) return;
            if (eventArgs.NewMessage is not IUserMessage newMessage) return;
            if (eventArgs.OldMessage is not IUserMessage oldMessage) return;

            if (oldMessage.Content == newMessage.Content) return;
            if (AuthorizationService.CurrentClaims.Contains(ClaimMapType.BypassAutoModeration))
                goto Log;
            var guild = Bot.GetGuild(DoraemonConfig.MainGuildId);
            var messageChannel = await newMessage.FetchChannelAsync();
            if (newMessage.Attachments.Any())
            {
                var blackListedFileNames = newMessage.Attachments
                    .Select(attachment => attachment.FileName.ToLower())
                    .Where(filename => BlacklistedExtensions
                        .Any(extension => filename.EndsWith(extension)))
                    .ToArray();
                if (!blackListedFileNames.Any())
                {
                    // If I just "return;" here, we will completely skip the discord link, and the restricted words check
                    // This way, people can't just upload an empty.txt file and bypass the rest of the automoderation.
                    goto DiscordAutoMod;
                }

                await newMessage.DeleteAsync();
                await messageChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"Your message had potentially harmful files attached, {Mention.User(newMessage.Author)}: {string.Join(", ", blackListedFileNames)}\nFor posting this, a warn has also been applied to your moderation record. Please refrain from posting files that aren't allowed."));
                await InfractionService.CreateInfractionAsync(newMessage.Author.Id, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "Posting suspicious files.", false, null);
            }

            DiscordAutoMod:
            var match = Regex.Match(newMessage.Content, @"(https?://)?(www.)?(discord.(gg|com|io|me|li)|discordapp.com/invite)/([a-z]+)");
            if (match.Success)
            {
                var group = match.Groups[5];
                if (!await IsGuildWhiteListed(group.ToString()))
                {
                    await newMessage.DeleteAsync();
                    await messageChannel.SendMessageAsync(new LocalMessage()
                        .WithContent($"{Mention.User(newMessage.Author)}, you can't post Discord Invite Links here that haven't been whitelisted. Please refrain from doing so again."));
                    await InfractionService.CreateInfractionAsync(newMessage.Author.Id.RawValue, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "Advertising via Discord Invite Link.", false, null);
                }
            }

            var restrictedWords = ModerationConfig.RestrictedWords;
            var splitMessage = newMessage.Content.ToLower().Split(" ");
            if (splitMessage.Intersect(restrictedWords).Any())
            {
                await newMessage.DeleteAsync();
                await messageChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"{Mention.User(newMessage.Author)}, using that language is prohibited. Please refrain from doing so again."));
                await InfractionService.CreateInfractionAsync(newMessage.Author.Id, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "NSFW language", false, null);
            }

            var capsLockMatch = Regex.Match(newMessage.Content, @"\p{Lu}{10,}");
            if (capsLockMatch.Success)
            {
                await newMessage.DeleteAsync();
                await messageChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"{Mention.User(newMessage.Author)}, spamming caps isn't allowed. Please refrain from doing so again."));
                await InfractionService.CreateInfractionAsync(newMessage.Author.Id, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "Spamming caps.", false, null);
            }

            Log:
            var embed = new LocalEmbed()
                .WithColor(DColor.Gold)
                .WithAuthor(newMessage.Author)
                .WithDescription($"Message edited in {Mention.Channel(newMessage.ChannelId)}\n**Before:** {oldMessage.Content}\n**After:** {newMessage.Content}")
                .WithFooter($"Author Id: {newMessage.Author.Id}")
                .WithTimestamp(DateTimeOffset.UtcNow);
            await Bot.SendMessageAsync(DoraemonConfig.LogConfiguration.MessageLogChannelId, new LocalMessage().WithEmbeds(embed));
        }

        /// <summary>
        ///     Returns if the unique invite identifier of a guild is present on the whitelist.
        /// </summary>
        /// <param name="inv"></param>
        /// <returns></returns>
        public async Task<bool> IsGuildWhiteListed(string inv)
        {
            var request = await _httpClient.GetStringAsync($"https://www.discord.com/api/invites/{inv}");
            var deserializedResponse = JsonConvert.DeserializeObject<Root>(request);
            var id = deserializedResponse.Guild.id;
            if (id == null) // if the request was sent, this is basically a 404 not found.
                return true;
            var whiteListedGuilds = await _guildManagementService.FetchAllWhitelistedGuildsAsync();
            return whiteListedGuilds.FirstOrDefault(x => x.Id == id) != null;
        }
    }

    public class Root
    {
        public Guild Guild { get; set; }
    }

    public class Guild
    {
        public string id { get; set; }
    }
}