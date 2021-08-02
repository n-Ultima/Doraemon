using System;
using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;

namespace Doraemon.Data.TypeReaders
{
    public class TimeSpanTypeReader : DiscordGuildTypeParser<TimeSpan>
    {
        
        public override ValueTask<TypeParserResult<TimeSpan>> ParseAsync(Parameter parameter, string value, DiscordGuildCommandContext context)
        {
            return TryParseTimeSpan(value.ToLowerInvariant(), out var timeSpan)
                ? Success(timeSpan)
                : Failure("Failed to parse time span.");
        }
        public bool TryParseTimeSpan(ReadOnlySpan<char> input, out TimeSpan result)
        {
            result = TimeSpan.Zero;

            if (input.Length <= 1)
                return false;

            var start = 0;

            while (start < input.Length)
                if (char.IsDigit(input[start]))
                {
                    var i = start + 1;

                    while (i < input.Length - 1 && char.IsDigit(input[i]))
                        i++;

                    if (!double.TryParse(input.Slice(start, i - start), out var timeQuantity))
                        return false;

                    switch (input[i])
                    {
                        case 'w':
                            result += TimeSpan.FromDays(timeQuantity * 7);
                            break;
                        case 'd':
                            result += TimeSpan.FromDays(timeQuantity);
                            break;
                        case 'h':
                            result += TimeSpan.FromHours(timeQuantity);
                            break;
                        case 'm':
                            result += TimeSpan.FromMinutes(timeQuantity);
                            break;
                        case 's':
                            result += TimeSpan.FromSeconds(timeQuantity);
                            break;
                        case 'y':
                            result += TimeSpan.FromDays(timeQuantity * 365);
                            break;
                        default:
                            return false;
                    }

                    start = i + 1;
                }
                else
                {
                    return false;
                }

            return true;
        }
        
    }
}