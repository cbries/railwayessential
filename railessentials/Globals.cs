﻿// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Globals.cs

using System;
using System.Collections.Generic;
using System.IO;

namespace railessentials
{
    public static class Globals
    {
        public static string Workspace = "";
        public static string RootWorkspace = "../../../Workspaces";

        public static string ConstDefaultEnterSide = "'+' Side";

        public const string Author = "Dr. Christian Benjamin Ries";
        public const string Company = Author + " -- www.christianbenjaminries.de";
        public const string ApplicationName = "RailEssentials";
        public const string ApplicationDescription = "RailEssentials is a software for controlling your Model Trains especially when ESU's ECoS 50210/50200 is used.";

        public const int DccKickStartM14 = 3;
        public const int DccKickStartM28 = 3;
        public const int DccKickStartM128 = 5;
        public const int DccKickStartDelayMsecs = 125;

        public static Dictionary<string, string> GetCfgDataPath()
        {
            var dict = new Dictionary<string, string>
            {
                {"Metamodel", Path.Combine(RootWorkspace, Workspace, "metamodel.json") },
                {"Routes", Path.Combine(RootWorkspace, Workspace, "routes.json") },
                {"Occ", Path.Combine(RootWorkspace, Workspace, "occ.json") },
                {"Locomotives", Path.Combine(RootWorkspace, Workspace, "locomotives.json") },
                {"LocomotivesDurations", Path.Combine(RootWorkspace, Workspace, "locomotives.duration.json")},
                {"FbEvents", Path.Combine(RootWorkspace, Workspace, "fbevents.json") },
                {"Statistics", Path.Combine(RootWorkspace, Workspace, "statistics.json") }
            };

            return dict;
        }

        public static string GetCfgDataPath(string name)
        {
            var dict = GetCfgDataPath();

            foreach (var it in dict)
            {
                var n = it.Key?.Trim();
                if (string.IsNullOrEmpty(n)) continue;
                if (n.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return it.Value;
            }

            return dict.ContainsKey(name) ? dict[name] : string.Empty;
        }
    }
}
