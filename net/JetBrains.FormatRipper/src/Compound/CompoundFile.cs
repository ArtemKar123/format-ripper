﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.FormatRipper.Compound.Impl;
using JetBrains.FormatRipper.Impl;

namespace JetBrains.FormatRipper.Compound
{
  /// <summary>
  ///   Object Linking and Embedding (OLE) Compound File (CF) (i.e., OLECF) or Compound Binary File format by Microsoft
  /// </summary>
  public sealed class CompoundFile
  {
    private const string RootEntryName = "Root Entry";

    private static readonly byte[] ourHeaderSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

    public enum FileType
    {
      Unknown,
      Msi
    }

    public readonly struct ExtractStream
    {
      public readonly string[] Names;
      public readonly Guid Clsid;
      public readonly byte[] Blob;

      internal ExtractStream(string[] names, Guid clsid, byte[] blob)
      {
        Names = names;
        Clsid = clsid;
        Blob = blob;
      }
    }

    public FileType Type { get; private set; }
    public bool HasSignature { get; private set; }
    public SignatureData SignatureData { get; private set; }
    public ExtractStream[] ExtractStreams { get; private set; }
    public ComputeHashInfo? ComputeHashInfo { get; private set; }
    public CompoundFileHeaderMetaInfo HeaderMetaInfo { get; private set; }
    public long fileSize { get; private set; }

    private uint sectorSize { get; }
    private Stream _stream { get; }
    private List<REGSECT> diFatTable { get; }
    private uint entitiesPerSector { get; }
    private DirectoryEntry rootDirectoryEntry { get; }
    private REGSECT firstMiniFatSectorLocation { get; }
    private List<DirectoryEntry> directoryEntries { get; }
    private uint miniStreamCutoffSize { get; }
    private static uint DirectoryEntrySize { get; } = 0x80u;

    public CompoundFile(CompoundFileHeaderMetaInfo headerMetaInfo, Stream stream)
    {
      _stream = stream;
      _stream.Position = 0;

      HeaderMetaInfo = headerMetaInfo;
      sectorSize = 1u << HeaderMetaInfo.Header.SectorShift;
      entitiesPerSector = sectorSize / sizeof(uint);
      firstMiniFatSectorLocation = (REGSECT)HeaderMetaInfo.Header.FirstMiniFatSectorLocation;

      WriteHeader(HeaderMetaInfo.Header);

      diFatTable = new List<REGSECT>();
      foreach (var it in HeaderMetaInfo.SectFat)
      {
        diFatTable.Add((REGSECT)it);
        var buf = MemoryUtil.ToByteArray(it);
        _stream.Write(buf, 0, buf.Length);
      }

      WriteFat();

      WriteMiniFat();
    }

    private void WriteMiniFat()
    {
      var it = 0;
      var nextSect = firstMiniFatSectorLocation;
      while (nextSect != REGSECT.ENDOFCHAIN)
      {
        if (nextSect > REGSECT.MAXREGSECT)
        {
          break;
        }

        _stream.Position = GetSectorPosition(nextSect);
        for (int i = 0; i < (1u << HeaderMetaInfo.Header.SectorShift) >> 2; i++)
        {
          var buf = MemoryUtil.ToByteArray(HeaderMetaInfo.MiniFat[it++]);
          _stream.Write(buf, 0, buf.Length);
        }

        nextSect = (REGSECT)HeaderMetaInfo.Fat[(int)nextSect];
      }
    }

    private void WriteFat()
    {
      var it = 0;
      foreach (var sect in diFatTable)
      {
        if (sect > REGSECT.MAXREGSECT)
        {
          continue;
        }

        _stream.Position = GetSectorPosition(sect);
        for (int i = 0; i < (1u << HeaderMetaInfo.Header.SectorShift) >> 2; i++)
        {
          var buf = MemoryUtil.ToByteArray(HeaderMetaInfo.Fat[it++]);
          _stream.Write(buf, 0, buf.Length);
        }
      }
    }

