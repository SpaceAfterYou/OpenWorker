using Arch.Core;
using OpenWorker.Channel.Community;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Commands.Attributes;
using OpenWorker.Hotspot.Modules.Chat.Responses;

namespace OpenWorker.Commands;

[CommandTrigger("notice")]
[CommandDescription("Show a line to everybody on every server")]
internal sealed partial class NoticeCommand : ICommand
{
    private const int MaxLength = 255;

    [ExternalDependency]
    private CommunityNetwork Network { get; }

    async ValueTask<bool> ICommand.TryExecute(Entity player, string[] tokens)
    {
        var text = string.Join(' ', tokens).Trim();

        if (text.Length is 0 or > MaxLength)
        {
            return false;
        }

        await Network.BroadcastAsync(new ChatNoticeResponse { Message = text });

        return true;
    }
}
