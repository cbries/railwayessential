﻿// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: JsonUtilities.cs

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Utilities
{
    public static class JsonUtilities
    {
        public static int GetInt(this JObject obj, string key, int def = 0)
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                if (int.TryParse(obj[key].ToString(), out var v))
                    return v;
            }
            return def;
        }

        public static string GetString(this JObject obj, string key, string def = "")
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                if (obj[key] == null)
                    return def;

                var v = obj[key]?.ToString();
                if (v != null && v.Trim().Equals("null"))
                    return null;
                return v;
            }
            return def;
        }

        public static List<string> GetStringList(this JObject obj, string key)
        {
            if (obj?[key] == null) return null;
            var ar = obj[key] as JArray;
            if (ar == null) return null;
            var v = new List<string>();
            foreach (var it in ar)
            {
                if (it == null) continue;
                v.Add(it.ToString());
            }
            return v;
        }


        public static bool GetBool(this JObject obj, string key, bool def = false)
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                var v = obj[key]?.ToString();
                if (bool.TryParse(v, out var vv))
                    return vv;
            }
            return def;
        }
    }
}
