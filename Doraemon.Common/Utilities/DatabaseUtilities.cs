using System;

namespace Doraemon.Common.Utilities
{
    public static class DatabaseUtilities
    {
        public static string ProduceId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}