﻿// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RouterListExtensions.cs

using System;
using System.Collections.Generic;
using ecoslib.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Feedbacks;
using railessentials.Plan;

namespace railessentials.Route
{
    public static class RouterListExtensions
    {
        private static Dictionary<string, bool> GetDefaultSettings()
        {
            return new()
            {
                { "TypeOthers", true }
            };
        }

        public static RouteList FilterNotOccupiedOrLocked(this RouteList routeList, Occ.OccData occData)
        {
            var res = new RouteList();

            foreach(var it in routeList)
            {
                var fromBlock = it.Blocks[1].identifier;
                if(string.IsNullOrEmpty(fromBlock)) continue;

                // 
                // if route is occupied or locked
                // do not use it
                //
                if (it.Occupied) continue;
                if (it.Locked) continue;

                //
                // check if any target block of a route is
                // listed in the occupied list, if yes
                // the route is not open to drive-threw
                //
                var blockIsOccupied = false;
                foreach(var itt in occData.Blocks)
                {
                    blockIsOccupied =
                        itt.FromBlock.Equals(fromBlock, StringComparison.OrdinalIgnoreCase)
                        || itt.FinalBlock.Equals(fromBlock, StringComparison.OrdinalIgnoreCase);
                    if (blockIsOccupied) break;
                }
                if (blockIsOccupied) continue;
                
                res.Add(it);
            }

            return res;
        }

        /// <summary>
        /// Filters the routes where the current locomotive
        /// is not allowed to enter the block.
        /// </summary>
        /// <param name="routeList"></param>
        /// <param name="locDataEcos"></param>
        /// <param name="locData"></param>
        /// <param name="feedbacks"></param>
        /// <returns></returns>
        public static RouteList FilterBy(
            this RouteList routeList,
            Locomotive locDataEcos,
            Locomotives.Data locData,
            FeedbacksData feedbacks
        )
        {
            var res = new RouteList();

            foreach (var it in routeList)
            {
                var targetBlock = it.Blocks[1];
                var targetBlockIdentifier = targetBlock.identifier;
                var targetEnterSide = targetBlock.side;

                var targetBlockData = feedbacks.GetByBlockId(targetBlockIdentifier, targetEnterSide);
                if (targetBlockData == null) continue;

                //
                // check if the target block denies entering by the current locomotive
                //
                var useIsDenied = false;
                foreach (var itLoc in targetBlockData.DeniedLocomotives)
                {
                    useIsDenied = itLoc.Id.Equals(locDataEcos.Name, StringComparison.Ordinal);
                    if (useIsDenied) break;
                }
                if (useIsDenied) continue;

                //
                // check if the locomotive type is not allowed to enter the target block
                //
                targetBlockData.Settings ??= GetDefaultSettings();
                if (targetBlockData.Settings.Count == 0) continue;
                var enterIsAllowed = false;
                foreach (var itLocSetting in locData.Settings)
                {
                    var name = itLocSetting.Key;
                    var state = itLocSetting.Value;

                    if (targetBlockData.Settings.ContainsKey(name))
                    {
                        enterIsAllowed = state == targetBlockData.Settings[name];
                        if (enterIsAllowed) break;
                    }
                }
                if (!enterIsAllowed) continue;

                res.Add(it);
            }

            return res;
        }

        public static RouteList FilterNoCrossingOccupied(this RouteList baseList, RouteList allRoutes)
        {
            if (allRoutes == null) return baseList;
            if (allRoutes.Count == 0) return baseList;

            var res = new RouteList();

            foreach(var it in baseList)
            {
                if (string.IsNullOrEmpty(it?.Name)) continue;
                var crossingRoutes = allRoutes.GetCrossingRoutesOf(it);
                if(crossingRoutes.Count == 0)
                {
                    if(!res.Contains(it))
                        res.Add(it);
                }
                else
                {
                    var isOccupied = false;
                    foreach (var itt in crossingRoutes)
                    {
                        if (itt == null) continue;
                        isOccupied = itt.Occupied;
                        if (isOccupied) break;
                    }

                    if (isOccupied) continue;

                    if (!res.Contains(it))
                        res.Add(it);
                }
            }

            return res;
        }

        public static bool Contains(this RouteList routeList, Route route)
        {
            var routeName = route?.Name;
            if (string.IsNullOrEmpty(routeName)) return false;
            foreach(var it in routeList)
            {
                if (string.IsNullOrEmpty(it?.Name)) continue;
                if (it.Name.Equals(routeName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static RouteList FilterSwitchesMaintenance(
            this RouteList routeList,
            JObject metamodel
            )
        {
            if (routeList == null) return routeList;
            if (routeList.Count == 0) return routeList;

            var planField = metamodel?["planField"] as JObject;
            if (planField == null) return routeList;

            var res = new RouteList();

            foreach (var it in routeList)
            {
                if (it == null) continue;

                var addToResult = true;

                foreach(var itSwitch in it.Switches)
                {
                    if (itSwitch == null) continue;
                    var key = $"{itSwitch.x}x{itSwitch.y}";
                    if(planField[key] != null)
                    {
                        var itemObj = JsonConvert.DeserializeObject<PlanItem>(planField[key].ToString());
                        if (itemObj == null) continue;
                        if(itemObj.IsMaintenance)
                        {
                            addToResult = false;
                            break;
                        }
                    }
                }

                if(addToResult)
                    res.Add(it);
            }

            return res;
        }
    }
}