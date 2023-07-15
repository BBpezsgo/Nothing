using Messages;

using System;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Netcode;

using UnityEngine;

public class NetcodeMessaging
{
    static NetworkManager NetworkManager => NetworkManager.Singleton;
    internal const int MessageSize = 1100;
    internal const NetworkDelivery DefaultNetworkDelivery = NetworkDelivery.ReliableFragmentedSequenced;

    internal static BaseMessage DeserializeMessage(ulong clientId, FastBufferReader reader)
    {
        BaseMessage baseMessage = new(MessageType.UNKNOWN, ulong.MaxValue);
        if (!reader.TryBeginRead(BaseMessage.HeaderSize))
        { throw new Exception($"Tried to read {BaseMessage.HeaderSize} bytes from buffer with size of {reader.Length} bytes from position {reader.Position}"); }
        baseMessage.Deserialize(reader);
        switch (baseMessage.Type)
        {
            case MessageType.SYNC:
                {
                    ComponentHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    return header;
                }
            case MessageType.UNKNOWN_OBJECT:
                {
                    ObjectHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    return header;
                }

            case MessageType.SPAWN_OBJECT:
                {
                    InstantiationHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    if (NetworkManager.IsServer)
                    { Debug.LogWarning($"[{nameof(NetcodeMessaging)}]: Server got a '{baseMessage.Type}' message"); }
                    return header;
                }
            case MessageType.GET_RATE:
                return baseMessage;
            case MessageType.RATE:
                {
                    LiteralHeader<byte> header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    if (NetworkManager.IsServer)
                    { Debug.LogWarning($"[{nameof(NetcodeMessaging)}]: Server got a '{baseMessage.Type}' message"); }
                    return header;
                }

            case MessageType.DESTROY_OBJECT:
                {
                    ObjectHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    return header;
                }
            case MessageType.RPC:
                {
                    RpcHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    return header;
                }

            case MessageType.GET_SCENE:
                return baseMessage;
            case MessageType.SCENE:
                {
                    StringHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    return header;
                }

            case MessageType.CHUNK:
                {
                    ChunkHeader header = new(baseMessage.Type, clientId);
                    if (!reader.TryBeginRead(header.Size()))
                    { throw new OverflowException($"Tried to read {header.Size()} bytes from buffer with size of {reader.Length} bytes"); }
                    header.Deserialize(reader);
                    return header;
                }
            case MessageType.CHUNK_ACK:
                {
                    EmptyChunkHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    return header;
                }

            case MessageType.SCENE_LOADED:
                return baseMessage;
            case MessageType.REQUEST:
                {
                    RequestHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    return header;
                }

            case MessageType.USER_DATA_REQUEST:
                {
                    UserDataRequestHeader header = new(baseMessage.Type, baseMessage.Sender);
                    header.Deserialize(reader);
                    return header;
                }

            case MessageType.USER_DATA:
                {
                    UserDataHeader header = new(baseMessage.Type, clientId);
                    header.Deserialize(reader);
                    return header;
                }

            case MessageType.USER_DATA_REQUEST_DIRECT:
                return baseMessage;
            case MessageType.UNKNOWN:
            default:
                throw new Exception($"[{nameof(NetcodeMessaging)}]: Unknown message type {baseMessage.Type}({baseMessage.TypeRaw}) form client {baseMessage.Sender}");
        }
    }

    internal static BaseMessage[] ReciveUnnamedMessage(ulong clientId, FastBufferReader reader)
    {
        string senderName = ((clientId == NetworkManager.ServerClientId) ? $"server" : $"client {clientId}");
        Debug.Log($"[{nameof(NetcodeMessaging)}]: Recived {reader.Length} bytes from {senderName}");
        int endlessSafe = 5000;
        List<BaseMessage> result = new();
        while (reader.TryBeginRead(BaseMessage.HeaderSize))
        {
            if (endlessSafe-- <= 0) { Debug.LogError($"[{nameof(NetcodeMessaging)}]: Endless loop!"); break; }

            try
            {
                var message = DeserializeMessage(clientId, reader);
                if (message.Type != MessageType.SYNC)
                {
                    Debug.Log(
                    $"[{nameof(NetcodeMessaging)}]: Recived message {message.Type} from {senderName} " +
                    $"{{\n" +
                    $"{message.ToString()}" +
                    $"}}"
                    );
                }
                result.Add(message);
            }
            catch (Exception ex)
            { Debug.LogException(ex); }
        }
        return result.ToArray();
    }

