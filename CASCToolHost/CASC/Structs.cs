﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace CASCToolHost
{
    public unsafe struct MD5Hash
    {
        public fixed byte Value[16];
    }

    public struct VersionsFile
    {
        public VersionsEntry[] entries;
    }

    public struct VersionsEntry
    {
        public string region;
        public string buildConfig;
        public string cdnConfig;
        public string buildId;
        public string versionsName;
        public string productConfig;
        public string keyRing;
    }

    public struct CdnsFile
    {
        public CdnsEntry[] entries;
    }

    public struct CdnsEntry
    {
        public string name;
        public string path;
        public string[] hosts;
        public string configPath;
    }

    public struct GameBlobFile
    {
        public string decryptionKeyName;
    }

    public struct BuildConfigFile
    {
        public MD5Hash root;
        public MD5Hash[] download;
        public string[] downloadSize;
        public MD5Hash[] install;
        public string[] installSize;
        public MD5Hash[] encoding;
        public string[] encodingSize;
        public string[] size;
        public string[] sizeSize;
        public string buildName;
        public string buildPlaybuildInstaller;
        public string buildProduct;
        public string buildUid;
        public string buildBranch;
        public string buildNumber;
        public string buildAttributes;
        public string buildComments;
        public string buildCreator;
        public string buildFixedHash;
        public string buildReplayHash;
        public string buildManifestVersion;
        public MD5Hash patch;
        public string patchSize;
        public MD5Hash patchConfig;
        public string partialPriority;
        public string partialPrioritySize;
    }

    public struct CDNConfigFile
    {
        public MD5Hash[] archives;
        public string archiveGroup;
        public MD5Hash[] patchArchives;
        public string patchArchiveGroup;
        public string[] builds;
        public string fileIndex;
        public string fileIndexSize;
        public string patchFileIndex;
        public string patchFileIndexSize;
    }

    public struct IndexEntry
    {
        public uint indexID;
        public uint offset;
        public uint size;
    }

    public struct IndexFooter
    {
        public byte[] tocHash;
        public byte version;
        public byte unk0;
        public byte unk1;
        public byte blockSizeKB;
        public byte offsetBytes;
        public byte sizeBytes;
        public byte keySizeInBytes;
        public byte checksumSize;
        public uint numElements;
        public byte[] footerChecksum;
    }

    public struct EncodingFile
    {
        public byte version;
        public byte cKeyLength;
        public byte eKeyLength;
        public ushort cKeyPageSize;
        public ushort eKeyPageSize;
        public uint cKeyPageCount;
        public uint eKeyPageCount;
        public byte unk;
        public ulong stringBlockSize;
        public string[] stringBlockEntries;
        public EncodingHeaderEntry[] aHeaders;
        public Dictionary<MD5Hash, EncodingFileEntry> aEntries;
        public EncodingHeaderEntry[] bHeaders;
        public EncodingFileDescEntry[] bEntries;
    }

    public struct EncodingHeaderEntry
    {
        public MD5Hash firstHash;
        public MD5Hash checksum;
    }

    public struct EncodingFileEntry
    {
        public long size;
        public MD5Hash eKey;
    }

    public struct EncodingFileDescEntry
    {
        public MD5Hash key;
        public uint stringIndex;
        public ulong compressedSize;
    }

    public struct InstallFile
    {
        public byte hashSize;
        public ushort numTags;
        public uint numEntries;
        public InstallTagEntry[] tags;
        public InstallFileEntry[] entries;
    }

    public struct InstallTagEntry
    {
        public string name;
        public ushort type;
        public BitArray files;
    }

    public struct InstallFileEntry
    {
        public string name;
        public string contentHash;
        public uint size;
        public List<string> tags;
    }

    public struct DownloadFile
    {
        public byte[] unk;
        public uint numEntries;
        public uint numTags;
        public DownloadEntry[] entries;
    }

    public struct DownloadEntry
    {
        public string hash;
        public byte[] unk;
    }

    public struct BLTEChunkInfo
    {
        public bool isFullChunk;
        public int compSize;
        public int decompSize;
        public byte[] checkSum;
    }

    public struct RootFile
    {
        public MultiDictionary<ulong, RootEntry> entriesLookup;
        public MultiDictionary<uint, RootEntry> entriesFDID;
    }

    public struct RootEntry
    {
        public ContentFlags contentFlags;
        public LocaleFlags localeFlags;
        public ulong lookup;
        public uint fileDataID;
        public MD5Hash md5;
    }

    public struct PatchFile
    {
        public byte version;
        public byte fileKeySize;
        public byte sizeB;
        public byte patchKeySize;
        public byte blockSizeBits;
        public ushort blockCount;
        public byte flags;
        public byte[] encodingContentKey;
        public byte[] encodingEncodingKey;
        public uint decodedSize;
        public uint encodedSize;
        public byte especLength;
        public string encodingSpec;
        public PatchBlock[] blocks;
    }

    public struct PatchBlock
    {
        public byte[] lastFileContentKey;
        public byte[] blockMD5;
        public uint blockOffset;
        public BlockFile[] files;
    }

    public struct BlockFile
    {
        public byte numPatches;
        public byte[] targetFileContentKey;
        public ulong decodedSize;
        public FilePatch[] patches;
    }

    public struct FilePatch
    {
        public byte[] sourceFileEncodingKey;
        public ulong decodedSize;
        public byte[] patchEncodingKey;
        public uint patchSize;
        public byte patchIndex;
    }

    [Flags]
    public enum LocaleFlags : uint
    {
        All = 0xFFFFFFFF,
        None = 0,
        //Unk_1 = 0x1,
        enUS = 0x2,
        koKR = 0x4,
        //Unk_8 = 0x8,
        frFR = 0x10,
        deDE = 0x20,
        zhCN = 0x40,
        esES = 0x80,
        zhTW = 0x100,
        enGB = 0x200,
        enCN = 0x400,
        enTW = 0x800,
        esMX = 0x1000,
        ruRU = 0x2000,
        ptBR = 0x4000,
        itIT = 0x8000,
        ptPT = 0x10000,
        enSG = 0x20000000, // custom
        plPL = 0x40000000, // custom
        All_WoW = enUS | koKR | frFR | deDE | zhCN | esES | zhTW | enGB | esMX | ruRU | ptBR | itIT | ptPT
    }

    [Flags]
    public enum ContentFlags : uint
    {
        None = 0,
        F00000001 = 0x1,
        F00000002 = 0x2,
        F00000004 = 0x4,
        F00000008 = 0x8, // added in 7.2.0.23436
        F00000010 = 0x10, // added in 7.2.0.23436
        LowViolence = 0x80, // many models have this flag
        Encrypted = 0x8000000,
        NoNames = 0x10000000,
        F20000000 = 0x20000000, // added in 21737
        Bundle = 0x40000000,
        NoCompression = 0x80000000 // sounds have this flag
    }
}
