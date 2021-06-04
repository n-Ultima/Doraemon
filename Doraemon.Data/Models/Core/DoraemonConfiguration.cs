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
        private ulong _MainGuildId = default!;
        private ulong _PromotionRoleId = default!;
        private LogConfiguration _logConfiguration = null!;
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
        public LogConfiguration LogConfiguration
        {
            get => _logConfiguration;
            set
            {
                if(value == null)
                {
                    throw new NullReferenceException($"Logging channel id's must be defined in {configurationPath}");
                }
                _logConfiguration = value;
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
            PromotionRoleId = config.GetValue<ulong>(nameof(PromotionRoleId));

            var logConfiguration = config.GetSection(nameof(LogConfiguration));
            LogConfiguration = new LogConfiguration
            {
                ModLogChannelId = logConfiguration.GetValue<ulong>(nameof(LogConfiguration.ModLogChannelId)),
                PromotionLogChannelId = logConfiguration.GetValue<ulong>(nameof(LogConfiguration.PromotionLogChannelId)),
                UserJoinedLogChannelId = logConfiguration.GetValue<ulong>(nameof(LogConfiguration.UserJoinedLogChannelId))

            };
        }
    }
}