    internal static void BroadcastUnnamedMessage(Messages.INetworkSerializable data, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
    {
        if (!NetworkManager.IsServer)
        {
            Debug.LogError($"[{nameof(NetcodeMessaging)}]: Client can not broadcast message");
            return;
        }
        using FastBufferWriter writer = new(MessageSize, Allocator.Temp);
        data.Serialize(writer);
        BroadcastUnnamedMessage(writer, networkDelivery);
    }

    internal static void SendUnnamedMessage(Messages.INetworkSerializable data, ulong destination, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
    {
        using FastBufferWriter writer = new(MessageSize, Allocator.Temp);
        if (data is BaseMessage message)
        {
            if (!writer.TryBeginWrite(message.Size()))
            { throw new OverflowException($"Not enough space in the buffer (Avaliable: {writer.Capacity}) (Requied: {message.Size()})"); }
            string destinationName = ((destination == NetworkManager.ServerClientId) ? $"server" : $"client {destination}");
            Debug.Log(
                $"[{nameof(NetcodeMessaging)}]: Sending message {message.Type} to {destinationName} " +
                $"{{\n" +
                $"{message.ToString()}" +
                $"}}"
                );
        }
        data.Serialize(writer);
        SendUnnamedMessage(writer, destination, networkDelivery);
    }

    internal static void BroadcastUnnamedMessage(FastBufferWriter writer, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
    {
        if (!NetworkManager.IsServer)
        {
            Debug.LogError($"[{nameof(NetcodeMessaging)}]: Client can not broadcast message");
            return;
        }
        NetworkManager.CustomMessagingManager.SendUnnamedMessageToAll(writer, networkDelivery);
        Debug.Log($"[{nameof(NetcodeMessaging)}]: Broadcasted {writer.Length} bytes");
    }

    internal static void SendUnnamedMessage(FastBufferWriter writer, ulong destination, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
    {
        NetworkManager.CustomMessagingManager.SendUnnamedMessage(destination, writer, networkDelivery);
        Debug.Log($"[{nameof(NetcodeMessaging)}]: Sent {writer.Length} bytes to client {destination}");
    }
}

public readonly struct NetcodeMessageInfo
{
    public bool IsReading => !_isWriting;
    public bool IsWriting => _isWriting;
    public TimeSpan Sent => _sent;
    public TimeSpan Received => _received;

    readonly bool _isWriting;
    readonly TimeSpan _sent;
    readonly TimeSpan _received;

    public NetcodeMessageInfo(bool isWriting, TimeSpan timeSpan)
    {
        _isWriting = isWriting;
        _sent = timeSpan;
        _received = DateTime.UtcNow.TimeOfDay;
    }
}

namespace Messages
{
    internal enum MessageType : byte
    {
        UNKNOWN,
        /// <summary>
        /// It is used to synchronize components of network objects.<br/>
        /// Used by <see cref="ComponentHeader"/> message.<br/>
        /// Sended by the <b>Host</b>
        /// </summary>
        SYNC,
        /// <summary>
        /// Used by <see cref="ObjectHeader"/> message.<br/>
        /// Sended by the <b>Client</b>
        /// </summary>
        UNKNOWN_OBJECT,
        /// <summary>
        /// Used by <see cref="InstantiationHeader"/> message.<br/>
        /// Sended by the <b>Host</b>
        /// </summary>
        SPAWN_OBJECT,
        /// <summary>
        /// Sended by the <b>Host</b>
        /// </summary>
        DESTROY_OBJECT,
        /// <summary>
        /// Sended by the <b>Client</b>
        /// </summary>
        GET_RATE,
        /// <summary>
        /// Used by <see cref="LiteralHeader{byte}"/> message.<br/>
        /// Sended by the <b>Client</b>
        /// </summary>
        RATE,
        /// <summary>
        /// Used by <see cref="RpcHeader"/> message.<br/>
        /// Sended by the <b>Host</b> or <b>Client</b>
        /// </summary>
        RPC,
        GET_SCENE,
        SCENE,
        SCENE_LOADED,

        CHUNK,
        CHUNK_ACK,

        REQUEST,

        USER_DATA_REQUEST,
        USER_DATA_REQUEST_DIRECT,
        USER_DATA,
    }

    internal class BaseMessage : INetworkSerializable, ISizeOf
    {
        internal MessageType Type => (MessageType)_type;
        internal byte TypeRaw => _type;
        internal uint Sender => _sender;
        /// <summary>
        /// <b>UTC</b>
        /// </summary>
        internal System.TimeSpan Sent => _sent;

        byte _type;
        uint _sender;
        TimeSpan _sent;

        public BaseMessage(MessageType type, ulong sender)
        {
            _type = (byte)type;
            _sender = (uint)sender;
            _sent = DateTime.UtcNow.TimeOfDay;
        }
        public BaseMessage(byte type, ulong sender)
        {
            _type = type;
            _sender = (uint)sender;
            _sent = DateTime.UtcNow.TimeOfDay;
        }

        public const int HeaderSize =
            sizeof(byte) +
            sizeof(uint) +
            sizeof(long);

        public virtual int Size()
        {
            return sizeof(byte) +
                   sizeof(uint) +
                   sizeof(long);
        }

        public virtual void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out _type);
            deserializer.ReadValueSafe(out _sender);
            deserializer.ReadValueSafe(out long ticks);
            _sent = new TimeSpan(ticks);
        }

        public virtual void Serialize(FastBufferWriter serializer)
        {
            serializer.WriteValueSafe(_type);
            serializer.WriteValueSafe(_sender);
            serializer.WriteValueSafe(_sent.Ticks);
        }

        public override string ToString() =>
            $"ObjType: \"{GetType().Name}\"\n";
    }

    internal class InstantiationHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        internal string PrefabName;
        internal uint NetworkID;
        internal Vector3 Position;

        public override int Size()
        {
            return base.Size() +
                sizeof(float) +
                sizeof(float) +
                sizeof(float) +
                sizeof(uint) +
                sizeof(uint) +
                System.Text.ASCIIEncoding.Unicode.GetByteCount(PrefabName ?? "");
        }

        public InstantiationHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out PrefabName);
            deserializer.ReadValueSafe(out NetworkID);
            deserializer.ReadValueSafe(out float x);
            deserializer.ReadValueSafe(out float y);
            deserializer.ReadValueSafe(out float z);
            Position = new Vector3(x, y, z);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(PrefabName);
            serializer.WriteValueSafe(NetworkID);
            serializer.WriteValueSafe(Position.x);
            serializer.WriteValueSafe(Position.y);
            serializer.WriteValueSafe(Position.z);
        }

