using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Serilog;
namespace Doraemon.Data.Models.Core
{
    public class DoraemonConfiguration
    {
        private string _Prefix = null!;
        private string _Token = null!;
        private string _DbConnection = null!;
        private ulong _ModLogChannelId = default!;
        private ulong _MessageLogChannelId = default!;
        private ulong _UserJoinedLogChannelId = default!;
        private ulong _MainGuildId = default!;
        private ulong _PromotionRoleId = default!;
        private ulong _PromotionLogChannelId = default!;
        private readonly string configurationPath = Path.Combine(Environment.CurrentDirectory, "config.json");
        public string Prefix
        {
            get => _Prefix;
            set
            {
                if (value == null)
                {
                    throw new NullReferenceException($"Prefix must be defined in {configurationPath}");
                }
                _Prefix = value;
            }
        }
        public ulong PromotionLogChannelId
        {
            get => _PromotionLogChannelId;
            set
            {
                if (value == default)
                {
                    Log.Logger.Warning($"The Promotions Log Channel ID was not set in {configurationPath}.\nThis means that no promotions will be logged.");
                }
                _PromotionLogChannelId = value;
            }
        }
        public ulong PromotionRoleId
        {
            get => _PromotionRoleId;
            set
            {
                if (value == default)
                {
                    Log.Logger.Warning($"The PromotionRoleId was not set in {configurationPath}!\nThis means that the PromotionModule and service will not work.");
                }
                _PromotionRoleId = value;
            }
        }
        public ulong MainGuildId
        {
            get => _MainGuildId;
            set
            {
                if (value == default)
                {
                    throw new NullReferenceException($"The main guild ID that the bot will be ran in must be defined in {configurationPath}");
                }
                _MainGuildId = value;
            }
        }
        public ulong ModLogChannelId
        {
            get => _ModLogChannelId;
            set
            {
                if (value == default)
                {
                    Log.Logger.Warning($"The moderation log channel ID has not been defined {configurationPath}!\nThis means that no logging will occur!");
                    return;
                }
                _ModLogChannelId = value;
            }
        }
        public ulong MessageLogChannelId
        {
            get => _MessageLogChannelId;
            set
            {
                if (value == default)
                {
                    Log.Logger.Warning($"The message log channel ID has not been defined in {configurationPath}!\nThis means that no message updates will be logged.");
                    return;
                }
                _MessageLogChannelId = value;
            }
        }
        public ulong UserJoinedLogChannelId
        {
            get => _UserJoinedLogChannelId;
            set
            {
                if (value == default)
                {
                    Log.Logger.Warning($"The user joined log channel ID has not been defined in {configurationPath}!\nThis means that user joins will not be logged.");
                    return;
                }
                _UserJoinedLogChannelId = value;
            }
        }
        public string Token
        {
            get => _Token;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new NullReferenceException($"Token must be defined in {configurationPath}");
                }
                _Token = value;
            }
        }
        public string DbConnection
        {
            get => _DbConnection;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new NullReferenceException($"DB Connection must be defined in {configurationPath}");

                _DbConnection = value;
            }
        }
        public DoraemonConfiguration()
        {
            LoadConfiguration();
        }
        private void LoadConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile(configurationPath)
                .Build();
            DbConnection = config.GetValue<string>(nameof(DbConnection));
            MainGuildId = config.GetValue<ulong>(nameof(MainGuildId));
            Prefix = config.GetValue<string>(nameof(Prefix));
            Token = config.GetValue<string>(nameof(Token));
            ModLogChannelId = config.GetValue<ulong>(nameof(ModLogChannelId));
            MessageLogChannelId = config.GetValue<ulong>(nameof(MessageLogChannelId));
            UserJoinedLogChannelId = config.GetValue<ulong>(nameof(UserJoinedLogChannelId));
            PromotionRoleId = config.GetValue<ulong>(nameof(PromotionRoleId));
            PromotionLogChannelId = config.GetValue<ulong>(nameof(PromotionLogChannelId));
        }
    }
}
