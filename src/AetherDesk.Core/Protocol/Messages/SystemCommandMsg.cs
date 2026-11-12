namespace AetherDesk.Core.Protocol.Messages;

public enum SystemCommandType : byte
{
    CtrlAltDel = 1,
    WinKey = 2,
    LockWorkstation = 3,
    TaskManager = 4
}

public sealed class SystemCommandMsg
{
    public SystemCommandType Command { get; set; }

    public byte[] Serialize() => new[] { (byte)Command };

    public static SystemCommandMsg Deserialize(ReadOnlySpan<byte> span)
    {
        return new SystemCommandMsg
        {
            Command = span.Length > 0 ? (SystemCommandType)span[0] : SystemCommandType.WinKey
        };
    }
}
