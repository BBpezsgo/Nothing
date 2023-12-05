using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

#nullable enable

namespace Networking
{
    using Messages;

    public class NetcodeMessaging
    {
        static NetworkManager NetworkManager => NetworkManager.Singleton;
        public const int MessageSize = 1100;
        public const NetworkDelivery DefaultNetworkDelivery = NetworkDelivery.ReliableFragmentedSequenced;
        static bool Logs => NetcodeSynchronizer.Instance!.Logs;

        public static BaseMessage DeserializeMessage(ulong clientId, FastBufferReader reader)
        {
            BaseMessage baseMessage = new(MessageHeader.Unknown);
            if (!reader.TryBeginRead(MessageHeader.Size))
            { throw new Exception($"Tried to read {MessageHeader.Size} bytes from buffer with size of {reader.Length} bytes from position {reader.Position}"); }
            baseMessage.Deserialize(reader);
            switch (baseMessage.Type)
            {
                case MessageType.USER_DATA_REQUEST:
                    {
                        UserDataRequestHeader header = new(new MessageHeader(baseMessage.Type, baseMessage.Sender));
                        header.Deserialize(reader);
                        return header;
                    }

                case MessageType.USER_DATA:
                    {
                        UserDataHeader header = new(new MessageHeader(baseMessage.Type, clientId));
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

        public static BaseMessage[] ReceiveUnnamedMessage(ulong clientId, FastBufferReader reader)
        {
            string senderName = ((clientId == NetworkManager.ServerClientId) ? $"server" : $"client {clientId}");
            if (Logs) Debug.Log($"[{nameof(NetcodeMessaging)}]: Received {reader.Length} bytes from {senderName}");
            int endlessSafe = 5000;
            List<BaseMessage> messages = new();
            while (reader.TryBeginRead(MessageHeader.Size))
            {
                if (endlessSafe-- <= 0) { Debug.LogError($"[{nameof(NetcodeMessaging)}]: Endless loop!"); break; }

                try
                {
                    var message = DeserializeMessage(clientId, reader);
                    messages.Add(message);
                }
                catch (Exception ex)
                { Debug.LogException(ex); }
            }
            return messages.ToArray();
        }

        public static void BroadcastUnnamedMessage(INetworkSerializable data, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
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

        public static void SendUnnamedMessage(INetworkSerializable data, ulong destination, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
        {
            using FastBufferWriter writer = new(MessageSize, Allocator.Temp);
            if (data is BaseMessage message)
            {
                if (!writer.TryBeginWrite(message.Size()))
                { throw new OverflowException($"Not enough space in the buffer (Available: {writer.Capacity}) (Required: {message.Size()})"); }
                string destinationName = ((destination == NetworkManager.ServerClientId) ? $"server" : $"client {destination}");
                if (Logs) Debug.Log(
                    $"[{nameof(NetcodeMessaging)}]: Sending message {message.Type} to {destinationName} " +
                    $"{{\n" +
                    $"{message}" +
                    $"}}"
                    );
            }
            data.Serialize(writer);
            SendUnnamedMessage(writer, destination, networkDelivery);
        }

        public static void BroadcastUnnamedMessage(FastBufferWriter writer, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
        {
            if (!NetworkManager.IsServer)
            {
                Debug.LogError($"[{nameof(NetcodeMessaging)}]: Client can not broadcast message");
                return;
            }
            NetworkManager.CustomMessagingManager.SendUnnamedMessageToAll(writer, networkDelivery);
            if (Logs) Debug.Log($"[{nameof(NetcodeMessaging)}]: Broadcasted {writer.Length} bytes");
        }

        public static void SendUnnamedMessage(FastBufferWriter writer, ulong destination, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
        {
            NetworkManager.CustomMessagingManager.SendUnnamedMessage(destination, writer, networkDelivery);
            if (Logs) Debug.Log($"[{nameof(NetcodeMessaging)}]: Sent {writer.Length} bytes to client {destination}");
        }
    }
}

namespace Networking.Messages
{
    public enum MessageType : byte
    {
        UNKNOWN,

        USER_DATA_REQUEST,
        USER_DATA_REQUEST_DIRECT,
        USER_DATA,
    }

    public struct MessageHeader : INetworkSerializable
    {
        public MessageType Type;
        public uint Sender;
        public TimeSpan Sent;

        public const int Size =
            sizeof(byte) +
            sizeof(uint) +
            sizeof(long);

        public static MessageHeader Unknown => new(MessageType.UNKNOWN, uint.MaxValue, TimeSpan.Zero);

        public MessageHeader(MessageType type, uint sender) : this(type, sender, DateTime.UtcNow.TimeOfDay)
        { }

        public MessageHeader(MessageType type, ulong sender) : this(type, (uint)sender, DateTime.UtcNow.TimeOfDay)
        { }

        public MessageHeader(MessageType type, ulong sender, TimeSpan sent) : this(type, (uint)sender, sent)
        { }

        public MessageHeader(MessageType type, uint sender, TimeSpan sent)
        {
            Type = type;
            Sender = sender;
            Sent = sent;
        }

        public void Deserialize(FastBufferReader deserializer)
        {
            deserializer.ReadValueSafe(out byte type);
            Type = (MessageType)type;
            deserializer.ReadValueSafe(out Sender);
            deserializer.ReadValueSafe(out long ticks);
            Sent = new TimeSpan(ticks);
        }

        public void Serialize(FastBufferWriter serializer)
        {
            serializer.WriteValueSafe((byte)Type);
            serializer.WriteValueSafe(Sender);
            serializer.WriteValueSafe(Sent.Ticks);
        }
    }

    public class BaseMessage : INetworkSerializable, ISizeOf
    {
        public MessageType Type => Header.Type;
        public byte TypeRaw => (byte)Header.Type;
        public uint Sender => Header.Sender;

        MessageHeader Header;

        public BaseMessage(MessageHeader header)
        {
            header.Sent = DateTime.UtcNow.TimeOfDay;
            Header = header;
        }

        public virtual int Size()
        {
            return sizeof(byte) +
                   sizeof(uint) +
                   sizeof(long);
        }

        public virtual void Deserialize(FastBufferReader deserializer)
        {
            Header.Deserialize(deserializer);
        }

        public virtual void Serialize(FastBufferWriter serializer)
        {
            Header.Serialize(serializer);
        }

        public override string ToString() =>
            $"ObjType: \"{GetType().Name}\"\n";
    }

    public class EmptyHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        public EmptyHeader(MessageHeader header) : base(header) { }
        public override void Deserialize(FastBufferReader deserializer) { }
        public override void Serialize(FastBufferWriter serializer) => base.Serialize(serializer);
    }
    public class UserDataHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        public string UserName;
        public string ID;

        public UserDataHeader(MessageHeader header) : base(header)
        {
            UserName = string.Empty;
            ID = string.Empty;
        }

        public override int Size() =>
            base.Size() +
            sizeof(uint) +
            System.Text.Encoding.Unicode.GetByteCount(UserName) +
            sizeof(uint) +
            System.Text.Encoding.Unicode.GetByteCount(ID);

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
    public class UserDataRequestHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        public string ID;

        public UserDataRequestHeader(MessageHeader header) : base(header)
        {
            ID = string.Empty;
        }

        public override int Size() =>
            base.Size() +
            sizeof(uint) +
            System.Text.Encoding.Unicode.GetByteCount(ID);

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

    public interface INetworkSerializable
    {
        void Deserialize(FastBufferReader reader);
        void Serialize(FastBufferWriter writer);
    }

    interface ISizeOf
    {
        int Size();
    }
}
