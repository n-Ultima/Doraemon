using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Doraemon.Common
{
    public class ModerationConfiguration
    {
        private int _SpamMessageCountPerUser = default!;
        private int _SpamMessageTimeout = default!;
        private int _MassMentionTrigger = default!;
        private string _BanMessage = null!;
        private string[] _RestrictedWords = null!;
        private readonly string configurationPath = Path.Combine(Environment.CurrentDirectory, "moderationConfig.json");

        public int SpamMessageCountPerUser
        {
            get => _SpamMessageCountPerUser;
            set
            {
                if (value == default)
                {
                    throw new NullReferenceException(
                        $"The spam message count per user must be defined in {configurationPath}");
                }

                _SpamMessageCountPerUser = value;
            }
        }

        public int SpamMessageTimeout
        {
            get => _SpamMessageTimeout;
            set
            {
                if (value == default)
                    throw new NullReferenceException(
                        $"The spam message timeout must be defined in {configurationPath}");
                _SpamMessageTimeout = value;
            }
        }

        public string BanMessage
        {
            get => _BanMessage;
            set
            {
                if (value is null)
                    throw new NullReferenceException($"The ban message must be defined in {configurationPath}");
                _BanMessage = value;
            }

        }

        public int MassMentionTrigger
        {
            get => _MassMentionTrigger;
            set
            {
                if (value == default)
                    throw new NullReferenceException($"The mass mention trigger must be defined in {configurationPath}");
                _MassMentionTrigger = value;
            }
        }
        public string[] RestrictedWords
        {
            get => _RestrictedWords;
            set
            {
                if (value == null)
                {
                    Log.Logger.Warning($"No restricted words have been defined in {configurationPath}");
                }

                _RestrictedWords = value;
            }
        }
        public ModerationConfiguration()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("moderationConfig.json")
                .Build();
            RestrictedWords = config.GetSection(nameof(RestrictedWords)).Get<string[]>();
            SpamMessageTimeout = config.GetValue<int>(nameof(SpamMessageTimeout));
            SpamMessageCountPerUser = config.GetValue<int>(nameof(SpamMessageCountPerUser));
            BanMessage = config.GetValue<string>(nameof(BanMessage));
            MassMentionTrigger = config.GetValue<int>(nameof(MassMentionTrigger));
        }
    }
}