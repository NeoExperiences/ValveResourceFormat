using System;
using System.IO;
using K4os.Compression.LZ4;
using ValveResourceFormat.Blocks;
using ValveResourceFormat.Serialization.KeyValues;

namespace ValveResourceFormat.ResourceTypes
{
    public class BinaryKV3 : ResourceData
    {
#pragma warning disable SA1310 // Field names should not contain underscore
        private static readonly Guid KV3_ENCODING_BINARY_BLOCK_COMPRESSED = new Guid(new byte[] { 0x46, 0x1A, 0x79, 0x95, 0xBC, 0x95, 0x6C, 0x4F, 0xA7, 0x0B, 0x05, 0xBC, 0xA1, 0xB7, 0xDF, 0xD2 });
        private static readonly Guid KV3_ENCODING_BINARY_UNCOMPRESSED = new Guid(new byte[] { 0x00, 0x05, 0x86, 0x1B, 0xD8, 0xF7, 0xC1, 0x40, 0xAD, 0x82, 0x75, 0xA4, 0x82, 0x67, 0xE7, 0x14 });
        private static readonly Guid KV3_ENCODING_BINARY_BLOCK_LZ4 = new Guid(new byte[] { 0x8A, 0x34, 0x47, 0x68, 0xA1, 0x63, 0x5C, 0x4F, 0xA1, 0x97, 0x53, 0x80, 0x6F, 0xD9, 0xB1, 0x19 });
        private static readonly Guid KV3_FORMAT_GENERIC = new Guid(new byte[] { 0x7C, 0x16, 0x12, 0x74, 0xE9, 0x06, 0x98, 0x46, 0xAF, 0xF2, 0xE6, 0x3E, 0xB5, 0x90, 0x37, 0xE7 });
        public const int MAGIC = 0x03564B56; // VKV3 (3 isn't ascii, its 0x03)
        public const int MAGIC2 = 0x4B563301; // KV3\x01
#pragma warning restore SA1310

        public KVObject Data { get; private set; }
        public Guid Encoding { get; private set; }
        public Guid Format { get; private set; }

        private string[] stringArray;

        public override void Read(BinaryReader reader, Resource resource)
        {
            reader.BaseStream.Position = Offset;
            var outStream = new MemoryStream();
            var outWrite = new BinaryWriter(outStream);
            var outRead = new BinaryReader(outStream); // Why why why why why why why

            var magic = reader.ReadUInt32();

            if (magic == MAGIC2)
            {
                ReadVersion2(reader, outWrite, outRead);

                return;
            }

            if (magic != MAGIC)
            {
                throw new InvalidDataException($"Invalid KV3 signature {magic}");
            }

            Encoding = new Guid(reader.ReadBytes(16));
            Format = new Guid(reader.ReadBytes(16));

            // Valve's implementation lives in LoadKV3Binary()
            // KV3_ENCODING_BINARY_BLOCK_COMPRESSED calls CBlockCompress::FastDecompress()
            // and then it proceeds to call LoadKV3BinaryUncompressed, which should be the same routine for KV3_ENCODING_BINARY_UNCOMPRESSED
            // Old binary with debug symbols for ref: https://users.alliedmods.net/~asherkin/public/bins/dota_symbols/bin/osx64/libmeshsystem.dylib

            if (Encoding.CompareTo(KV3_ENCODING_BINARY_BLOCK_COMPRESSED) == 0)
            {
                BlockDecompress(reader, outWrite, outRead);
            }
            else if (Encoding.CompareTo(KV3_ENCODING_BINARY_BLOCK_LZ4) == 0)
            {
                DecompressLZ4(reader, outWrite);
            }
            else if (Encoding.CompareTo(KV3_ENCODING_BINARY_UNCOMPRESSED) == 0)
            {
                reader.BaseStream.CopyTo(outStream);
                outStream.Position = 0;
            }
            else
            {
                throw new InvalidDataException($"Unrecognised KV3 Encoding: {Encoding.ToString()}");
            }

            var stringCount = outRead.ReadUInt32();
            stringArray = new string[stringCount];
            for (var i = 0; i < stringCount; i++)
            {
                stringArray[i] = outRead.ReadNullTermString(System.Text.Encoding.UTF8);
            }

            Data = ParseBinaryKV3(outRead, null, true);
        }

