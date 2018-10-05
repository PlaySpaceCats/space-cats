using System.IO;

public struct MovementState : IMessage
{
    public float moveX;
    public float moveY;
    public bool fire;
    public float fireX;
    public float fireY;

    public byte[] ToByteArray()
    {
        byte[] data;
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte) MessageType.MovementState);
                writer.Write(moveX);
                writer.Write(moveY);
                writer.Write(fire);
                writer.Write(fireX);
                writer.Write(fireY);
                data = stream.ToArray();
            }
        }

        return data;
    }

    public static MovementState FromByteArray(byte[] data)
    {
        var state = new MovementState();
        using (var stream = new MemoryStream(data))
        {
            using (var reader = new BinaryReader(stream))
            {
                reader.ReadByte(); // skip
                state.moveX = reader.ReadSingle();
                state.moveY = reader.ReadSingle();
                state.fire = reader.ReadBoolean();
                state.fireX = reader.ReadSingle();
                state.fireY = reader.ReadSingle();
            }
        }

        return state;
    }

    public override string ToString()
    {
        return $"{nameof(MovementState)}({nameof(moveX)}: {moveX}, {nameof(moveY)}: {moveY}, {nameof(fire)}: {fire}, {nameof(fireX)}: {fireX}, {nameof(fireY)}: {fireY})";
    }
}