    private unsafe void WriteHeader(CompoundFileHeader header)
    {
      BinaryWriter writer = new BinaryWriter(_stream, Encoding.Unicode);

      for (int i = 0; i < Declarations.HeaderSignatureSize; i++)
      {
        writer.Write(header.HeaderSignature[i]);
      }

      writer.Write(header.HeaderClsid.ToByteArray());
      writer.Write(header.MinorVersion);
      writer.Write(header.MajorVersion);
      writer.Write(header.ByteOrder);
      writer.Write(header.SectorShift);
      writer.Write(header.MiniSectorShift);

      for (int i = 0; i < 6; i++)
      {
        writer.Write(header.Reserved[i]);
      }

      writer.Write(header.NumberOfDirectorySectors);
      writer.Write(header.NumberOfFatSectors);
      writer.Write(header.FirstDirectorySectorLocation);
      writer.Write(header.TransactionSignatureNumber);
      writer.Write(header.MiniStreamCutoffSize);
      writer.Write(header.FirstMiniFatSectorLocation);
      writer.Write(header.NumberOfMiniFatSectors);
      writer.Write(header.FirstDiFatSectorLocation);
      writer.Write(header.NumberOfDiFatSectors);
    }

    private unsafe CompoundFile(Stream stream, Mode mode = Mode.Default,
      ExtractFilter? extractFilter = null)
    {
      fileSize = stream.Length;
      _stream = stream;
      _stream.Position = 0;
      CompoundFileHeader cfh;
      StreamUtil.ReadBytes(_stream, (byte*)&cfh, sizeof(CompoundFileHeader));
      if (!MemoryUtil.ArraysEqual(cfh.HeaderSignature, Declarations.HeaderSignatureSize, ourHeaderSignature))
        throw new FormatException("Invalid CF header signature");
      if (MemoryUtil.GetLeGuid(cfh.HeaderClsid) != Guid.Empty)
        throw new FormatException("Invalid CF header CLSID");
      if (MemoryUtil.GetLeU2(cfh.ByteOrder) != Declarations.LittleEndianByteOrder)
        throw new FormatException("Invalid CF header byte order");
      switch (MemoryUtil.GetLeU2(cfh.MajorVersion))
      {
        case 3:
          if (MemoryUtil.GetLeU2(cfh.MinorVersion) != 0x3E)
            throw new FormatException("Invalid CF minor version");
          if (MemoryUtil.GetLeU2(cfh.SectorShift) != 9)
            throw new FormatException("Invalid CF sector shift");
          if (MemoryUtil.GetLeU4(cfh.NumberOfDirectorySectors) != 0)
            throw new FormatException("Invalid CF number of directory sectors");
          break;
        case 4:
          if (MemoryUtil.GetLeU2(cfh.MinorVersion) != 0x3E)
            throw new FormatException("Invalid CF minor version");
          if (MemoryUtil.GetLeU2(cfh.SectorShift) != 0xC)
            throw new FormatException("Invalid CF sector shift");
          break;
        default:
          throw new FormatException($"Unsupported CF major version {MemoryUtil.GetLeU2(cfh.MajorVersion)}");
      }

      if (MemoryUtil.GetLeU2(cfh.MiniSectorShift) != 6)
        throw new FormatException("Invalid CF mini sector shift");
      if (MemoryUtil.GetLeU4(cfh.MiniStreamCutoffSize) != 0x00001000)
        throw new FormatException("Invalid CF mini stream cutoff size");


      var metaInfo = new CompoundFileHeaderMetaInfo(cfh);

      sectorSize = 1u << MemoryUtil.GetLeU2(cfh.SectorShift);
      entitiesPerSector = sectorSize / sizeof(uint);
      var entitiesPerDirectorySector = sectorSize / sizeof(CompoundFileDirectoryEntry);

      diFatTable = new List<REGSECT>(Declarations.HeaderDiFatSize);
      {
        var buffer = stackalloc uint[checked((int)entitiesPerSector)];
        StreamUtil.ReadBytes(_stream, (byte*)buffer, sizeof(uint) * Declarations.HeaderDiFatSize);
        for (var n = 0; n < Declarations.HeaderDiFatSize; ++n)
        {
          var sect = (REGSECT)MemoryUtil.GetLeU4(buffer[n]);
          if (sect == REGSECT.FREESECT)
          {
            break;
          }

          metaInfo.SectFat.Add((uint)sect);
          diFatTable.Add(sect);
        }

        var diFatSectorLocation = (REGSECT)MemoryUtil.GetLeU4(cfh.FirstDiFatSectorLocation);
        for (var k = MemoryUtil.GetLeU4(cfh.NumberOfDiFatSectors); k-- > 0;)
        {
          _stream.Position = GetSectorPosition(diFatSectorLocation);
          StreamUtil.ReadBytes(_stream, (byte*)buffer, checked((int)sectorSize));
          var n = 0;
          for (; n < entitiesPerSector - 1; ++n)
            diFatTable.Add((REGSECT)MemoryUtil.GetLeU4(buffer[n]));
          diFatSectorLocation = (REGSECT)MemoryUtil.GetLeU4(buffer[n]);
        }
      }

      var position = _stream.Position;
      try
      {
        foreach (var sect in diFatTable)
        {
          if (sect > REGSECT.MAXREGSECT)
          {
            continue;
          }

          _stream.Position = GetSectorPosition(sect);
          for (int i = 0; i < (1u << cfh.SectorShift) >> 2; i++)
          {
            uint buffer;
            StreamUtil.ReadBytes(_stream, (byte*)&buffer, sizeof(uint));
            metaInfo.Fat.Add(MemoryUtil.GetLeU4(buffer));
          }
        }
      }
      catch (Exception)
      {
        // ignored
      }

      _stream.Position = position;
      var firstDirectorySectorLocation = (REGSECT)MemoryUtil.GetLeU4(cfh.FirstDirectorySectorLocation);
      directoryEntries = new List<DirectoryEntry>();
      {
        var cfdes = stackalloc CompoundFileDirectoryEntry[checked((int)entitiesPerDirectorySector)];
        for (var directorySectorLocation = firstDirectorySectorLocation;
             directorySectorLocation != REGSECT.ENDOFCHAIN;
             directorySectorLocation = GetNextSector(directorySectorLocation))
        {
          _stream.Position = GetSectorPosition(directorySectorLocation);
          StreamUtil.ReadBytes(_stream, (byte*)cfdes, checked((int)sectorSize));
          for (var n = 0; n < entitiesPerDirectorySector; ++n)
          {
            var directoryEntryNameLength = MemoryUtil.GetLeU2(cfdes[n].DirectoryEntryNameLength);
            if (directoryEntryNameLength is < 0 or > Declarations.DirectoryEntryNameSize ||
                directoryEntryNameLength % 2 != 0)
              throw new FormatException("Invalid CF directory entry name length");
            var name = directoryEntryNameLength == 0
              ? ""
              : new string(Encoding.Unicode.GetChars(MemoryUtil.CopyBytes(cfdes[n].DirectoryEntryName,
                directoryEntryNameLength - 2)));
            directoryEntries.Add(new DirectoryEntry(
              name,
              MemoryUtil.GetLeGuid(cfdes[n].Clsid),
              (STGTY)cfdes[n].ObjectType,
              (CF)cfdes[n].ColorFlag,
              (REGSID)MemoryUtil.GetLeU4(cfdes[n].LeftSiblingId),
              (REGSID)MemoryUtil.GetLeU4(cfdes[n].RightSiblingId),
              (REGSID)MemoryUtil.GetLeU4(cfdes[n].ChildId),
              (REGSECT)MemoryUtil.GetLeU4(cfdes[n].StartingSectorLocation),
              MemoryUtil.GetLeU8(cfdes[n].StreamSize),
              cfdes[n]));
          }
        }
      }

      if (directoryEntries.Count == 0)
        throw new FormatException("No CF root directory");
      rootDirectoryEntry = directoryEntries[0];
      if (rootDirectoryEntry.Name != RootEntryName)
        throw new FormatException("Invalid CF root directory name");

      firstMiniFatSectorLocation = (REGSECT)MemoryUtil.GetLeU4(cfh.FirstMiniFatSectorLocation);

      position = _stream.Position;
      try
      {
        var nextSect = firstMiniFatSectorLocation;
        while (nextSect != REGSECT.ENDOFCHAIN)
        {
          if (nextSect > REGSECT.MAXREGSECT)
          {
            break;
          }

          _stream.Position = GetSectorPosition(nextSect);
          for (int i = 0; i < (1u << cfh.SectorShift) >> 2; i++)
          {
            uint buffer;
            StreamUtil.ReadBytes(_stream, (byte*)&buffer, sizeof(uint));
            metaInfo.MiniFat.Add(MemoryUtil.GetLeU4(buffer));
          }

          nextSect = (REGSECT)metaInfo.Fat[(int)nextSect];
        }
      }
      catch (Exception)
      {
        // ignored
      }

      _stream.Position = position;

      miniStreamCutoffSize = MemoryUtil.GetLeU4(cfh.MiniStreamCutoffSize);

      var extractBlobs = new List<ExtractStream>();
      if (extractFilter != null)
      {
        var stack = new Stack<StackWalk>();
        stack.Push(new StackWalk(new string[0], rootDirectoryEntry));
        while (stack.Count > 0)
        {
          var (names, entry) = stack.Pop();
          foreach (var child in GetDirectoryEntryChildren(entry,
                     _ => _ is { ObjectType: STGTY.STGTY_STREAM } &&
                          extractFilter(MemoryUtil.ArrayMerge(names, entry.Name), _.Clsid, _.StreamSize)))
            extractBlobs.Add(new ExtractStream(MemoryUtil.ArrayMerge(names, child.Name), child.Clsid,
              Read(child.StreamSize < miniStreamCutoffSize, child.StartingSectorLocation, 0, child.StreamSize)));
          foreach (var child in GetDirectoryEntryChildren(entry, _ => _ is { ObjectType: STGTY.STGTY_STORAGE }))
            stack.Push(new StackWalk(MemoryUtil.ArrayMerge(names, child.Name), child));
        }
      }

      var hasSignature = false;

      var signatureData = new SignatureData();
      {
        var entry = TakeFirst(GetDirectoryEntryChildren(rootDirectoryEntry,
          _ => _ is { ObjectType: STGTY.STGTY_STREAM, Name: DirectoryNames.DigitalSignatureName }));
        if (entry != null)
        {
          hasSignature = true;
          if ((mode & Mode.SignatureData) == Mode.SignatureData)
            signatureData = new SignatureData(null,
              Read(entry.StreamSize < miniStreamCutoffSize, entry.StartingSectorLocation, 0, entry.StreamSize));
        }
      }

      ComputeHashInfo? computeHashInfo = null;
      if ((mode & Mode.ComputeHashInfo) == Mode.ComputeHashInfo)
      {
        var sortedDirectoryEntries = new List<DirectoryEntry>();
        foreach (var entry in directoryEntries)
          if (entry is
              {
                ObjectType: STGTY.STGTY_STREAM, StreamSize: > 0,
                Name: not (DirectoryNames.DigitalSignatureName or DirectoryNames.MsiDigitalSignatureExName)
              })
            sortedDirectoryEntries.Add(entry);
        sortedDirectoryEntries.Sort((x, y) =>
        {
          // Note(ww898): Compatibility with previous version!!!
          var a = Encoding.Unicode.GetBytes(x.Name);
          var b = Encoding.Unicode.GetBytes(y.Name);
          var size = Math.Min(a.Length, b.Length);
          for (var i = 0; i < size; i++)
            if (a[i] != b[i])
              return (a[i] & 0xFF) - (b[i] & 0xFF);
          return a.Length - b.Length;
        });

        var orderedIncludeRanges = new List<StreamRange>();
        foreach (var entry in sortedDirectoryEntries)
          Walk(entry.StreamSize < miniStreamCutoffSize, entry.StartingSectorLocation, 0, entry.StreamSize,
            _ => orderedIncludeRanges.Add(_));

        CompoundFileDirectoryEntry cfde;
        orderedIncludeRanges.Add(new StreamRange(
          GetSectorPosition(firstDirectorySectorLocation) + ((byte*)&cfde.Clsid - (byte*)&cfde), sizeof(Guid)));
        StreamRangeUtil.MergeNeighbors(orderedIncludeRanges);

        computeHashInfo = new ComputeHashInfo(0, orderedIncludeRanges, 0);
      }

      var type = GetDirectoryEntryChildren(rootDirectoryEntry, entry => entry is
      {
        ObjectType: STGTY.STGTY_STREAM,
        Name: DirectoryNames.䡀_ValidationName or DirectoryNames.䡀_TablesName or DirectoryNames.䡀_ColumnsName
        or DirectoryNames.䡀_StringPoolName or DirectoryNames.䡀_StringDataName
      }).Count >= 5
        ? FileType.Msi
        : FileType.Unknown;

      Type = type;
      HasSignature = hasSignature;
      SignatureData = signatureData;
      ExtractStreams = extractBlobs.ToArray();
      ComputeHashInfo = computeHashInfo;
      HeaderMetaInfo = metaInfo;
    }

