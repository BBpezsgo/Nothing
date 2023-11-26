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
        internal const int MessageSize = 1100;
        internal const NetworkDelivery DefaultNetworkDelivery = NetworkDelivery.ReliableFragmentedSequenced;
        static bool Logs => NetcodeSynchronizer.Instance.Logs;

        internal static BaseMessage DeserializeMessage(ulong clientId, FastBufferReader reader)
        {
            BaseMessage baseMessage = new(MessageType.UNKNOWN, ulong.MaxValue);
            if (!reader.TryBeginRead(BaseMessage.HeaderSize))
            { throw new Exception($"Tried to read {BaseMessage.HeaderSize} bytes from buffer with size of {reader.Length} bytes from position {reader.Position}"); }
            baseMessage.Deserialize(reader);
            switch (baseMessage.Type)
            {
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

        internal static BaseMessage[] ReceiveUnnamedMessage(ulong clientId, FastBufferReader reader)
        {
            string senderName = ((clientId == NetworkManager.ServerClientId) ? $"server" : $"client {clientId}");
            if (Logs) Debug.Log($"[{nameof(NetcodeMessaging)}]: Received {reader.Length} bytes from {senderName}");
            int endlessSafe = 5000;
            List<BaseMessage> result = new();
            while (reader.TryBeginRead(BaseMessage.HeaderSize))
            {
                if (endlessSafe-- <= 0) { Debug.LogError($"[{nameof(NetcodeMessaging)}]: Endless loop!"); break; }

                try
                {
                    var message = DeserializeMessage(clientId, reader);
                    result.Add(message);
                }
                catch (Exception ex)
                { Debug.LogException(ex); }
            }
            return result.ToArray();
        }

        internal static void BroadcastUnnamedMessage(INetworkSerializable data, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
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

        internal static void SendUnnamedMessage(INetworkSerializable data, ulong destination, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
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

        internal static void BroadcastUnnamedMessage(FastBufferWriter writer, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
        {
            if (!NetworkManager.IsServer)
            {
                Debug.LogError($"[{nameof(NetcodeMessaging)}]: Client can not broadcast message");
                return;
            }
            NetworkManager.CustomMessagingManager.SendUnnamedMessageToAll(writer, networkDelivery);
            if (Logs) Debug.Log($"[{nameof(NetcodeMessaging)}]: Broadcasted {writer.Length} bytes");
        }

        internal static void SendUnnamedMessage(FastBufferWriter writer, ulong destination, NetworkDelivery networkDelivery = DefaultNetworkDelivery)
        {
            NetworkManager.CustomMessagingManager.SendUnnamedMessage(destination, writer, networkDelivery);
            if (Logs) Debug.Log($"[{nameof(NetcodeMessaging)}]: Sent {writer.Length} bytes to client {destination}");
        }
    }
}

namespace Networking.Messages
{
    internal enum MessageType : byte
    {
        UNKNOWN,

        USER_DATA_REQUEST,
        USER_DATA_REQUEST_DIRECT,
        USER_DATA,
    }

    internal class BaseMessage : INetworkSerializable, ISizeOf
    {
        internal MessageType Type => (MessageType)_type;
        internal byte TypeRaw => _type;
        internal uint Sender => _sender;

        byte _type;
        uint _sender;
        TimeSpan _sent;

        public BaseMessage(MessageType type, ulong sender)
        {
            _type = (byte)type;
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

    internal class EmptyHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        public EmptyHeader(MessageType type, ulong sender) : base(type, sender) { }
        public override void Deserialize(FastBufferReader deserializer) { }
        public override void Serialize(FastBufferWriter serializer) => base.Serialize(serializer);
    }
    internal class UserDataHeader : BaseMessage, INetworkSerializable, ISizeOf
    {
        internal string? UserName;
        internal string? ID;

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
        internal string? ID;

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
