﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Decompiler
{
    internal class Crossmap
    {
        public Dictionary<ulong, ulong> TranslationTable = new();

        public ulong TranslateHash(ulong hash)
        {
            TranslationTable.TryGetValue(hash, out hash);
            return hash;
        }

        public Crossmap()
            : base()
        {
            StreamReader sr;
            sr = new StreamReader(new MemoryStream(Properties.Resources.CrossmapFile));

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line.Length > 1)
                {
                    var val = line.Remove(line.IndexOfAny(new char[] { ':', '=', ',' }));
                    var nat = line[(val.Length + 1)..];

                    if (val.StartsWith("0x"))
                        val = val[2..];

                    if (nat.StartsWith("0x"))
                        nat = nat[2..];

                    if (ulong.TryParse(val.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var newer))
                    {
                        if (ulong.TryParse(nat.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var older))
                        {
                            TranslationTable.Add(newer, older);
                        }
                    }
                }
            }

            sr.Close();
        }
    }
}