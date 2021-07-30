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
using Doraemon.Data.Models;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Doraemon.Data.Models.Core;


namespace Doraemon.Services.GatewayEventHandlers
{
    public class AutoModeration : DoraemonEventService
    {
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
            var messageChannel = await message.FetchChannelAsync();
            if (message.Attachments.Any())
            {
                var blackListedFileNames = message.Attachments
                    .Select(attachment => attachment.FileName.ToLower())
                    .Where(filename => BlacklistedExtensions
                        .Any(extension => filename.EndsWith(extension)))
                    .ToArray();
                await message.DeleteAsync();
                await messageChannel.SendMessageAsync(new LocalMessage()
                    .WithContent($"Your message had potentially harmful files attached, {Mention.User(message.Author)}: {string.Join(", ", blackListedFileNames)}\nFor posting this, a warn has also been applied to your moderation record. Please refrain from posting files that aren't allowed."));
                await InfractionService.CreateInfractionAsync(message.Author.Id, Bot.CurrentUser.Id, guild.Id, InfractionType.Warn, "Posting suspicious files.", false, null);
            }

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
        }

        /// <summary>
        ///     Returns if the unique invite identifier of a guild is present on the whitelist.
        /// </summary>
        /// <param name="inv"></param>
        /// <returns></returns>
        public async Task<bool> IsGuildWhiteListed(string inv)
        {
            var request = await _httpClient.GetStringAsync($"https://www.discord.com/api/invites/{inv}");
            var deserializedResponse = JsonSerializer.Deserialize<Root>(request);
            var id = deserializedResponse.Guild.Id;
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
        public string Id { get; set; }
    }
}