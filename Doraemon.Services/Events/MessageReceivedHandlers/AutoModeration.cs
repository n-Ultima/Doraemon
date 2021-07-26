using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Doraemon.Common;
using Doraemon.Common.Utilities;
using Doraemon.Data;
using Doraemon.Data.Models;
using Doraemon.Data.Models.Core;
using Doraemon.Services.Core;
using Doraemon.Services.Moderation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace Doraemon.Services.Events.MessageReceivedHandlers
{
    [DoraemonService]
    public class AutoModeration
    {
        public const string muteRoleName = "Doraemon_Moderation_Mute";

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

        public Timer timer;
        public ConcurrentDictionary<ulong, int> UserMessages = new();
        private readonly DiscordSocketClient _client;
        private readonly ClaimService _claimService;
        private readonly GuildManagementService _guildManagementService;
        private readonly InfractionService _infractionService;
        public ModerationConfiguration ModerationConfig { get; private set; } = new();

        public AutoModeration
        (
            InfractionService infractionService,
            DiscordSocketClient client,
            ClaimService claimService,
            GuildManagementService guildManagementService
        )
        {
            _infractionService = infractionService;
            _client = client;
            _guildManagementService = guildManagementService;
            
            _claimService = claimService;
            SetTimer();
        }

        public static DoraemonConfiguration DoraemonConfig { get; } = new();

       

        private void SetTimer()
        {
            var timeSpan = TimeSpan.FromSeconds(ModerationConfig.SpamMessageTimeout);
            Log.Logger.Information($"Started the anti-spam timer!\nDuration: {ModerationConfig.SpamMessageTimeout} seconds\n");
            timer = new Timer(_ => _ = Task.Run(HandleTimerAsync), null, timeSpan, TimeSpan.FromSeconds(1));
        }

        public async Task HandleTimerAsync()
        {
            var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
            var usersToWarn = UserMessages.Where(x => x.Value >= ModerationConfig.SpamMessageCountPerUser);
            var usersNotToWarn = UserMessages.Where(x => x.Value < ModerationConfig.SpamMessageCountPerUser);
            foreach (var user in usersToWarn.ToList())
            {
                var messageAuthor = guild.GetUser(user.Key);
                if (messageAuthor is null) continue;
                
                await _infractionService.CreateInfractionAsync(messageAuthor.Id, _client.CurrentUser.Id, guild.Id,
                    InfractionType.Warn, "Spamming messages.", false, null);
                UserMessages.Remove(user.Key, out var success);
                await Task.Delay(250);
            }

            foreach (var user in usersNotToWarn.ToList())
            {
                UserMessages.TryRemove(user.Key, out _);
            }

            //timer.Change(TimeSpan.FromSeconds(ModerationConfig.SpamMessageTimeout), Timeout.InfiniteTimeSpan);
        }

        public async Task CheckForMultipleMessageSpamAsync(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message)) return;
            if (message.Channel is SocketDMChannel) return;
            if (message.Source != MessageSource.User)
                return;
            var context = new SocketCommandContext(_client, message);
            if (await _claimService.UserHasClaimAsync(message.Author.Id, ClaimMapType.BypassAutoModeration))
            {
                return;
            }
            var check = UserMessages.Where(x => x.Key == message.Author.Id);
            UserMessages.AddOrUpdate(message.Author.Id, 1, (_, oldValue) => oldValue + 1);
        }

        public async Task CheckForSpamAsync(SocketMessage arg)
        {
            if (arg.Channel.GetType() == typeof(SocketDMChannel)) return;
            if (!(arg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(_client, message);
            var autoModId = _client.CurrentUser.Id;
            if (message.Content.Length > 1200)
                // Put the lowest role allowed to bypass the spam filter.
                if (! await _claimService.UserHasClaimAsync(message.Author.Id, ClaimMapType.BypassAutoModeration))
                {
                    await message.DeleteAsync();
                    await context.Channel.SendMessageAsync(
                        $"{context.Message.Author.Mention}, you aren't allowed to spam. Continuing to do this will result in a mute.");
                    await _infractionService.CreateInfractionAsync(message.Author.Id, autoModId, context.Guild.Id,
                        InfractionType.Warn, "Spamming characters in a message", false, null);
                }

            var mentions = message.MentionedUsers;
            if (mentions.Count > 4)
            {
                if (!await _claimService.UserHasClaimAsync(message.Author.Id, ClaimMapType.BypassAutoModeration))
                {
                    await message.DeleteAsync();
                    await context.Channel.SendMessageAsync(
                        $"{context.Message.Author.Mention}, you aren't allowed to spam mentions. Continuing to do this will result in a mute.");
                    await _infractionService.CreateInfractionAsync(message.Author.Id, autoModId, context.Guild.Id,
                        InfractionType.Warn, "Spamming mentions in a message", false, null);
                }
                
            }
        }

        public async Task CheckForBlacklistedAttachmentTypesAsync(SocketMessage arg)
        {
            if (arg.Channel.GetType() == typeof(SocketDMChannel)) return;
            if (!(arg is SocketUserMessage message)) return;
            var channel = message.Channel;
            var author = message.Author;
            var guild = (channel as SocketGuildChannel)?.Guild;
            if (guild is null) return;
            if (!message.Attachments.Any()) return;
            if (message.Author.IsWebhook || message.Author.IsBot) return;
            var selfUser = _client.CurrentUser;
            var blackListedFileNames = message.Attachments
                .Select(attachment => attachment.Filename.ToLower())
                .Where(filename => BlacklistedExtensions
                    .Any(extension => filename.EndsWith(extension)))
                .ToArray();
            if (!blackListedFileNames.Any()) return;
            
            if (!await _claimService.UserHasClaimAsync(author.Id, ClaimMapType.BypassAutoModeration))
            {
                await message.DeleteAsync();
                await channel.SendMessageAsync($"Your message had potentially harmful files attached, {message.Author.Mention}: {string.Join(", ", blackListedFileNames)}\nFor posting this, a warn has also been applied to your moderation record. Please refrain from posting files that aren't allowed.");
                await _infractionService.CreateInfractionAsync(message.Author.Id, selfUser.Id, guild.Id,
                    InfractionType.Warn, "Posting suspicious files.", false, null);
            }
            
        }

        public async Task CheckForRestrictedWordsAsync(SocketMessage arg)
        {
            if (arg.Channel.GetType() == typeof(SocketDMChannel)) return;
            if (!(arg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(_client, message);
            // Declare the filtered-word list in advance.
            var restrictedWords = ModerationConfig.RestrictedWords;
            foreach (var word in restrictedWords)
            {
                var guild = _client.GetGuild(DoraemonConfig.MainGuildId);
                // If the message contains the word, it will perform the actions. However, by writing it like this, we prevent some cases like the word "ass" being detected in "class".
                if (message.Content.ToLower().Split(" ").Intersect(restrictedWords).Any())
                {
                    var autoModId = _client.CurrentUser.Id;
                    var caseId = DatabaseUtilities.ProduceId();
                    if (! await _claimService.UserHasClaimAsync(message.Author.Id, ClaimMapType.BypassAutoModeration))
                    {
                        // Deletes the message and warns the user.
                        await message.DeleteAsync();
                        await context.Channel.SendMessageAsync(
                            $"{context.Message.Author.Mention}, you aren't allowed to use offensive language here. Continuing to do this will result in a mute.");
                        await _infractionService.CreateInfractionAsync(message.Author.Id, autoModId, context.Guild.Id,
                            InfractionType.Warn, "NSFW Language", false, null);
                    }
                    
                }
            }
        }

        public async Task CheckForDiscordInviteLinksAsync(SocketMessage arg)
        {
            if (arg.Channel.GetType() == typeof(SocketDMChannel)) return;
            if (!(arg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var context = new SocketCommandContext(_client, message);
            var autoModId = _client.CurrentUser.Id;
            var match = Regex.Match(message.Content,
                @"(https?://)?(www.)?(discord.(gg|com|io|me|li)|discordapp.com/invite)/([a-z]+)");
            if (match.Success)
            {
                // discord.gg/code 
                // -> code
                var g = match.Groups[5].ToString();
                try
                {
                    if (!await IsGuildWhiteListed(g))
                        // Before deletion, we check if the user is a moderator.
                        if (! await _claimService.UserHasClaimAsync(message.Author.Id, ClaimMapType.BypassAutoModeration))
                        {
                            await _infractionService.CreateInfractionAsync(message.Author.Id, autoModId,
                                context.Guild.Id, InfractionType.Warn,
                                "Posting Discord Invite Links that are not present on the whitelist.", false, null);
                            await message.DeleteAsync();
                            await context.Channel.SendMessageAsync(
                                $"{context.Message.Author.Mention}, you aren't allowed to post Discord Invite Links that aren't present on the whitelist, continuing will result in a mute.");
                        }
                }
                catch // Catch exceptions
                {
                    Log.Logger.Warning(
                        $"The object: \"{g}\", when sent to the endpoint for retrieving guild information, returned 404 Not Found.");
                }
            }
        }

        /// <summary>
        ///     Returns if the unique invite identifier of a guild is present on the whitelist.
        /// </summary>
        /// <param name="inv"></param>
        /// <returns></returns>
        public async Task<bool> IsGuildWhiteListed(string inv)
        {
            var request = await _client.GetInviteAsync(inv);
            if (!request.GuildId.HasValue)
            {
                return false;
            }
            
            var whiteListedGuilds = await _guildManagementService.FetchAllWhitelistedGuildsAsync();
            return whiteListedGuilds.Where(x => x.Id == request.GuildId.ToString()) != null;
        }
    }
}