    long GetSectorPosition(REGSECT sectorNumber, uint offset = 0)
    {
      if (sectorNumber > REGSECT.MAXREGSECT)
        throw new FormatException("Invalid CF sector number");
      if (offset >= sectorSize)
        throw new FormatException("Invalid CF sector offset");
      return (1 + (uint)sectorNumber) * sectorSize + offset;
    }

    unsafe REGSECT GetNextSector(REGSECT sectorNumber)
    {
      if (sectorNumber > REGSECT.MAXREGSECT)
        throw new FormatException("Invalid CF FAT sector number");
      _stream.Position = GetSectorPosition(diFatTable![checked((int)((uint)sectorNumber / entitiesPerSector))],
        (uint)sectorNumber % entitiesPerSector * sizeof(uint));
      uint buffer;
      StreamUtil.ReadBytes(_stream, (byte*)&buffer, sizeof(uint));
      return (REGSECT)MemoryUtil.GetLeU4(buffer);
    }

    long GetMiniSectorPosition(REGSECT miniSectorNumber, uint offset = 0)
    {
      if (miniSectorNumber > REGSECT.MAXREGSECT)
        throw new FormatException("Invalid CF mini sector number");
      if (offset >= 64)
        throw new FormatException("Invalid CF mini sector offset");
      var absoluteOffset = (uint)miniSectorNumber * 64 + offset;
      var fatSectorLocation = rootDirectoryEntry!.StartingSectorLocation;
      for (var n = absoluteOffset / sectorSize; n-- > 0;)
        fatSectorLocation = GetNextSector(fatSectorLocation);
      return GetSectorPosition(fatSectorLocation, absoluteOffset % sectorSize);
    }

