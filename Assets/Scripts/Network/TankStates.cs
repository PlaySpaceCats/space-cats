using System.IO;

public struct TankStates : IMessage
{
    public struct TankState
    {
        public float moveX;
        public float moveY;
        public bool fire;
        public float fireX;
        public float fireY;
        public bool respawned;
        public float? rotation;
        public string publicKey;

        public override string ToString()
        {
            return $"{nameof(moveX)}: {moveX}, {nameof(moveY)}: {moveY}, {nameof(fire)}: {fire}, {nameof(fireX)}: {fireX}, {nameof(fireY)}: {fireY}, {nameof(respawned)}: {respawned}, {nameof(rotation)}: {rotation}, {nameof(publicKey)}: {publicKey}";
        }
    }

    public MessageType Type;

    public TankState[] States;

    public byte[] ToByteArray()
    {
        byte[] data;
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte) Type);
                writer.Write((byte) States.Length);
                foreach (var state in States)
                {
                    writer.Write(state.moveX);
                    writer.Write(state.moveY);
                    writer.Write(state.fire);
                    writer.Write(state.fireX);
                    writer.Write(state.fireY);
                    writer.Write(state.respawned);
                    if (state.respawned)
                    {
                        writer.Write(state.rotation.Value);
                    }
                    if (Type == MessageType.InitialTankStates)
                    {
                        writer.Write(state.publicKey);
                    }
                }
                data = stream.ToArray();
            }
        }

        return data;
    }

    public static TankStates FromByteArray(byte[] data)
    {
        using (var stream = new MemoryStream(data))
        {
            using (var reader = new BinaryReader(stream))
            {
                var type = (MessageType) reader.ReadByte();
                var length = reader.ReadByte();
                var states = new TankState[length];
                for (var i = 0; i < length; i++)
                {
                    states[i] = new TankState
                    {
                        moveX = reader.ReadSingle(),
                        moveY = reader.ReadSingle(),
                        fire = reader.ReadBoolean(),
                        fireX = reader.ReadSingle(),
                        fireY = reader.ReadSingle(),
                        respawned = reader.ReadBoolean()
                    };

                    if (states[i].respawned)
                    {
                        states[i].rotation = reader.ReadSingle();
                    }

                    if (type == MessageType.InitialTankStates)
                    {
                        states[i].publicKey = reader.ReadString();
                    }
                }

                return new TankStates
                {
                    Type = type,
                    States = states
                };
            }
        }
    }

    public override string ToString()
    {
        return $"{nameof(Type)}: {Type}, {nameof(States)}: {States}";
    }
}