        public override string ToString() => base.ToString() +
            $"Prefab: \"{PrefabName}\"\n" +
            $"NetworkID: {NetworkID}\n" +
            $"Position: {Position}\n";
    }
    internal class ObjectHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        internal uint ObjectID;

        public ObjectHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size()
        {
            return base.Size() +
                   sizeof(uint);
        }

        public override void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out ObjectID);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(ObjectID);
        }

        public override string ToString() => base.ToString() +
            $"ObjectID: {ObjectID}\n";
    }
    internal class ComponentHeader : ObjectHeader, INetworkSerializable, ISizeOf
    {
        internal byte ComponentIndex;
        internal byte[] Data;

        public ComponentHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size()
        {
            return base.Size() +
                   sizeof(byte) +
                   sizeof(int) +
                   sizeof(byte) * Data.Length;
        }

        public override void Deserialize(FastBufferReader deserializer)
        {
            base.Deserialize(deserializer);
            deserializer.ReadValueSafe(out ComponentIndex);
            deserializer.ReadValueSafe(out Data);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(ComponentIndex);
            serializer.WriteValueSafe(Data);
        }
    }
    internal class LiteralHeader<T> : BaseMessage, INetworkSerializable, ISizeOf where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>
    {
        internal T Value;

        public LiteralHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size()
        {
            return base.Size() +
                   System.Runtime.InteropServices.Marshal.SizeOf<T>();
        }

        public override void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe<T>(out Value);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe<T>(Value);
        }

        public override string ToString() => base.ToString() +
            $"Value: \"{Value}\"\n";
    }
    internal class StringHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        internal string Value;

        public StringHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size()
        {
            return base.Size() +
                   Value.Length +
                   sizeof(uint);
        }

        public override void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out Value, true);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(Value, true);
        }

        public override string ToString() => base.ToString() +
            $"Value: \"{Value}\"\n";
    }
    internal class EmptyHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        public EmptyHeader(MessageType type, ulong sender) : base(type, sender) { }
        public override void Deserialize(FastBufferReader deserializer) { }
        public override void Serialize(FastBufferWriter serializer) => base.Serialize(serializer);
    }
    internal class ChunkHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        internal byte[] Chunk;
        /// <summary>
        /// Request's id
        /// </summary>
        internal ulong ID;
        /// <summary>
        /// Chunk's index
        /// </summary>
        internal ulong SerialNumber;
        /// <summary>
        /// In bytes
        /// </summary>
        internal int TotalSize;
        internal int ChunkSize;

        public ChunkHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size()
        {
            return base.Size() +
                   (Chunk == null ? 0 : Chunk.Length) +
                   sizeof(int) +
                   sizeof(ulong) +
                   sizeof(ulong) +
                   sizeof(int) +
                   sizeof(int);
        }

        public override void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out Chunk);
            deserializer.ReadValueSafe(out ID);
            deserializer.ReadValueSafe(out SerialNumber);
            deserializer.ReadValueSafe(out TotalSize);
            deserializer.ReadValueSafe(out ChunkSize);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(Chunk);
            serializer.WriteValueSafe(ID);
            serializer.WriteValueSafe(SerialNumber);
            serializer.WriteValueSafe(TotalSize);
            serializer.WriteValueSafe(ChunkSize);
        }

        public override string ToString() => base.ToString() +
            $"ID: {ID}\n" +
            $"SerialNumber: {SerialNumber}\n" +
            $"ChunkSize: {ChunkSize}\n" +
            $"TotalSize: {TotalSize} bytes\n" +
            $"Chunk: {Chunk.Length} bytes\n";
    }
    internal class EmptyChunkHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        internal ulong ID;
        internal ulong SerialNumber;

        public EmptyChunkHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size()
        {
            return base.Size() +
                   sizeof(ulong) +
                   sizeof(ulong);
        }

        public override void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out ID);
            deserializer.ReadValueSafe(out SerialNumber);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(ID);
            serializer.WriteValueSafe(SerialNumber);
        }

        public override string ToString() => base.ToString() +
            $"ID: {ID}\n" +
            $"SerialNumber: {SerialNumber}\n";
    }
    internal class RequestHeader : StringHeader, INetworkSerializable, ISizeOf
    {
        internal ulong ID;

        public RequestHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size()
        {
            return base.Size() +
                   sizeof(ulong);
        }

        public override void Deserialize(FastBufferReader deserializer)
        {
            base.Deserialize(deserializer);
            deserializer.ReadValueSafe(out ID);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(ID);
        }

        public override string ToString() => base.ToString() +
            $"ID: {ID}\n";
    }
    internal class UserDataHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        internal string UserName;
        internal string ID;

        public UserDataHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size() =>
            base.Size() +
            sizeof(uint) +
            System.Text.ASCIIEncoding.Unicode.GetByteCount(UserName ?? "") +
            sizeof(uint) +
            System.Text.ASCIIEncoding.Unicode.GetByteCount(ID ?? "");

        public override void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out UserName);
            deserializer.ReadValueSafe(out ID);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(UserName);
            serializer.WriteValueSafe(ID);
        }

        public override string ToString() => base.ToString() +
            $"UserName: {UserName}\n" +
            $"ID: {ID}\n";
    }
    internal class UserDataRequestHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        internal string ID;

        public UserDataRequestHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override int Size() =>
            base.Size() +
            sizeof(uint) +
            System.Text.ASCIIEncoding.Unicode.GetByteCount(ID ?? "");

        public override void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out ID);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(ID);
        }

        public override string ToString() => base.ToString() +
            $"ID: {ID}\n";
    }

    internal class RpcHeader : ComponentHeader, INetworkSerializable, ISizeOf
    {
        public string MethodName;

        public override int Size()
        {
            return base.Size() +
                   System.Text.ASCIIEncoding.Unicode.GetByteCount(MethodName ?? "");
        }

        public RpcHeader(MessageType type, ulong sender) : base(type, sender) { }

        public override void Deserialize(FastBufferReader deserializer)
        {
            base.Deserialize(deserializer);
            deserializer.ReadValueSafe(out MethodName);
        }

        public override void Serialize(FastBufferWriter serializer)
        {
            base.Serialize(serializer);
            serializer.WriteValueSafe(MethodName);
        }

        public override string ToString() => base.ToString() +
            $"MethodName: \"{MethodName}\"\n";
    }

    interface INetworkSerializable
    {
        void Deserialize(FastBufferReader reader);
        void Serialize(FastBufferWriter writer);
    }

    interface ISizeOf
    {
        int Size();
    }
}