    unsafe REGSECT GetNextMiniSector(REGSECT miniSectorNumber)
    {
      if (miniSectorNumber > REGSECT.MAXREGSECT)
        throw new FormatException("Invalid CF mini FAT sector number");
      var miniFatSectorLocation = firstMiniFatSectorLocation;
      for (var n = (uint)miniSectorNumber / entitiesPerSector; n-- > 0;)
        miniFatSectorLocation = GetNextSector(miniFatSectorLocation);
      _stream.Position =
        GetSectorPosition(miniFatSectorLocation, (uint)miniSectorNumber % entitiesPerSector * sizeof(uint));
      uint buffer;
      StreamUtil.ReadBytes(_stream, (byte*)&buffer, sizeof(uint));
      return (REGSECT)MemoryUtil.GetLeU4(buffer);
    }

    public static unsafe bool Is(Stream stream)
    {
      stream.Position = 0;
      CompoundFileHeader cfh;
      StreamUtil.ReadBytes(stream, (byte*)&cfh, sizeof(CompoundFileHeader));
      return MemoryUtil.ArraysEqual(cfh.HeaderSignature, Declarations.HeaderSignatureSize, ourHeaderSignature);
    }

    void Walk(bool isMiniStream, REGSECT firstSectorNumber, ulong index, ulong size, SubmitDelegate submit)
    {
      var sectorNumber = firstSectorNumber;
      var blockSize = isMiniStream ? 64 : sectorSize;
      for (var n = index / blockSize; n-- > 0;)
        sectorNumber = isMiniStream ? GetNextMiniSector(sectorNumber) : GetNextSector(sectorNumber);
      var offset = (uint)(index % blockSize);
      while (size > 0)
      {
        var readSize = size > blockSize - offset ? blockSize - offset : (uint)size;
        var position = isMiniStream
          ? GetMiniSectorPosition(sectorNumber, offset)
          : GetSectorPosition(sectorNumber, offset);
        submit(new StreamRange(position, readSize));
        offset = 0;
        size -= readSize;
        sectorNumber = isMiniStream ? GetNextMiniSector(sectorNumber) : GetNextSector(sectorNumber);
      }
    }

