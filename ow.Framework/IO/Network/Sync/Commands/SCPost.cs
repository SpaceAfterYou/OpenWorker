namespace ow.Framework.IO.Network.Sync.Commands
{
    public enum SCPost : byte
    {
        SendList = 0x1,
        RecvList = 0x2,
        Send = 0x3,
        Read = 0x4,
        Receipt = 0x5,
        SendDel = 0x6,
        RecvDel = 0x7,
        Sendback = 0x8,
        Recv = 0x9,
        Info = 0x10,
        SendNameCheck = 0x11,
        Save = 0x12,
        SaveList = 0x13,
        AccountList = 0x14,
        AccountRecv = 0x15,
        AccountRead = 0x16,
        AccountReceipt = 0x17,
        AccountDel = 0x18,
        DeleteAll = 0x19,
        ReceiptAll = 0x20,
        ListRefresh = 0x21,
        End = 0x80
    }
}