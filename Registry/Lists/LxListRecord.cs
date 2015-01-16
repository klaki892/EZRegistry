﻿using System;
using System.Collections.Generic;
using System.Text;
using NFluent;
using Registry.Other;

// namespaces...

namespace Registry.Lists
{
    // internal classes...
    internal class LxListRecord : IListTemplate, IRecordBase
    {
        // private fields...
        private readonly int _size;
        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="LFListRecord" /> or <see cref="LHListRecord" />  class.
        ///     <remarks>The signature determines how the hash is calculated/verified</remarks>
        /// </summary>
        /// <param name="rawBytes"></param>
        /// <param name="relativeOffset"></param>
        public LxListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);
            IsFree = _size > 0;

            NumberOfEntries = BitConverter.ToUInt16(rawBytes, 0x06);

            Signature = Encoding.ASCII.GetString(rawBytes, 4, 2);

            Check.That(Signature).IsOneOfThese("lh", "lf");

            Offsets = new Dictionary<uint, string>();

            var index = 0x8;
            var counter = 0;

            while (counter < NumberOfEntries)
            {
                if (index >= rawBytes.Length)
                {
                    // i have seen cases where there isnt enough data, so get what we can
                    break;
                }
                var os = BitConverter.ToUInt32(rawBytes, index);
                index += 4;

                var hash = string.Empty;

                if (Signature == "lf")
                {
                    //first 4 chars of string
                    hash = Encoding.ASCII.GetString(rawBytes, index, 4);
                }
                else
                {
                    //numerical hash
                    hash = BitConverter.ToUInt32(rawBytes, index).ToString();
                }

                index += 4;

                Offsets.Add(os, hash);

                counter += 1;
            }
        }

        /// <summary>
        ///     A dictionary of relative offsets and hashes to other records
        ///     <remarks>The offset is the key and the hash value is the value</remarks>
        /// </summary>
        public Dictionary<uint, string> Offsets { get; private set; }

        public bool IsFree { get; private set; }
        public bool IsReferenced { get; internal set; }
        public int NumberOfEntries { get; private set; }
        public byte[] RawBytes { get; private set; }
        public long RelativeOffset { get; private set; }
        public string Signature { get; private set; }

        public int Size
        {
            get { return Math.Abs(_size); }
        }

        // public properties...
        public long AbsoluteOffset
        {
            get { return RelativeOffset + 4096; }
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Math.Abs(_size)));
            sb.AppendLine(string.Format("Relative Offset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("Absolute Offset: 0x{0:X}", AbsoluteOffset));
            sb.AppendLine(string.Format("Signature: {0}", Signature));

            sb.AppendLine();

            sb.AppendLine(string.Format("Is Free: {0}", IsFree));

            sb.AppendLine();

            sb.AppendLine(string.Format("Number Of Entries: {0:N0}", NumberOfEntries));
            sb.AppendLine();

            var i = 0;

            foreach (var offset in Offsets)
            {
                sb.AppendLine(string.Format("------------ Offset/hash record #{0} ------------", i));
                sb.AppendLine(string.Format("Offset: 0x{0:X}, Hash: {1}", offset.Key, offset.Value));
                i += 1;
            }
            sb.AppendLine();
            sb.AppendLine("------------ End of offsets ------------");
            sb.AppendLine();

            if (IsFree)
            {
                sb.AppendLine(string.Format("Raw Bytes: {0}", BitConverter.ToString(RawBytes)));
                sb.AppendLine();
            }


            return sb.ToString();
        }
    }
}