    byte[] Read(bool isMiniStream, REGSECT firstSectorNumber, ulong index, ulong size)
    {
      var res = new List<byte>(size > int.MaxValue ? int.MaxValue : (int)size);
      Walk(isMiniStream, firstSectorNumber, index, size, range =>
      {
        _stream.Position = range.Position;
        res.AddRange(StreamUtil.ReadBytes(_stream, checked((int)range.Size)));
      });
      return res.ToArray();
    }

    public void PutEntries(List<KeyValuePair<DirectoryEntry, byte[]>> data, REGSECT startSector, bool wipe = false)
    {
      var entries = new List<DirectoryEntry>();
      foreach (var kv in data)
      {
        entries.Add(kv.Key);
      }

      PutDirectoryEntries(entries, wipe);
      PutStreamData(data, startSector, wipe);
    }

    public void PutDirectoryEntries(List<DirectoryEntry> data, bool wipe)
    {
      var header = HeaderMetaInfo.Header;
      var nextSect = (REGSECT)header.FirstDirectorySectorLocation;
      var it = 0;
      while (nextSect != REGSECT.ENDOFCHAIN)
      {
        _stream.Position = GetSectorPosition(nextSect);
        for (int i = 0; i < Math.Min((1u << header.SectorShift) / DirectoryEntrySize, data.Count); i++)
        {
          var entry = data[it];
          if (wipe)
          {
            WipeDirectoryEntry();
          }
          else
          {
            WriteDirectoryEntry(entry);
          }
        }

        nextSect = (REGSECT)HeaderMetaInfo.Fat[(int)nextSect];
      }
    }

