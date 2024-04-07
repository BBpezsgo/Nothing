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
        public const int MessageSize = 1100;
        public const NetworkDelivery DefaultNetworkDelivery = NetworkDelivery.ReliableFragmentedSequenced;

        static NetworkManager NetworkManager => NetworkManager.Singleton;
        static bool Logs => NetcodeSynchronizer.Instance!.Logs;

        public static BaseMessage DeserializeMessage(ulong clientId, FastBufferReader reader)
        {
            MessageHeader header = new(reader);
            return header.Type switch
            {
                MessageType.UserDataRequest => new UserDataRequestHeader(new MessageHeader(header.Type, header.Sender), reader),
                MessageType.UserDataResponse => new UserDataHeader(new MessageHeader(header.Type, clientId), reader),
                MessageType.UserDataRequestDirect => new BaseMessage(header),
                _ => throw new Exception($"[{nameof(NetcodeMessaging)}]: Unknown message type {header.Type} form client {header.Sender}"),
            };
        }

        public static IEnumerable<BaseMessage> ReceiveUnnamedMessage(ulong clientId, FastBufferReader reader)
        {
            string senderName = (clientId == NetworkManager.ServerClientId) ? $"server" : $"client {clientId}";
            if (Logs) Debug.Log($"[{nameof(NetcodeMessaging)}]: Received {reader.Length} bytes from {senderName}");
            int endlessSafe = 5000;
            while (reader.TryBeginRead(MessageHeader.Size))
            {
                if (endlessSafe-- <= 0) { Debug.LogError($"[{nameof(NetcodeMessaging)}]: Endless loop!"); break; }

                BaseMessage message;

                try
                { message = DeserializeMessage(clientId, reader); }
                catch (Exception ex)
                { Debug.LogException(ex); continue; }

                yield return message;
            }
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
                if (Logs)
                { Debug.Log($"[{nameof(NetcodeMessaging)}]: Sending message {message.Type} to {destinationName} {{\n{message}\n}}"); }
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
        Unknown,

        UserDataRequest,
        UserDataRequestDirect,
        UserDataResponse,
    }

    public struct MessageHeader : INetworkSerializable
    {
        public static MessageHeader Unknown => new(MessageType.Unknown, ulong.MaxValue, TimeSpan.Zero);

        public const int Size =
            sizeof(byte) +
            sizeof(ulong) +
            sizeof(long);

        public MessageType Type;
        public ulong Sender;
        public TimeSpan Sent;

        public MessageHeader(FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte type);
            Type = (MessageType)type;
            reader.ReadValueSafe(out Sender);
            reader.ReadValueSafe(out long ticks);
            Sent = new TimeSpan(ticks);
        }

        public MessageHeader(MessageType type, ulong sender) : this(type, sender, DateTime.UtcNow.TimeOfDay)
        { }

        public MessageHeader(MessageType type, ulong sender, TimeSpan sent)
        {
            Type = type;
            Sender = sender;
            Sent = sent;
        }

        public readonly void Serialize(FastBufferWriter serializer)
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
        public ulong Sender => Header.Sender;

        MessageHeader Header;

        public BaseMessage(MessageHeader header)
        {
            Header = new MessageHeader(header.Type, header.Sender, DateTime.UtcNow.TimeOfDay);
        }

        public BaseMessage(FastBufferReader reader)
        {
            if (!reader.TryBeginRead(MessageHeader.Size))
            { throw new Exception($"Tried to read {MessageHeader.Size} bytes from buffer with size of {reader.Length} bytes from position {reader.Position}"); }
            Header = new MessageHeader(reader);
        }

        public virtual int Size()
        {
            return sizeof(byte) +
                   sizeof(uint) +
                   sizeof(long);
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

        public UserDataHeader(MessageHeader header, FastBufferReader reader) : base(header)
        {
            reader.ReadValueSafe(out UserName);
            reader.ReadValueSafe(out ID);
        }

        public override int Size() =>
            base.Size() +
            sizeof(uint) +
            System.Text.Encoding.Unicode.GetByteCount(UserName) +
            sizeof(uint) +
            System.Text.Encoding.Unicode.GetByteCount(ID);

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

        public UserDataRequestHeader(MessageHeader header, FastBufferReader reader) : base(header)
        {
            reader.ReadValueSafe(out ID);
        }

        public override int Size() =>
            base.Size() +
            sizeof(uint) +
            System.Text.Encoding.Unicode.GetByteCount(ID);

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
        void Serialize(FastBufferWriter writer);
    }

    interface ISizeOf
    {
        int Size();
    }
}
