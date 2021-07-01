using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
