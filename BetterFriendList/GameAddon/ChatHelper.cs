using FFXIVClientStructs.FFXIV.Client.System.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

namespace BetterFriendList.GameAddon;

public static class ChatHelper
{
    public static unsafe void ExecuteCommand(string command)
    {
        if (!command.StartsWith('/'))
            return;

        using var cmd = new Utf8String(command);

        // Technically not needed since we don't use payloads but provides a better example.
        cmd.SanitizeString(
            AllowedEntities.Unknown9     |
            AllowedEntities.Payloads          |
            AllowedEntities.OtherCharacters   |
            AllowedEntities.SpecialCharacters |
            AllowedEntities.Numbers           |
            AllowedEntities.LowercaseLetters  |
            AllowedEntities.UppercaseLetters  );

        if (cmd.Length > 500)
            return;

        UIModule.Instance()->ProcessChatBoxEntry(&cmd);
        //RaptureShellModule.Instance()->ExecuteCommandInner(&cmd, UIModule.Instance());
    }

    public static unsafe bool IsInputTextActive => RaptureAtkModule.Instance()->IsTextInputActive();
}
