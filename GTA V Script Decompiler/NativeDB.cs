using Decompiler.Hooks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Decompiler
{
    internal class NativeDBParam
    {
        public string type { get; set; }
        public string name { get; set; }

        public bool AutoName;
        public Types.TypeInfo TypeInfo;
    }

    internal class NativeDBEntry
    {
        public string name { get; set; }
        public string jhash { get; set; }
        public string comment { get; set; }
        public List<NativeDBParam> @params { get; set; }
        public string return_type { get; set; }
        public string build { get; set; }
        public string[]? old_names { get; set; }
        public bool? unused { get; set; }

        public string @namespace;

        public Types.TypeInfo ReturnTypeInfo;

        public NativeHook? NativeHook = null;

        public NativeDBParam? GetParam(int index)
        {
            return index >= @params.Count ? null : @params[index];
        }

        public Types.TypeInfo GetParamType(int index)
        {
            return index > @params.Count - 1 ? Types.UNKNOWN : @params[index].TypeInfo;
        }

        public void SetParamType(int index, Types.TypeInfo type)
        {
            NativeDBParam param = @params[index];
            param.type = type.SingleName;
            param.TypeInfo = type;
            @params[index] = param;
        }

        public Types.TypeInfo GetReturnType()
        {
            return ReturnTypeInfo;
        }

        public void SetReturnType(Types.TypeInfo type)
        {
            return_type = type.SingleName;
            ReturnTypeInfo = type;
        }
    }

    internal class NativeDB
    {
        private Dictionary<string, Dictionary<string, NativeDBEntry>> data;
        private Dictionary<ulong, NativeDBEntry> entries;
        private static readonly string[] reservedNames = { "int", "float", "bool", "if", "else", "while", "do", "switch", "for", "break" };

        public static bool CanBeUsedAsAutoName(string param)
        {
            if (param.StartsWith("p") && param.Length < 3)
                return false;

            if (param.Contains("unk"))
                return false;

            //if (param == "toggle" || param == "enable")
            //    return false; // TODO

            if (param == "string")
                return false;

            foreach (Types.TypeInfo type in Types.typeInfos)
                if (type.AutoName == param)
                    return false;

            return true;
        }

        public static string SanitizeAutoName(string name)
        {
            return reservedNames.Contains(name) ? "_" + name : name;
        }

        public void LoadData()
        {
            string file = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "natives.json");

            data = File.Exists(file)
                ? JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, NativeDBEntry>>>(File.ReadAllText(file))
                : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, NativeDBEntry>>>(Properties.Resources.native_db_json);

            entries = new();

            NativeTypeOverride.Initialize();

            foreach (KeyValuePair<string, Dictionary<string, NativeDBEntry>> ns in data)
            {
                foreach (KeyValuePair<string, NativeDBEntry> native in ns.Value)
                {
                    NativeDBEntry entry = native.Value;
                    NativeTypeOverride.Visit(ref entry);
                    entry.@namespace = ns.Key;

                    foreach (NativeDBParam param in entry.@params)
                    {
                        param.TypeInfo = Types.GetFromName(param.type);
                        param.AutoName = CanBeUsedAsAutoName(param.name);
                    }

                    entry.ReturnTypeInfo = Types.GetFromName(entry.return_type);

                    entries[Convert.ToUInt64(native.Key, 16)] = entry;
                }
            }

            foreach (NativeHook hook in Program.NativeHooks)
            {
                IEnumerable<KeyValuePair<ulong, NativeDBEntry>> matching = entries.Where(p => p.Value.name == hook.Native);

                if (!matching.Any())
                    continue;

                matching.Single().Value.NativeHook = hook;
            }
        }

        public NativeDBEntry? GetEntry(ulong hash)
        {
            return entries.ContainsKey(hash) ? entries[hash] : null;
        }
    }
}
