using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Common.CommandHelp
{
    public class ParameterHelpData
    {
        public string Name { get; set; }

        public string Summary { get; set; }

        public string Type { get; set; }

        public bool IsOptional { get; set; }

        public IReadOnlyCollection<string> Options { get; set; }

        public static ParameterHelpData FromParameterInfo(ParameterInfo parameter)
        {
            bool isNullable = parameter.Type.IsGenericType && parameter.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
            var paramType = isNullable ? parameter.Type.GetGenericArguments()[0] : parameter.Type;
            string typeName = paramType.Name;

            if (paramType.IsInterface && paramType.Name.StartsWith('I'))
            {
                typeName = typeName.Substring(1);
            }

            var ret = new ParameterHelpData
            {
                Name = parameter.Name,
                Summary = parameter.Summary,
                Type = typeName,
                IsOptional = isNullable || parameter.IsOptional,
                Options = parameter.Type.IsEnum
                    ? parameter.Type.GetEnumNames()
                    : Array.Empty<string>(),
            };

            return ret;
        }
    }
}
