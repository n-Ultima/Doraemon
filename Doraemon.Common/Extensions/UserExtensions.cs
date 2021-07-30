using Disqord.Models;

namespace Doraemon.Common.Extensions
{
    public static class UserExtensions
    {
        public static string GetFullUsername(this UserJsonModel user)
        {
            return user.Username + "#" + user.Discriminator;
        }
    }
}