using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity.Menus;
using Disqord.Rest;

namespace Doraemon.Modules
{
    /// <summary>
    /// Represents a <see cref="DiscordGuildModuleBase"/> with custom <see cref="DiscordCommandResult"/>s.
    /// </summary>
    public class DoraemonGuildModuleBase : DiscordGuildModuleBase
    {
        /// <summary>
        /// Returns a <see cref="DiscordCommandResult"/> that adds the ✅ to the message.
        /// </summary>
        /// <returns>A <see cref="DiscordCommandResult"/> representing success of a command.</returns>
        protected DiscordCommandResult Confirmation()
        {
            return Reaction(new LocalEmoji("✅"));
        }
        
        /// <summary>
        /// Prompts the user for confirmation that what's about to happen, should be what's about to happen.
        /// </summary>
        /// <param name="message">The message to display to the user.</param>
        /// <returns>A <see cref="bool"/> representing if the user confirmed or denied the action.</returns>
        protected async ValueTask<bool> PromptAsync(LocalMessage message = null)
        {
            var view = new PromptView(message ?? new LocalMessage().WithContent("Do you want to proceed?"));
            await View(view);
            return view.Result;
        }

        private sealed class PromptView : ViewBase
        {
            public bool Result { get; private set; }

            public PromptView(LocalMessage message)
                : base(message)
            { }

            [Button(Label = "Confirm", Style = LocalButtonComponentStyle.Success)]
            public ValueTask Confirm(ButtonEventArgs e)
                => HandleAsync(true, e);

            [Button(Label = "Cancel", Style = LocalButtonComponentStyle.Danger)]
            public ValueTask Deny(ButtonEventArgs e)
                => HandleAsync(false, e);

            private ValueTask HandleAsync(bool result, ButtonEventArgs e)
            {
                Result = result;
                var message = (Menu as InteractiveMenu).Message;
                _ = result ? message.DeleteAsync() : message.ModifyAsync(x =>
                {
                    x.Content = "Cancellation received, cancelling the operation.";
                    x.Embeds = Array.Empty<LocalEmbed>();
                    x.Components = Array.Empty<LocalRowComponent>();
                });
                Menu.Stop();
                return default;
            }
        }
    }
}