using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace VN.Data
{
    public static class MasterDataBinarySerializer
    {
        private const string Magic = "MDB1";
        private const int Version = 1;

        [Serializable]
        private sealed class MasterDataEnvelope
        {
            public int version;
            public MasterDataTable table;
        }

        public static byte[] Serialize(MasterDataTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var envelope = new MasterDataEnvelope
            {
                version = Version,
                table = table
            };

            var json = JsonUtility.ToJson(envelope, false);
            var payload = Encoding.UTF8.GetBytes(json);

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(payload.Length);
            writer.Write(payload);
            writer.Flush();

            return stream.ToArray();
        }

        public static MasterDataTable Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new InvalidDataException("MasterData bytes are empty.");
            }

            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream, Encoding.UTF8, true);

            var magic = reader.ReadString();
            if (!string.Equals(magic, Magic, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Invalid MasterData magic header: {magic}");
            }

            var version = reader.ReadInt32();
            if (version != Version)
            {
                throw new InvalidDataException($"Unsupported MasterData version: {version}");
            }

            var payloadLength = reader.ReadInt32();
            if (payloadLength <= 0)
            {
                throw new InvalidDataException("Invalid MasterData payload length.");
            }

            var payload = reader.ReadBytes(payloadLength);
            if (payload.Length != payloadLength)
            {
                throw new EndOfStreamException("MasterData payload is truncated.");
            }

            var json = Encoding.UTF8.GetString(payload);
            var envelope = JsonUtility.FromJson<MasterDataEnvelope>(json);
            if (envelope?.table == null)
            {
                throw new InvalidDataException("Failed to parse MasterData payload.");
            }

            return envelope.table;
        }
    }
}