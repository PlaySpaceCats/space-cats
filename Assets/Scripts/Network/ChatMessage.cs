using System.IO;

public struct ChatMessage : IMessage
{
    public string Message;

    public ChatMessage(string message)
    {
        Message = message;
    }

    public byte[] ToByteArray()
    {
        byte[] data;
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte) MessageType.ChatMessage);
                writer.Write(Message);
                data = stream.ToArray();
            }
        }

        return data;
    }

    public static ChatMessage FromByteArray(byte[] data)
    {
        var message = new ChatMessage();
        using (var stream = new MemoryStream(data))
        {
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadByte(); // skip
                message.Message = reader.ReadString();
            }
        }

        return message;
    }

}