﻿using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System;

namespace SharpestInjector
{
    public class PeFile
    {
        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        struct DosHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] Magic;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 29)]
            public ushort[] Reserved; // Reserved means dontcare from now on
            public uint OffsetPE;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PeHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] Magic;
            public ushort Architecture;
            public ushort SectionCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Reserved;
            public ushort OptionalHeaderSize;
            public ushort Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Pe32pHeader // I have no idea where I got this name from, but I'm keeping it
        {
            public PeFormat Magic;
            public byte LinkerMajor;
            public byte LinkerMinor;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Reserved;
            public uint EntryPointAddress;
            public uint CodeBaseAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Win64
        {
            public ulong ImageBaseAddress;  // Must be multiple of 64K
            public uint SectionAlignment;   // Greater or equal to FileAlignment. If smaller than page size, both must be equal. Default is 4K
            public uint FileAlignment;      // Must be power of 2 between 512 and 64K
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] Reserved;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Reserved2;
            public uint ImageSize;
            public uint HeaderSize;
            public uint Checksum;
            public ushort Subsystem;
            public ushort Reserved3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ulong[] Reserved4;
            public uint Reserved5;
            public uint DataDirectoryCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Win32
        {
            public uint Reserved0;
            public uint ImageBaseAddress;  // Must be multiple of 64K
            public uint SectionAlignment;   // Greater or equal to FileAlignment. If smaller than page size, both must be equal. Default is 4K
            public uint FileAlignment;      // Must be power of 2 between 512 and 64K
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] Reserved;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Reserved2;
            public uint ImageSize;
            public uint HeaderSize;
            public uint Checksum;
            public ushort Subsystem;
            public ushort Reserved3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public uint[] Reserved4;
            public uint Reserved5;
            public uint DataDirectoryCount;
        }


        [StructLayout(LayoutKind.Sequential)]
        struct DataDirectory
        {
            public uint VirtualAddress;
            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SectionTable
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public char[] Name;
            public uint VirtualSize;
            public uint VirtualAddress;
            public uint RawDataSize;
            public uint RawDataPointer;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Reserved;
            public uint Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ExportDirectoryTable
        {
            public uint Flags;
            public uint TimeStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public uint NameVirtualAddress;
            public uint OrdinalBase;
            public uint AddressTableEntriesCount;
            public uint NamePointersCount;
            public uint ExportAddressTableVirtualAddress;
            public uint NamePointerVirtualAddress;
            public uint OrdinalTableVirtualAddress;
        }

        #endregion

        enum PeFormat : ushort
        {
            Unknown = 0,
            x86 = 0x10b,
            x64 = 0x20b
        }

        const string DosMagic = "MZ";
        const string PeMagic = "PE\0\0";

        public readonly string FileName;
        public readonly Dictionary<string, int> Exports;
        public IReadOnlyCollection<string> ExportNames => Exports.Keys;

        public bool Is64Bit = false;

        PeFile(string fileName, Dictionary<string, int> exports, bool is64Bit)
        {
            FileName = Path.GetFullPath(fileName);
            Exports = exports;
            Is64Bit = is64Bit;
        }

        public int GetExportAddress(string exportName)
        {
            if (Exports.TryGetValue(exportName, out var loadLibraryAddress))
                return loadLibraryAddress;

            throw new Exception($"Couldn't find export {exportName} in {FileName}");
        }

        public static PeFile Parse(string filePath)
        {
            var file = File.OpenRead(filePath);

            var dosHeader = ParseStruct<DosHeader>(file);
            for (int i = 0; i < dosHeader.Magic.Length; i++)
            {
                if (dosHeader.Magic[i] != DosMagic[i])
                    throw new Exception($"Incorrect DOS header magic for file {filePath}");
            }

            file.Seek(dosHeader.OffsetPE, SeekOrigin.Begin);
            var peHeader = ParseStruct<PeHeader>(file);
            for (int i = 0; i < peHeader.Magic.Length; i++)
            {
                if (peHeader.Magic[i] != PeMagic[i])
                    throw new Exception($"Incorrect PE header magic for file {filePath}");
            }

            var pe32pHeader = ParseStruct<Pe32pHeader>(file);

            bool is64Bit = false;

            uint dataDirectoryCount;
            switch (pe32pHeader.Magic)
            {
                case PeFormat.x86:
                    dataDirectoryCount = ParseStruct<Win32>(file).DataDirectoryCount;
                    break;
                case PeFormat.x64:
                    is64Bit = true;
                    dataDirectoryCount = ParseStruct<Win64>(file).DataDirectoryCount;
                    break;
                default:
                    throw new Exception($"Incorrect PE32p header magic for file {filePath}");
            }

            // Parse directories and get export directory address
            uint exportVa = 0;
            bool exportsEmpty = false;
            for (int i = 0; i < dataDirectoryCount; i++)
            {
                var dataDirectory = ParseStruct<DataDirectory>(file);

                switch (i)
                {
                    case 0:
                        exportVa = dataDirectory.VirtualAddress;
                        exportsEmpty = (dataDirectory.Size == 0);
                        break;
                }
            }

            var nameToAddress = new Dictionary<string, int>();

            if(exportsEmpty)
                return new PeFile(filePath, nameToAddress, is64Bit); // No exports, don't continue. TODO: I can do this better

            // Get sections
            var sections = new List<SectionTable>();
            for (int i = 0; i < peHeader.SectionCount; i++)
            {
                var teee = ParseStruct<SectionTable>(file);
                sections.Add(teee);
            }

            // Thank neojordan https://programmer.help/blogs/export-table-of-pe-file-image_export_directory.html
            var exportTableAddress = ConvertRvaToFoa(exportVa, sections);

            // Get export directory table
            file.Position = exportTableAddress;
            var exportDirectoryTable = ParseStruct<ExportDirectoryTable>(file);

            var exportAddressTableAddress = ConvertRvaToFoa(exportDirectoryTable.ExportAddressTableVirtualAddress, sections);
            var ordinalTableAddress = ConvertRvaToFoa(exportDirectoryTable.OrdinalTableVirtualAddress, sections);
            var namePointerAddress = ConvertRvaToFoa(exportDirectoryTable.NamePointerVirtualAddress, sections);

            // Get export addresses
            file.Position = exportAddressTableAddress;
            var exportAddresses = new List<uint>();
            for (int i = 0; i < exportDirectoryTable.AddressTableEntriesCount; i++)
            {
                var bytes = new byte[4];

                file.Read(bytes, 0, 4);

                exportAddresses.Add(BitConverter.ToUInt32(bytes, 0));
            }

            // Get name ordinals
            file.Position = ordinalTableAddress;
            var nameOrdinalList = new List<ushort>();
            for (int i = 0; i < exportDirectoryTable.NamePointersCount; i++)
            {
                var bytes = new byte[2];

                file.Read(bytes, 0, 2);

                nameOrdinalList.Add(BitConverter.ToUInt16(bytes, 0));
            }

            // Get name pointers
            file.Position = namePointerAddress;
            var namePointerList = new List<uint>();
            for (int i = 0; i < exportDirectoryTable.NamePointersCount; i++)
            {
                var bytes = new byte[4];

                file.Read(bytes, 0, 4);

                namePointerList.Add(BitConverter.ToUInt32(bytes, 0));
            }

            // Get final names
            for (int i = 0; i < exportDirectoryTable.NamePointersCount; i++)
            {
                var name = "";
                char character;

                file.Position = ConvertRvaToFoa(namePointerList[i], sections);

                while ((character = (char)file.ReadByte()) != 0)
                {
                    name += character;
                }

                nameToAddress[name] = (int)exportAddresses[nameOrdinalList[i]];
            }

            return new PeFile(filePath, nameToAddress, is64Bit);
        }

        static T ParseStruct<T>(Stream stream) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var bytes = new byte[size];
            stream.Read(bytes, 0, size);
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var struc = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return struc;
        }

        // And also thank https://www.programmersought.com/article/71876003055/
        static uint ConvertRvaToFoa(uint rva, List<SectionTable> sections)
        {
            if (sections.Count == 0)
                return 0;

            var sectionStart = sections[0];

            if (rva < sectionStart.VirtualAddress)
            {
                if (rva < sectionStart.RawDataPointer)
                    return rva;
                else
                    return 0;
            }

            foreach (var section in sections)
            {
                if (section.VirtualAddress <= rva && rva <= section.VirtualAddress + section.RawDataSize)
                {
                    return rva - section.VirtualAddress + section.RawDataPointer;
                }
            }

            return 0;
        }
    }
}
