using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Doraemon.Common
{
    public class DoraemonConfiguration
    {
        private readonly string configurationPath = Path.Combine(Environment.CurrentDirectory, "config.json");
        private string _DbConnection = null!;
        private LogConfiguration _logConfiguration = null!;
        private ulong _MainGuildId;
        private string _Prefix = null!;
        private ulong _PromotionRoleId;
        private string _Token = null!;
        private static int timesWarned = 0;
        public DoraemonConfiguration()
        {
            LoadConfiguration();
        }

        /// <summary>
        ///     What the bot should listen for, signaling commands.
        /// </summary>
        public string Prefix
        {
            get => _Prefix;
            set
            {
                if (value == null) throw new NullReferenceException($"Prefix must be defined in {configurationPath}");
                _Prefix = value;
            }
        }

        /// <summary>
        ///     The role that a user can be promoted to. Users with this role can also nominate other users for promotions.
        /// </summary>
        public ulong PromotionRoleId
        {
            get => _PromotionRoleId;
            set
            {
                if (value == default)
                {
                    if (timesWarned == 0)
                    {
                        Log.Logger.Warning($"The PromotionRoleId was not set in {configurationPath}!\nThis means that the PromotionModule and service will not work.");
                    }
                    timesWarned++;
                }
                else
                {
                    _PromotionRoleId = value;
                }
            }
        }

        /// <summary>
        ///     The ID of the guild that the bot will be run in.
        /// </summary>
        public ulong MainGuildId
        {
            get => _MainGuildId;
            set
            {
                if (value == default)
                    throw new NullReferenceException(
                        $"The main guild ID that the bot will be ran in must be defined in {configurationPath}");
                _MainGuildId = value;
            }
        }

        /// <summary>
        ///     Your bot application's token.
        /// </summary>
        public string Token
        {
            get => _Token;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new NullReferenceException($"Token must be defined in {configurationPath}");
                _Token = value;
            }
        }

        /// <summary>
        ///     The connection string used to connect to your PostgreSQL database.
        /// </summary>
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
        /// <summary>
        ///     The <see cref="LogConfiguration" /> used for log channels.
        /// </summary>
        public LogConfiguration LogConfiguration
        {
            get => _logConfiguration;
            set
            {
                if (value == null)
                    throw new NullReferenceException($"Logging channel id's must be defined in {configurationPath}");
                _logConfiguration = value;
            }
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
            ulong promoLogChannelId;
            if (logConfiguration.GetValue<ulong>(nameof(LogConfiguration.PromotionLogChannelId)) == default)
            {
                promoLogChannelId = 0;
            }
            else
            {
                promoLogChannelId = config.GetValue<ulong>(nameof(LogConfiguration.PromotionLogChannelId));
            }
            LogConfiguration = new LogConfiguration
            {
                ModLogChannelId = logConfiguration.GetValue<ulong>(nameof(LogConfiguration.ModLogChannelId)), 
                PromotionLogChannelId = promoLogChannelId,
                UserJoinedLogChannelId = logConfiguration.GetValue<ulong>(nameof(LogConfiguration.UserJoinedLogChannelId)),
                MessageLogChannelId = logConfiguration.GetValue<ulong>(nameof(LogConfiguration.MessageLogChannelId)),
                EmbedOrText = logConfiguration.GetValue<string>(nameof(LogConfiguration.EmbedOrText)),
                MiscellaneousLogChannelId = logConfiguration.GetValue<ulong>(nameof(LogConfiguration.MiscellaneousLogChannelId))
            };
        }
    }
}