        private void ReadVersion2(BinaryReader reader, BinaryWriter outWrite, BinaryReader outRead)
        {
            Format = new Guid(reader.ReadBytes(16));

            reader.ReadInt32(); // appears to always be 1
            reader.ReadInt32(); // appears to always be 0
            reader.ReadInt32(); // ?
            reader.ReadInt32(); // ?

            DecompressLZ4(reader, outWrite);

            // this appears to the number of strings
            var count = outRead.ReadInt32();

            // values?

            stringArray = new string[count];

            for (var i = 0; i < count; i++)
            {
                stringArray[i] = outRead.ReadNullTermString(System.Text.Encoding.UTF8);
            }

            // data is now kvtype bytes

            // end is  00 DD EE FF

            // 1. First int in decompressed data appears to be count of strings
            // 2. probably list of values
            // 3. null terminated strings (as many as the first int specifies)
            // 4. the remaining data after last null byte is a list of KVType bytes
            // 00 DD EE FF ???
        }

        private void BlockDecompress(BinaryReader reader, BinaryWriter outWrite, BinaryReader outRead)
        {
            // It is flags, right?
            var flags = reader.ReadBytes(4); // TODO: Figure out what this is

            // outWrite.Write(flags);
            if ((flags[3] & 0x80) > 0)
            {
                outWrite.Write(reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position)));
            }
            else
            {
                var running = true;
                while (reader.BaseStream.Position != reader.BaseStream.Length && running)
                {
                    try
                    {
                        var blockMask = reader.ReadUInt16();
                        for (var i = 0; i < 16; i++)
                        {
                            // is the ith bit 1
                            if ((blockMask & (1 << i)) > 0)
                            {
                                var offsetSize = reader.ReadUInt16();
                                var offset = ((offsetSize & 0xFFF0) >> 4) + 1;
                                var size = (offsetSize & 0x000F) + 3;

                                var lookupSize = (offset < size) ? offset : size; // If the offset is larger or equal to the size, use the size instead.

                                // Kill me now
                                var p = outRead.BaseStream.Position;
                                outRead.BaseStream.Position = p - offset;
                                var data = outRead.ReadBytes(lookupSize);
                                outWrite.BaseStream.Position = p;

                                while (size > 0)
                                {
                                    outWrite.Write(data, 0, (lookupSize < size) ? lookupSize : size);
                                    size -= lookupSize;
                                }
                            }
                            else
                            {
                                var data = reader.ReadByte();
                                outWrite.Write(data);
                            }

                            //TODO: is there a better way of making an unsigned 12bit number?
                            if (outWrite.BaseStream.Length == (flags[2] << 16) + (flags[1] << 8) + flags[0])
                            {
                                running = false;
                                break;
                            }
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                }
            }

            outRead.BaseStream.Position = 0;
        }

        private void DecompressLZ4(BinaryReader reader, BinaryWriter outWrite)
        {
            var uncompressedSize = reader.ReadUInt32();
            var compressedSize = (int)(Size - (reader.BaseStream.Position - Offset));

            var input = reader.ReadBytes(compressedSize);
            var output = new Span<byte>(new byte[uncompressedSize]);

            LZ4Codec.Decode(input, output);

            outWrite.Write(output.ToArray()); // TODO: Write as span
            outWrite.BaseStream.Position = 0;
        }

        private static (KVType, KVFlag) ReadType(BinaryReader reader)
        {
            var databyte = reader.ReadByte();
            var flagInfo = KVFlag.None;

            if ((databyte & 0x80) > 0)
            {
                databyte &= 0x7F; // Remove the flag bit
                flagInfo = (KVFlag)reader.ReadByte();
            }

            return ((KVType)databyte, flagInfo);
        }

        private KVObject ParseBinaryKV3(BinaryReader reader, KVObject parent, bool inArray = false)
        {
            string name = null;
            if (!inArray)
            {
                var stringID = reader.ReadInt32();
                name = (stringID == -1) ? string.Empty : stringArray[stringID];
            }

            var (datatype, flagInfo) = ReadType(reader);

            return ReadBinaryValue(name, datatype, flagInfo, reader, parent);
        }

        private KVObject ReadBinaryValue(string name, KVType datatype, KVFlag flagInfo, BinaryReader reader, KVObject parent)
        {
            switch (datatype)
            {
                case KVType.NULL:
                    parent.AddProperty(name, MakeValue(datatype, null, flagInfo));
                    break;
                case KVType.BOOLEAN:
                    parent.AddProperty(name, MakeValue(datatype, reader.ReadBoolean(), flagInfo));
                    break;
                case KVType.BOOLEAN_TRUE:
                    parent.AddProperty(name, MakeValue(datatype, true, flagInfo));
                    break;
                case KVType.BOOLEAN_FALSE:
                    parent.AddProperty(name, MakeValue(datatype, false, flagInfo));
                    break;
                case KVType.INT64_ZERO:
                    parent.AddProperty(name, MakeValue(datatype, 0L, flagInfo));
                    break;
                case KVType.INT64_ONE:
                    parent.AddProperty(name, MakeValue(datatype, 1L, flagInfo));
                    break;
                case KVType.INT64:
                    parent.AddProperty(name, MakeValue(datatype, reader.ReadInt64(), flagInfo));
                    break;
                case KVType.UINT64:
                    parent.AddProperty(name, MakeValue(datatype, reader.ReadUInt64(), flagInfo));
                    break;
                case KVType.INT32:
                    parent.AddProperty(name, MakeValue(datatype, reader.ReadInt32(), flagInfo));
                    break;
                case KVType.UINT32:
                    parent.AddProperty(name, MakeValue(datatype, reader.ReadUInt32(), flagInfo));
                    break;
                case KVType.DOUBLE:
                    parent.AddProperty(name, MakeValue(datatype, reader.ReadDouble(), flagInfo));
                    break;
                case KVType.DOUBLE_ZERO:
                    parent.AddProperty(name, MakeValue(datatype, 0.0D, flagInfo));
                    break;
                case KVType.DOUBLE_ONE:
                    parent.AddProperty(name, MakeValue(datatype, 1.0D, flagInfo));
                    break;
                case KVType.STRING:
                    var id = reader.ReadInt32();
                    parent.AddProperty(name, MakeValue(datatype, id == -1 ? string.Empty : stringArray[id], flagInfo));
                    break;
                case KVType.BINARY_BLOB:
                    var length = reader.ReadInt32();
                    parent.AddProperty(name, MakeValue(datatype, reader.ReadBytes(length), flagInfo));
                    break;
                case KVType.ARRAY:
                    var arrayLength = reader.ReadInt32();
                    var array = new KVObject(name, true);
                    for (var i = 0; i < arrayLength; i++)
                    {
                        ParseBinaryKV3(reader, array, true);
                    }

                    parent.AddProperty(name, MakeValue(datatype, array, flagInfo));
                    break;
                case KVType.ARRAY_TYPED:
                    var typeArrayLength = reader.ReadInt32();
                    var (subType, subFlagInfo) = ReadType(reader);
                    var typedArray = new KVObject(name, true);

                    for (var i = 0; i < typeArrayLength; i++)
                    {
                        ReadBinaryValue(name, subType, subFlagInfo, reader, typedArray);
                    }

                    parent.AddProperty(name, MakeValue(datatype, typedArray, flagInfo));
                    break;
                case KVType.OBJECT:
                    var objectLength = reader.ReadInt32();
                    var newObject = new KVObject(name, false);
                    for (var i = 0; i < objectLength; i++)
                    {
                        ParseBinaryKV3(reader, newObject, false);
                    }

                    if (parent == null)
                    {
                        parent = newObject;
                    }
                    else
                    {
                        parent.AddProperty(name, MakeValue(datatype, newObject, flagInfo));
                    }

                    break;
                default:
                    throw new InvalidDataException($"Unknown KVType {datatype} for field '{name}' on byte {reader.BaseStream.Position - 1}");
            }

            return parent;
        }

        private static KVType ConvertBinaryOnlyKVType(KVType type)
        {
            switch (type)
            {
                case KVType.BOOLEAN:
                case KVType.BOOLEAN_TRUE:
                case KVType.BOOLEAN_FALSE:
                    return KVType.BOOLEAN;
                case KVType.INT64:
                case KVType.INT32:
                case KVType.INT64_ZERO:
                case KVType.INT64_ONE:
                    return KVType.INT64;
                case KVType.UINT64:
                case KVType.UINT32:
                    return KVType.UINT64;
                case KVType.DOUBLE:
                case KVType.DOUBLE_ZERO:
                case KVType.DOUBLE_ONE:
                    return KVType.DOUBLE;
                case KVType.ARRAY_TYPED:
                    return KVType.ARRAY;
            }

            return type;
        }

        private static KVValue MakeValue(KVType type, object data, KVFlag flag)
        {
            var realType = ConvertBinaryOnlyKVType(type);

            if (flag != KVFlag.None)
            {
                return new KVFlaggedValue(realType, flag, data);
            }

            return new KVValue(realType, data);
        }

        public KV3File GetKV3File()
        {
            // TODO: Other format guids are not "generic" but strings like "vpc19"
            return new KV3File(Data, format: $"generic:version{{{Format.ToString()}}}");
        }

        public override void WriteText(IndentedTextWriter writer)
        {
            Data.Serialize(writer);
        }
    }
}