namespace Network
{
    class ChunkCollector
    {
        internal uint Sender;
        internal ulong ID;
        internal byte[] Data;
        internal int ExpectedSize;

        readonly List<ulong> ReceivedSerialNumbers;
        int ChunkSize;

        TimeSpan lastTransaction;
        int lastReceivedBytes;
        /// <summary>
        /// bytes per secs
        /// </summary>
        float speed;

        internal int TotalReceivedBytes => ReceivedSerialNumbers.Count * ChunkSize;

        internal float Speed => speed;
        internal float Progress
        {
            get
            {
                if (Data == null) return 0f;
                if (ExpectedSize == 0f) return 0f;
                return Mathf.Clamp01((float)TotalReceivedBytes / (float)ExpectedSize);
            }
        }
        internal bool ReceivedEverything => TotalReceivedBytes >= ExpectedSize;

        public ChunkCollector(uint sender, ulong iD, int expectedSize)
        {
            this.Sender = sender;
            this.ID = iD;
            this.ExpectedSize = expectedSize;
            this.ReceivedSerialNumbers = new List<ulong>();

            this.lastReceivedBytes = 0;
            this.lastTransaction = DateTime.Now.TimeOfDay;
        }

        internal void Receive(ChunkHeader message)
        {
            Data ??= new byte[message.TotalSize];
            Array.Copy(message.Chunk, 0, Data, (int)message.SerialNumber * message.ChunkSize, message.Chunk.Length);
            ReceivedSerialNumbers.Add(message.SerialNumber);
            ChunkSize = message.ChunkSize;

            int receivedBytesDifference = TotalReceivedBytes - lastReceivedBytes;
            double secsSinceLastTransition = DateTime.Now.TimeOfDay.TotalSeconds - lastTransaction.TotalSeconds;

            speed = (float)((double)receivedBytesDifference / secsSinceLastTransition);

            lastReceivedBytes = TotalReceivedBytes;
            lastTransaction = DateTime.Now.TimeOfDay;
        }
    }

    internal class ChunkCollectorManager
    {
        readonly List<ChunkCollector> list;

        public ChunkCollectorManager()
        {
            list = new List<ChunkCollector>();
        }

        internal byte[] Receive(ChunkHeader message)
            => Receive(message, out _);

        internal byte[] Receive(ChunkHeader message, out ChunkCollector chunkCollector)
        {
            chunkCollector = null;

            bool foundChunkCollector = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (message.ID != list[i].ID) continue;

                foundChunkCollector = true;
                break;
            }

            if (!foundChunkCollector)
            { list.Add(new ChunkCollector(message.Sender, message.ID, message.TotalSize)); }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (message.ID != list[i].ID) continue;

                list[i].Receive(message);
                chunkCollector = list[i];

                if (!list[i].ReceivedEverything) break;

                byte[] result = list[i].Data;
                list.RemoveAt(i);
                return result;
            }

            return null;
        }
    }
}