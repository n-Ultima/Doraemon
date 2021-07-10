using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Doraemon.Common
{
    public class ModerationConfiguration
    {
        private int _SpamMessageCountPerUser = default!;
        private int _SpamMessageTimeout = default!;
        private string _BanMessage = null!;
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

        public ModerationConfiguration()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("moderationConfig.json")
                .Build();
            SpamMessageTimeout = config.GetValue<int>(nameof(SpamMessageTimeout));
            SpamMessageCountPerUser = config.GetValue<int>(nameof(SpamMessageCountPerUser));
            BanMessage = config.GetValue<string>(nameof(BanMessage));
        }
    }
}