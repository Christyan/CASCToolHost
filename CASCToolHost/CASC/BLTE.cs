﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace CASCToolHost
{
    public static class BLTE
    {
        public static byte[] Parse(byte[] content, bool verify = true)
        {
            using (var result = new MemoryStream())
            using (var bin = new BinaryReader(new MemoryStream(content)))
            {
                if (bin.ReadUInt32() != 0x45544c42) { throw new Exception("Not a BLTE file"); }

                var blteSize = bin.ReadUInt32(true);

                BLTEChunkInfo[] chunkInfos;

                if (blteSize == 0)
                {
                    chunkInfos = new BLTEChunkInfo[1];
                    chunkInfos[0].isFullChunk = false;
                    chunkInfos[0].inFileSize = Convert.ToInt32(bin.BaseStream.Length - bin.BaseStream.Position);
                    chunkInfos[0].actualSize = Convert.ToInt32(bin.BaseStream.Length - bin.BaseStream.Position);
                    chunkInfos[0].checkSum = new byte[16]; ;
                }
                else
                {
                    var bytes = bin.ReadBytes(4);

                    var chunkCount = bytes[1] << 16 | bytes[2] << 8 | bytes[3] << 0;

                    var supposedHeaderSize = 24 * chunkCount + 12;

                    if (supposedHeaderSize != blteSize)
                    {
                        throw new Exception("Invalid header size!");
                    }

                    if (supposedHeaderSize > bin.BaseStream.Length)
                    {
                        throw new Exception("Not enough data");
                    }

                    chunkInfos = new BLTEChunkInfo[chunkCount];

                    for (int i = 0; i < chunkCount; i++)
                    {
                        chunkInfos[i].isFullChunk = true;
                        chunkInfos[i].inFileSize = bin.ReadInt32(true);
                        chunkInfos[i].actualSize = bin.ReadInt32(true);
                        chunkInfos[i].checkSum = new byte[16];
                        chunkInfos[i].checkSum = bin.ReadBytes(16);
                    }
                }

                for (var index = 0; index < chunkInfos.Count(); index++)
                {
                    var chunk = chunkInfos[index];

                    if (chunk.inFileSize > (bin.BaseStream.Length - bin.BaseStream.Position))
                    {
                        throw new Exception("Trying to read more than is available!");
                    }

                    var chunkBuffer = bin.ReadBytes(chunk.inFileSize);

                    if (verify)
                    {
                        var hasher = MD5.Create();
                        var md5sum = hasher.ComputeHash(chunkBuffer);

                        if (chunk.isFullChunk && BitConverter.ToString(md5sum) != BitConverter.ToString(chunk.checkSum))
                        {
                            throw new Exception("MD5 checksum mismatch on BLTE chunk! Sum is " + BitConverter.ToString(md5sum).Replace("-", "") + " but is supposed to be " + BitConverter.ToString(chunk.checkSum).Replace("-", ""));
                        }
                    }

                    using (var chunkResult = new MemoryStream())
                    {
                        HandleDataBlock(chunkBuffer, index, chunk, chunkResult);

                        var chunkres = chunkResult.ToArray();

                        if (chunk.isFullChunk && chunkres.Length != chunk.actualSize)
                        {
                            throw new Exception("Decoded result is wrong size!");
                        }

                        result.Write(chunkres, 0, chunkres.Length);
                    }
                }

                return result.ToArray();
            }
        }
        private static void HandleDataBlock(byte[] chunkBuffer, int index, BLTEChunkInfo chunk, MemoryStream chunkResult)
        {
            using (var chunkreader = new BinaryReader(new MemoryStream(chunkBuffer)))
            {
                var mode = chunkreader.ReadChar();

                switch (mode)
                {
                    case 'N': // none
                        chunkResult.Write(chunkreader.ReadBytes(chunk.actualSize), 0, chunk.actualSize); //read actual size because we already read the N from chunkreader
                        break;
                    case 'Z': // zlib
                        using (var stream = new MemoryStream(chunkreader.ReadBytes(chunk.inFileSize - 1), 2, chunk.inFileSize - 3))
                        using (var ds = new DeflateStream(stream, CompressionMode.Decompress))
                        {
                            ds.CopyTo(chunkResult);
                        }
                        break;
                    case 'E': // encrypted
                        byte[] decrypted = new byte[chunkBuffer.Length - 15];
                        decrypted[0] = Convert.ToByte('N');
                        try
                        {
                            decrypted = Decrypt(chunkBuffer, index);
                        }
                        catch (KeyNotFoundException e)
                        {
                            Console.WriteLine(e.Message);
                            chunkResult.Write(new byte[chunk.actualSize], 0, chunk.actualSize);
                            break;
                        }

                        // Override inFileSize with decrypted length because it now differs from original encrypted chunk.inFileSize which breaks decompression
                        chunk.inFileSize = decrypted.Length;

                        //Console.WriteLine("Decrypted chunk size is " + chunk.inFileSize);
                        HandleDataBlock(decrypted, index, chunk, chunkResult);
                        break;
                    case 'F': // frame
                    default:
                        throw new Exception("Unsupported mode " + mode + "!");
                }
            }
        }
        private static string ReturnEncryptionKeyName(byte[] data)
        {
            byte keyNameSize = data[0];

            if (keyNameSize == 0 || keyNameSize != 8)
            {
                Console.WriteLine(keyNameSize.ToString());
                throw new Exception("keyNameSize == 0 || keyNameSize != 8");
            }

            byte[] keyNameBytes = new byte[keyNameSize];
            Array.Copy(data, 1, keyNameBytes, 0, keyNameSize);

            Array.Reverse(keyNameBytes);

            return BitConverter.ToString(keyNameBytes).Replace("-", "");
        }
        private static byte[] Decrypt(byte[] data, int index)
        {
            byte keyNameSize = data[1];

            if (keyNameSize == 0 || keyNameSize != 8)
                throw new Exception("keyNameSize == 0 || keyNameSize != 8");

            byte[] keyNameBytes = new byte[keyNameSize];
            Array.Copy(data, 2, keyNameBytes, 0, keyNameSize);

            ulong keyName = BitConverter.ToUInt64(keyNameBytes, 0);

            byte IVSize = data[keyNameSize + 2];

            if (IVSize != 4 || IVSize > 0x10)
                throw new Exception("IVSize != 4 || IVSize > 0x10");

            byte[] IVpart = new byte[IVSize];
            Array.Copy(data, keyNameSize + 3, IVpart, 0, IVSize);

            if (data.Length < IVSize + keyNameSize + 4)
                throw new Exception("data.Length < IVSize + keyNameSize + 4");

            int dataOffset = keyNameSize + IVSize + 3;

            byte encType = data[dataOffset];

            if (encType != 'S' && encType != 'A') // 'S' or 'A'
                throw new Exception("encType != ENCRYPTION_SALSA20 && encType != ENCRYPTION_ARC4");

            dataOffset++;

            // expand to 8 bytes
            byte[] IV = new byte[8];
            Array.Copy(IVpart, IV, IVpart.Length);

            // magic
            for (int shift = 0, i = 0; i < sizeof(int); shift += 8, i++)
            {
                IV[i] ^= (byte)((index >> shift) & 0xFF);
            }

            byte[] key = KeyService.GetKey(keyName);

            if (key == null)
                throw new KeyNotFoundException("Unknown keyname " + keyName.ToString("X16"));

            if (encType == 'S')
            {
                ICryptoTransform decryptor = KeyService.SalsaInstance.CreateDecryptor(key, IV);

                return decryptor.TransformFinalBlock(data, dataOffset, data.Length - dataOffset);
            }
            else
            {
                // ARC4 ?
                throw new Exception("encType ENCRYPTION_ARC4 not implemented");
            }
        }

        public static byte[] DecryptFile(string name, byte[] data, string decryptionKeyName)
        {
            byte[] key = new byte[16];

            using (BinaryReader reader = new BinaryReader(new FileStream(decryptionKeyName + ".ak", FileMode.Open)))
            {
                key = reader.ReadBytes(16);
            }

            byte[] IV = name.ToByteArray();

            Array.Copy(IV, 8, IV, 0, 8);
            Array.Resize(ref IV, 8);

            var salsa = new Salsa20();
            var decryptor = salsa.CreateDecryptor(key, IV);

            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }
}