    private void WipeDirectoryEntry()
    {
      _stream.Write(new byte[128], 0, 128);
    }

    private unsafe void WriteDirectoryEntry(DirectoryEntry entry)
    {
      BinaryWriter writer = new BinaryWriter(_stream, Encoding.Unicode);

      fixed (Byte* ptr = entry.CompoundFileDirectoryEntry.DirectoryEntryName)
      {
        for (int i = 0; i < Declarations.DirectoryEntryNameSize; i++)
        {
          writer.Write(ptr[i]);
        }
      }

      writer.Write(entry.CompoundFileDirectoryEntry.DirectoryEntryNameLength);
      writer.Write(entry.CompoundFileDirectoryEntry.ObjectType);
      writer.Write(entry.CompoundFileDirectoryEntry.ColorFlag);
      writer.Write(entry.CompoundFileDirectoryEntry.LeftSiblingId);
      writer.Write(entry.CompoundFileDirectoryEntry.RightSiblingId);
      writer.Write(entry.CompoundFileDirectoryEntry.ChildId);
      writer.Write(entry.CompoundFileDirectoryEntry.Clsid.ToByteArray());
      writer.Write(entry.CompoundFileDirectoryEntry.StateBits);
      writer.Write(entry.CompoundFileDirectoryEntry.CreationTime);
      writer.Write(entry.CompoundFileDirectoryEntry.ModifiedTime);
      writer.Write(entry.CompoundFileDirectoryEntry.StartingSectorLocation);
      writer.Write(entry.CompoundFileDirectoryEntry.StreamSize);
    }

    private void PutStreamData(List<KeyValuePair<DirectoryEntry, byte[]>> data, REGSECT startSector, bool wipe = false)
    {
      var header = HeaderMetaInfo.Header;
      var nextSect = (REGSECT)header.FirstDirectorySectorLocation;
      var it = 0;

      while (nextSect != REGSECT.ENDOFCHAIN)
      {
        _stream.Position = GetSectorPosition(nextSect);
        for (int i = 0; i < Math.Min((1u << header.SectorShift) / DirectoryEntrySize, data.Count); i++)
        {
          var entry = data[it++];
          if (wipe)
          {
            WriteStreamData(entry.Key, new byte[entry.Value.Length], startSector);
          }
          else
          {
            WriteStreamData(entry.Key, entry.Value, startSector);
          }
        }

        nextSect = (REGSECT)HeaderMetaInfo.Fat[(int)nextSect];
      }
    }

    private void WriteStreamData(DirectoryEntry directoryEntry,
      byte[] data,
      REGSECT startSector
    )
    {
      var offset = 0;
      Walk(directoryEntry.StreamSize < miniStreamCutoffSize,
        directoryEntry.StartingSectorLocation,
        0,
        directoryEntry.StreamSize, range =>
        {
          _stream.Position = range.Position;
          ArraySegment<byte> segment = new ArraySegment<byte>(data, offset, (int)range.Size);
          _stream.Write(segment.Array, 0, (int)range.Size);
          offset += (int)range.Size;
        });
    }


