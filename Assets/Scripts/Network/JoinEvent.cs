public struct JoinEvent : IMessage
{
    public byte[] ToByteArray()
    {
        return new[] { (byte) MessageType.JoinEvent };
    }
}