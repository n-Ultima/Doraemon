// Copyright 2016 Emzi0767
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;

namespace Doraemon.Data.TypeReaders
{
    public class TimeSpanTypeReader : DiscordGuildTypeParser<TimeSpan>
    {
        private static Regex TimeSpanRegex { get; } = new Regex(@"^(?<days>\d+d)?(?<hours>\d{1,2}h)?(?<minutes>\d{1,2}m)?(?<seconds>\d{1,2}s)?$", RegexOptions.Compiled);
        private static string[] RegexGroups { get; } = new string[] { "days", "hours", "minutes", "seconds" };
        public override async ValueTask<TypeParserResult<TimeSpan>> ParseAsync(Parameter parameter, string input, DiscordGuildCommandContext context)
        {
            await Task.Yield();

            var result = TimeSpan.Zero;
            if (input == "0")
                return Failure("0 is not a valid timespan.");

            if (TimeSpan.TryParse(input, out result))
                return Success(result);
            
            var mtc = TimeSpanRegex.Match(input);
            if (!mtc.Success)
                return Failure("Failed to parse TimeSpan.");

            var d = 0;
            var h = 0;
            var m = 0;
            var s = 0;
            foreach (var gp in RegexGroups)
            {
                var gpc = mtc.Groups[gp].Value;
                if (string.IsNullOrWhiteSpace(gpc))
                    continue;

                var gpt = gpc.Last();
                int.TryParse(gpc.Substring(0, gpc.Length - 1), out var val);
                switch (gpt)
                {
                    case 'd':
                        d = val;
                        break;

                    case 'h':
                        h = val;
                        break;

                    case 'm':
                        m = val;
                        break;

                    case 's':
                        s = val;
                        break;
                }
            }
            result = new TimeSpan(d, h, m, s);
            return Success(result);
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