    List<DirectoryEntry> GetDirectoryEntryChildren(DirectoryEntry entry, FilterDelegate filter)
    {
      if (entry.ObjectType is not (STGTY.STGTY_ROOT or STGTY.STGTY_STORAGE))
        throw new InvalidOperationException("Invalid CF storage node");
      var childrenIds = new List<DirectoryEntry>();
      if (entry.ChildId != REGSID.NOSTREAM)
      {
        var stack = new Stack<REGSID>();
        stack.Push(entry.ChildId);
        while (stack.Count > 0)
        {
          var currId = stack.Pop();
          var currEntry = directoryEntries[checked((int)currId)];
          if (filter(currEntry))
            childrenIds.Add(currEntry);
          if (currEntry.LeftSiblingId != REGSID.NOSTREAM)
            stack.Push(currEntry.LeftSiblingId);
          if (currEntry.RightSiblingId != REGSID.NOSTREAM)
            stack.Push(currEntry.RightSiblingId);
        }
      }

      return childrenIds;
    }

    public List<KeyValuePair<DirectoryEntry, byte[]>> GetEntries()
    {
      var entries = directoryEntries;

      List<KeyValuePair<DirectoryEntry, byte[]>> result = new List<KeyValuePair<DirectoryEntry, byte[]>>();
      foreach (var directoryEntry in entries)
      {
        result.Add(
          new KeyValuePair<DirectoryEntry, byte[]>(
            directoryEntry,
            Read(directoryEntry.StreamSize < miniStreamCutoffSize,
              directoryEntry.StartingSectorLocation,
              0,
              directoryEntry.StreamSize)
          )
        );
      }

      return result;
    }

    static DirectoryEntry? TakeFirst(IEnumerable<DirectoryEntry> ids)
    {
      using var en = ids.GetEnumerator();
      return en.MoveNext() ? en.Current : null;
    }

    [Flags]
    public enum Mode : uint
    {
      Default = 0x0,
      SignatureData = 0x1,
      ComputeHashInfo = 0x2
    }

    public delegate bool ExtractFilter(string[] namesFromRoot, Guid clsid, ulong size);

    public static unsafe CompoundFile Parse(Stream stream, Mode mode = Mode.Default,
      ExtractFilter? extractFilter = null)
    {
      return new CompoundFile(stream, mode, extractFilter);
    }

    private readonly struct StackWalk
    {
      public readonly string[] Name;
      public readonly DirectoryEntry Entry;

      public StackWalk(string[] name, DirectoryEntry entry)
      {
        Name = name;
        Entry = entry;
      }

      public void Deconstruct(out string[] name, out DirectoryEntry entry)
      {
        name = Name;
        entry = Entry;
      }
    }

    public sealed class DirectoryEntry
    {
      public readonly REGSID ChildId;
      public readonly REGSECT StartingSectorLocation;
      public readonly ulong StreamSize;
      public readonly CF ColorFlag;
      public readonly REGSID LeftSiblingId;
      public readonly string Name;
      public readonly Guid Clsid;
      public readonly STGTY ObjectType;
      public readonly REGSID RightSiblingId;
      public readonly CompoundFileDirectoryEntry CompoundFileDirectoryEntry;

      internal DirectoryEntry(string name,
        Guid clsid,
        STGTY objectType,
        CF colorFlag,
        REGSID leftSiblingId,
        REGSID rightSiblingId,
        REGSID childId,
        REGSECT startingSectorLocation,
        ulong streamSize,
        CompoundFileDirectoryEntry cfde)
      {
        Name = name;
        Clsid = clsid;
        ObjectType = objectType;
        ColorFlag = colorFlag;
        LeftSiblingId = leftSiblingId;
        RightSiblingId = rightSiblingId;
        ChildId = childId;
        StartingSectorLocation = startingSectorLocation;
        StreamSize = streamSize;
        CompoundFileDirectoryEntry = cfde;
      }
    }

    private delegate bool FilterDelegate(DirectoryEntry entry);

    private delegate void SubmitDelegate(StreamRange range);
  }
}