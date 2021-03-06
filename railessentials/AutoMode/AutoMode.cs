﻿// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoMode.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecoslib.Entities;
using ecoslib.Utilities.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railessentials.Analyzer;
using railessentials.Occ;
using railessentials.Plan;
using railessentials.Route;
using RouteList = railessentials.Route.RouteList;
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Local
// ReSharper disable RemoveRedundantBraces
// ReSharper disable ConvertIfStatementToNullCoalescingAssignment

namespace railessentials.AutoMode
{
    public delegate void AutoModeStarted(AutoMode sender);
    public delegate void AutoModeStopping(AutoMode sender);
    public delegate void AutoModeStopped(AutoMode sender);
    public delegate void AutoModeFailed(AutoMode sender, string reason);
    public delegate void AutoModeFauled(AutoMode sender, Exception ex);
    public delegate void AutoModeUpdate(AutoMode sender, string message);

    public partial class AutoMode
    {
        private const int RunDelayBetweenChecksMsecs = 2500;

        public event AutoModeStarted Started;
        public event AutoModeStopping Stopping;
        public event AutoModeStopped Stopped;
        public event AutoModeUpdate Update;

        private readonly ClientHandler.ClientHandler _ctx;
        private bool _isStopped = true;
        private bool _isStopping;
        private bool _isStarted;

        internal Metadata _metadata;
        internal object _metadataLock;
        private DataProvider _dataProvider;
        private DataProvider _dataProviderS88;
        private RouteList _routeList;
        private PlanField _planfield;

        private readonly object _autoModeTasksLock = new();
        private readonly List<AutoModeTaskCore> _autoModeTasks = new();

        private readonly Random _random = new Random();

        internal static int GetSecondsToWaitFallback()
        {
            return 10;
        }

        internal int GetSecondsToWait()
        {
            var cfg = _ctx?._cfg?.Cfg;
            if (cfg == null) return GetSecondsToWaitFallback();

            if (cfg?.OccWait?.WaitModeStatic != null)
            {
                if (cfg.OccWait.WaitModeStatic.Enabled)
                {
                    return cfg.OccWait.WaitModeStatic.Seconds;
                }
            }

            if (cfg?.OccWait?.WaitModeRandom != null)
            {
                if (cfg.OccWait.WaitModeRandom.Enabled)
                {
                    return _random.Next(
                        cfg.OccWait.WaitModeRandom.SecondsMin,
                        cfg.OccWait.WaitModeRandom.SecondsMax
                        );
                }
            }

            return GetSecondsToWaitFallback();
        }

        internal ClientHandler.ClientHandler GetClientHandler()
        {
            return _ctx;
        }

        public AutoMode(ClientHandler.ClientHandler ctx)
        {
            _ctx = ctx;
        }

        public bool IsStarted()
        {
            return _isStarted && !_isStopped;
        }

        public bool IsStopping()
        {
            return _isStopping && !_isStopped;
        }

        public bool IsStopped()
        {
            return _isStopped;
        }

        public void Stop()
        {
            _isStarted = false;
            _isStopping = true;

            Update?.Invoke(this, "AutoMode STOPPING");
        }

        public void StartLocomotive(int oid)
        {
            // This call is a dummy and not really used in the moment.
            // When the locomotive is not stopped 
            // (i.e. railessentials.Locomotives.Data.IsStopped := false)
            // then the locomotive will start in one of the next
            // iteration of finding a free route

            // REMARK Maybe we will change this behaviour in future, keep this method!
        }

        public void FinalizeLocomotive(int oid)
        {
            // This call do not stop the locomotive immediatelly.
            // The flag for disabling the loc is already set to
            // true (i.e. IsStopped:=true) and this will not start
            // a next round for the loc. The current trip will finish
            // until the loc reaches it's current target final block.

            // REMARK Maybe we will change this behaviour in future, keep this method!
        }

        public void StopLocomotive(int oid)
        {
            if (oid <= 0) return;

            lock (_autoModeTasksLock)
            {
                foreach (var it in _autoModeTasks)
                    it?.Cancel();
            }
        }

        public async Task HandleFeedbacks()
        {
            await Task.Run(() =>
            {
                //_ctx?.Logger?.Log.Info("+++ handle feedbacks +++");
            });
        }

        public async Task Run()
        {
            Initialize();

            await Task.Run(async () =>
            {
                _isStarted = true;
                _isStopped = false;
                _isStopping = false;

                Started?.Invoke(this);
                Update?.Invoke(this, "AutoMode START");

                StartGhostDetection();

                while (_isStopped == false)
                {
                    if (_isStopping) break;

                    var nextRouteInformation = CheckForRoutesAndAssign();
                    if (nextRouteInformation != null)
                    {
                        LogInfo($"{nextRouteInformation}");

                        var instance = AutoModeTaskBase.Create(nextRouteInformation, this);
                        instance.Finished += Instance_Finished;
                        lock (_autoModeTasksLock)
                        {
                            _autoModeTasks.Add(instance);
                        }

                        try
                        {
                            _ = Task.Run(async () => await instance.Run());
                        }
                        catch
                        {
                            // catch any exception
                            // do not bubble them up
                        }
                    }

                    System.Threading.Thread.Sleep(RunDelayBetweenChecksMsecs);
                }

                // stop all tasks, cleanup tasks and event handler
                if (_isStopping)
                {
                    //
                    // Because of issue #75 we do not cancel running tasks.
                    // If tasks are canceled during run, the locomotive will 
                    // not stop automatically in their destination.
                    // For imporving this, we will wait at this point
                    // of execution until all tasks are finished.
                    // REMARK: https://github.com/cbries/railessentials/issues/75
                    //

                    Stopping?.Invoke(this);

                    await WaitForTasks();
                }

                await StopGhostDetection();

                _isStopped = true;

                Stopped?.Invoke(this);
            });
        }

        private async Task WaitForTasks()
        {
            await Task.Run(() =>
            {
                lock (_autoModeTasksLock)
                {
                    var iMax = 0;
                    foreach (var it in _autoModeTasks)
                    {
                        if (it == null) continue;
                        ++iMax;
                    }

                    var listOfFinishedTasks = new List<int>();
                    var previousMessage = string.Empty;

                    //
                    // TODO add walltime to avoid endless waiting
                    //
                    while (true)
                    {
                        var noOfWaitingTasks = iMax - listOfFinishedTasks.Count;
                        var allTasksStopped = iMax == 0;

                        for (var j = 0; j < iMax; ++j)
                        {
                            var task = _autoModeTasks[j];
                            if (task == null) continue;
                            if (task.IsFinished)
                            {
                                task.Finished -= Instance_Finished;
                                if (!listOfFinishedTasks.Contains(j))
                                    listOfFinishedTasks.Add(j);
                            }
                            else
                            {
                                // ignore
                            }

                            var m = $"Waiting for {noOfWaitingTasks} locomotives...";
                            if (!m.Equals(previousMessage, StringComparison.OrdinalIgnoreCase))
                            {
                                _ctx?.Logger?.Log.Info(m);
                                SendAutoModeStateToClients(m);
                                previousMessage = m;
                            }

                            allTasksStopped = noOfWaitingTasks == 0;
                            if (allTasksStopped) break;

                            System.Threading.Thread.Sleep(10);
                        }

                        if (allTasksStopped) break;

                        System.Threading.Thread.Sleep(100);
                    }

                    _autoModeTasks.Clear();
                }

                CleanOccAfterStop();

                _isStopping = false;
            });
        }

        private void CleanOccAfterStop()
        {
            //
            // check periodically if the tasks are really stopped
            //
            const int maxCheckSteps = 10;
            const int checkStep = (2 * RunDelayBetweenChecksMsecs) / maxCheckSteps;
            for (var i = 0; i < maxCheckSteps; i += checkStep)
            {
                if (_isStopped) break;
                System.Threading.Thread.Sleep(checkStep);
            }

            var locOids = new List<int>();
            lock (_metadataLock)
            {
                foreach (var itOcc in _metadata?.Occ.Blocks ?? new List<OccBlock>())
                {
                    if (itOcc == null) continue;
                    locOids.Add(itOcc.Oid);
                }
            }
            locOids.ForEach(ResetRouteFor);
            CleanOcc();
        }

        private void Instance_Finished(AutoModeTaskCore sender)
        {
            lock (_autoModeTasksLock)
            {
                _autoModeTasks.Remove(sender);
            }
        }

        public void ResetRouteFor(int locOid)
        {
            if (locOid <= 0) return;
            var routeFinalName = string.Empty;
            var routeNextName = string.Empty;
            lock (_metadataLock)
            {
                var blocks = _metadata.Occ.Blocks;
                foreach (var itOccBlock in blocks)
                {
                    if (itOccBlock.Oid != locOid) continue;
                    routeFinalName = itOccBlock.RouteToFinal;
                    CleanOccBlock(itOccBlock);
                    break;
                }
            }

            var routeFinal = _routeList.GetByName(routeFinalName);
            if (routeFinal != null)
                routeFinal.Occupied = false;

            var routeNext = _routeList.GetByName(routeNextName);
            if (routeNext != null)
                routeNext.Occupied = false;

            SaveOccAndPromote();
            SaveRoutesAndPromote();

            var ar = new JArray();
            if (!string.IsNullOrEmpty(routeFinalName)) ar.Add(routeFinalName);
            if (!string.IsNullOrEmpty(routeNextName)) ar.Add(routeNextName);
            if (ar.Count > 0)
            {
                _ctx?.SendCommandToClients(new JObject
                {
                    ["command"] = "autoMode",
                    ["data"] = new JObject
                    {
                        ["command"] = "routeReset",
                        ["routeNames"] = ar
                    }
                });
            }
        }

        public void SendAutoModeStateToClients(string additionalMessage = null)
        {
            _ctx?.SendCommandToClients(new JObject
            {
                ["command"] = "autoMode",
                ["data"] = new JObject
                {
                    ["command"] = "state",
                    ["state"] = new JObject
                    {
                        ["started"] = IsStarted(),
                        ["stopping"] = IsStopping(),
                        ["stopped"] = IsStopped(),
                        ["message"] = additionalMessage ?? string.Empty
                    }
                }
            });
        }

        /// <summary>
        /// Sends the current ghost train state to all clients.
        /// In general this state should only be used, when
        /// a ghost train has been detected.
        /// </summary>
        /// <param name="ghostFbs"></param>
        private void SendAutoModeGhostFoundToClients(List<PlanItem> ghostFbs)
        {
            if (ghostFbs == null) return;
            if (ghostFbs.Count == 0) return;

            var listOfFbs = JArray.Parse(JsonConvert.SerializeObject(ghostFbs));

            _ctx?.SendCommandToClients(new JObject
            {
                ["command"] = "autoMode",
                ["data"] = new JObject
                {
                    ["command"] = "ghost",
                    ["state"] = new JObject
                    {
                        ["found"] = true,
                        ["fbs"] = listOfFbs
                    }
                }
            });
        }

        private void SendAutoModeGhostResetToClients()
        {
            _ctx?.SendCommandToClients(new JObject
            {
                ["command"] = "autoMode",
                ["data"] = new JObject
                {
                    ["command"] = "ghost",
                    ["state"] = new JObject
                    {
                        ["found"] = false
                    }
                }
            });
        }

        public void SendRouteToClients()
        {
            var routeNames = new JArray();
            lock (_autoModeTasksLock)
            {
                foreach (var it in _autoModeTasks)
                {
                    try
                    {
                        if (it == null) continue;
                        routeNames.Add(it.RouteName);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            SendAutoModeStateToClients();

            if (routeNames.Count > 0)
            {
                _ctx?.SendCommandToClients(new JObject
                {
                    ["command"] = "autoMode",
                    ["data"] = new JObject
                    {
                        ["command"] = "routeShow",
                        ["routeNames"] = routeNames
                    }
                });
            }
        }

        private NextRouteInformation CheckForRoutesAndAssign()
        {
            lock (_metadataLock)
            {
                foreach (var itOccBlock in _metadata.Occ.Blocks)
                {
                    Route.Route nextRoute = null;
                    var locomotiveObjectId = -1;

                    //
                    // has already a destination
                    // and is traveling
                    //
                    if (!string.IsNullOrEmpty(itOccBlock.FinalBlock)
                        && itOccBlock.IsTraveling)
                        continue;

                    //
                    // has already a destination
                    // but it is not traveling
                    //
                    if (!string.IsNullOrEmpty(itOccBlock.FinalBlock)
                        && !itOccBlock.IsTraveling)
                    {
                        // find route for traveling
                        nextRoute = GetUserNextRoute(itOccBlock, out locomotiveObjectId);
                    }

                    //
                    // check route for cleaning train
                    // if `nextRoute` is null no route is found OR the loc is not for cleaning
                    //
                    if (nextRoute == null)
                        nextRoute = GetCleaningNextRoute(itOccBlock, out locomotiveObjectId);

                    //
                    // the most interesting call is `GetNextRoute(..)` which
                    // is responsible for selecting the best next journey path
                    // if not already set by user decision
                    //
                    if (nextRoute == null)
                    {
                        nextRoute = GetNextRoute(itOccBlock, out locomotiveObjectId);
                        if (nextRoute == null) continue;
                    }

                    //
                    // query feedback sensors for the route
                    // in case no feedback sensors are set
                    // the route is invalid and will not be used
                    //
                    var resFb = GetFeedbacksForBlock(nextRoute.Blocks[1], out var fbEnter, out var fbIin);
                    if (!resFb)
                    {
                        LogInfo($"Route {nextRoute.Name} has no feedback sensors and will be ignored");
                        continue;
                    }

                    nextRoute.Occupied = true;

                    var fromBlock = nextRoute.Blocks[0];
                    var targetBlock = nextRoute.Blocks[1];

                    LogInfo($"Route: {nextRoute.Name} ({fromBlock.identifier} going to {targetBlock.identifier})");

                    itOccBlock.FinalBlock = targetBlock.identifier;
                    itOccBlock.RouteToFinal = nextRoute.Name;

                    // used to signal that the occ is started
                    // relevant if the user has set any route 
                    // by drag & drop of locomotives between blocks
                    itOccBlock.IsTraveling = true;

                    SaveOccAndPromote();
                    SaveRoutesAndPromote();

                    // 
                    // change switch states for the route
                    //
                    _ctx?.ApplyRouteCommandForSwitches(nextRoute.Switches);
                    if (_ctx != null && _ctx.IsSimulationMode())
                    {
                        _ctx.SaveAll();
                        _ctx?._sniffer?.TriggerDataProviderModifiedForSimulation();
                    }
                    else
                    {
                        _ctx?._sniffer?.SendCommandsToEcosStation();
                    }

                    //
                    // inform all client about a new taken route
                    //
                    _ctx?.SendCommandToClients(new JObject
                    {
                        ["command"] = "autoMode",
                        ["data"] = new JObject
                        {
                            ["command"] = "routeShow",
                            ["routeNames"] = new JArray
                            {
                                nextRoute.Name
                            }
                        }
                    });

                    // prepare route information
                    var locDataEcos = _dataProvider.GetObjectBy(locomotiveObjectId) as Locomotive;
                    var locData = _metadata.LocomotivesData.GetData(locomotiveObjectId);
                    var planField = GetPlanField(_metadata);

                    return new NextRouteInformation
                    {
                        Route = nextRoute,
                        FbEnter = planField?.Get(fbEnter),
                        FbIn = planField?.Get(fbIin),
                        LocomotiveObjectId = locomotiveObjectId,
                        Locomotive = locDataEcos,
                        LocomotivesData = locData,
                        DataProvider = _dataProvider,
                        DataProviderS88 = _dataProviderS88,
                        OccBlock = itOccBlock,
                        FromBlock = fromBlock,
                        TargetBlock = targetBlock
                    };
                }
            }

            return null;
        }

        private Feedbacks.Data GetFeedbackDataOf(string blockName, SideMarker side)
        {
            if (string.IsNullOrEmpty(blockName)) return null;
            if (side == SideMarker.None) return null;

            Feedbacks.FeedbacksData fbs;
            lock (_metadataLock)
                fbs = _metadata.FeedbacksData;
            if (fbs == null) return null;

            foreach (var itFb in fbs.Entries)
            {
                if (string.IsNullOrEmpty(itFb?.BlockId)) continue;
                if (!itFb.BlockId.StartsWith(blockName, StringComparison.OrdinalIgnoreCase)) continue;

                if (side == SideMarker.Plus)
                {
                    if (!itFb.BlockId.EndsWith("[+]", StringComparison.Ordinal))
                        continue;
                }
                else if (side == SideMarker.Minus)
                {
                    if (!itFb.BlockId.EndsWith("[-]", StringComparison.Ordinal))
                        continue;
                }

                return itFb;
            }

            return null;
        }

        internal IReadOnlyList<Feedbacks.Data> GetFeedbacksDataForBlock(string blockName)
        {
            var blocks = new List<Feedbacks.Data>();

            if (string.IsNullOrEmpty(blockName)) return blocks;
            Feedbacks.FeedbacksData fbs;
            lock (_metadataLock)
                fbs = _metadata.FeedbacksData;
            if (fbs == null) return blocks;

            foreach (var itFb in fbs.Entries)
            {
                if (string.IsNullOrEmpty(itFb?.BlockId)) continue;
                if (!itFb.BlockId.StartsWith(blockName, StringComparison.OrdinalIgnoreCase)) continue;
                blocks.Add(itFb);
            }

            return blocks;
        }

        private bool GetFeedbacksForBlock(RouteBlock block, out string fbEnter, out string fbIn)
        {
            fbEnter = string.Empty;
            fbIn = string.Empty;

            if (block == null) return false;
            if (_metadataLock == null) return false;

            Feedbacks.FeedbacksData fbs;
            lock (_metadataLock)
                fbs = _metadata.FeedbacksData;
            if (fbs == null) return false;

            foreach (var itFb in fbs.Entries)
            {
                if (string.IsNullOrEmpty(itFb?.BlockId)) continue;
                if (!itFb.BlockId.StartsWith(block.identifier, StringComparison.OrdinalIgnoreCase)) continue;

                if (block.side == SideMarker.Plus)
                {
                    if (!itFb.BlockId.EndsWith("[+]", StringComparison.Ordinal))
                        continue;
                }
                else if (block.side == SideMarker.Minus)
                {
                    if (!itFb.BlockId.EndsWith("[-]", StringComparison.Ordinal))
                        continue;
                }

                fbEnter = itFb.FbEnter;
                fbIn = itFb.FbIn;

                if (!string.IsNullOrEmpty(fbEnter)
                    && !string.IsNullOrEmpty(fbIn))
                    return true;

                fbEnter = string.Empty;
                fbIn = string.Empty;
            }

            return false;
        }

        internal void SaveFeedbacksAndPromote(bool promote = true)
        {
            if (_metadata == null || _metadataLock == null) return;
            lock (_metadataLock)
            {
                _metadata.Save(Metadata.SaveModelType.FeedbacksData);
            }
            if (promote)
                _ctx?.SendModelToClients(ClientHandler.ClientHandler.ModelType.UpdateFeedbacks);
        }

        internal void SaveOccAndPromote(bool promote = true)
        {
            if (_metadata == null || _metadataLock == null) return;
            lock (_metadataLock)
            {
                _metadata.Save(Metadata.SaveModelType.OccData);
            }
            if (promote)
                _ctx?.SendModelToClients(ClientHandler.ClientHandler.ModelType.UpdateOcc);
        }

        internal void SaveRoutesAndPromote(bool promote = true)
        {
            if (_metadata == null || _metadataLock == null) return;
            lock (_metadataLock)
            {
                _metadata.SetRoutes(_routeList);
                _metadata?.Save(Metadata.SaveModelType.RouteData);
            }
            if (promote)
                _ctx?.SendModelToClients(ClientHandler.ClientHandler.ModelType.UpdateRoutes);
        }

        internal void SaveLocomotivesAndPromote(bool promote = true)
        {
            if (_metadata == null || _metadataLock == null) return;
            lock (_metadataLock)
            {
                _metadata?.Save(Metadata.SaveModelType.LocomotivesData);
            }
            if (promote)
                _ctx?.SendModelToClients(ClientHandler.ClientHandler.ModelType.UpdateLocomotivesData);
        }

        private void Initialize()
        {
            if (_metadata != null) return;

            _metadataLock = _ctx._metadataLock;

            lock (_metadataLock)
            {
                _metadata = _ctx._metadata;
                _dataProvider = _ctx._sniffer.GetDataProvider() as DataProvider;
                _dataProviderS88 = _ctx._sniffer.GetDataProviderS88() as DataProvider;

                var nativeRouteData = _metadata.Routes.ToString();
                _routeList = JsonConvert.DeserializeObject<RouteList>(nativeRouteData);
                _planfield = GetPlanField(_metadata);
            }
        }

        public void ApplyRouteDisableState(string routeName, bool disableState)
        {
            if (string.IsNullOrEmpty(routeName)) return;

            foreach (var itRoute in _routeList)
            {
                if (itRoute == null) continue;
                if (routeName.Equals(itRoute.Name, StringComparison.OrdinalIgnoreCase))
                {
                    itRoute.IsDisabled = disableState;
                    return;
                }
            }
        }

        /// <summary>
        /// Resets all OCC states.
        /// Frees the occupied state for all routes.
        /// </summary>
        public void CleanOcc()
        {
            var occ = _metadata?.Occ;
            if (occ != null)
            {
                for (var i = 0; i < occ.Blocks.Count; ++i)
                    occ.Blocks[i] = CleanOccBlock(occ.Blocks[i]);

                _metadata.Occ = occ;

                SaveOccAndPromote();
            }

            if (_routeList != null)
            {
                foreach (var itRoute in _routeList)
                {
                    if (itRoute == null) continue;
                    itRoute.Occupied = false;
                }

                SaveRoutesAndPromote();
            }
        }

        /// <summary>
        /// Resets the block of a occ block to empty.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        internal static OccBlock CleanOccBlock(OccBlock block)
        {
            block.FinalBlock = string.Empty;
            block.RouteToFinal = string.Empty;
            block.FinalEntered = false;
            return block;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locData"></param>
        /// <param name="fbData"></param>
        /// <returns></returns>
        private static bool IsLocAllowedForTargetBlock(
            Locomotives.Data locData,
            Feedbacks.Data fbData
            )
        {
            if (locData == null) return false;
            if (fbData == null) return false;

            var locOption = locData.Settings.Where(x => x.Value && x.Key.StartsWith("Type", StringComparison.OrdinalIgnoreCase)).ToList();
            var fbOption = fbData.Settings.Where(x => x.Value && x.Key.StartsWith("Type", StringComparison.OrdinalIgnoreCase)).ToList();

            if (locOption.Count == 0) return false;
            if (fbOption.Count == 0) return false;

            foreach (var it in locOption)
            {
                foreach (var itt in fbOption)
                {
                    if (it.Key.Equals(itt.Key, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Queries the next route for the cleaning locomotive.
        /// The cleaning locomotive is small, is allowed
        /// to change the direction at any time, and
        /// is allowed to enter any block/route.
        /// </summary>
        /// <param name="occBlock"></param>
        /// <param name="locomotiveObjectId"></param>
        /// <param name="isOpposideCheck"></param>
        /// <returns></returns>
        public Route.Route GetCleaningNextRoute(
            OccBlock occBlock,
            out int locomotiveObjectId,
            bool isOpposideCheck = false
        )
        {
            locomotiveObjectId = 0;

            var occFromBlock = occBlock.FromBlock;
            if (string.IsNullOrEmpty(occFromBlock)) return null;

            var occLocOid = occBlock.Oid;
            locomotiveObjectId = occLocOid;

            var locDataEcos = _dataProvider.GetObjectBy(occLocOid) as Locomotive;
            var locData = _metadata.LocomotivesData.GetData(occLocOid);

            if (locDataEcos == null) return null;
            if (locData == null) return null;
            
            //
            // NOTE check if the OCC has waited long enough for a new start
            // 
            var lastReachedTime = occBlock.ReachedTime;
            var allowedMinimumTime = lastReachedTime.AddSeconds(occBlock.SecondsToWait);
            if (allowedMinimumTime > DateTime.Now)
                return null;

            //
            // If the loc is no cleaning vehicle, just leave this part
            //
            if (!locData.IsCleaner) return null;

            //
            // do not start any loc on any route when the loc is locked (i.e. not allowed to start)
            //
            if (locData.IsLocked) return null;

            //
            // do not start any loc on any route when the loc is "IsStopped:=true"
            //
            // REMARK we have to distinguish IsLocked and IsStopped somehow
            //
            if (locData.IsStopped) return null;

            var sideToLeave = locData.EnterBlockSide.IndexOf("+", StringComparison.Ordinal) != -1
                ? SideMarker.Minus
                : SideMarker.Plus;

            var originalSideEntered = sideToLeave == SideMarker.Minus
                ? SideMarker.Plus
                : SideMarker.Minus;

            RouteList routesFrom;

            if (!isOpposideCheck)
            {
                routesFrom = _routeList.GetRoutesWithFromBlock(occFromBlock, sideToLeave, true);
            }
            else
            {
                var r = CheckOpposide(_routeList,
                    occBlock, sideToLeave, originalSideEntered,
                    locDataEcos, locData, out routesFrom, true);
                if (!r) return null;
            }

            // 
            // filter by "BlockEnabled" option
            //
            var routesFrom1 = FilterByBlockEnabled(routesFrom, sideToLeave, locData);

            //
            // check if routes have target blocks which are locked by other blocks
            // if fromBlock is referenced, the target is allowed
            //
            var routesFrom2 = FilterByBlockedRoutes(routesFrom1, sideToLeave);

            //
            // filter routes which are occupied or locked
            //
            var routesFromNotOccupied = routesFrom2.FilterNotOccupiedOrLocked(_metadata.Occ);

            //
            // filter routes if any accessory is in "maintenance" mode
            //
            var routesNoMaintenance = routesFromNotOccupied.FilterSwitchesMaintenance(_metadata.Metamodel);

            //
            // filter all routes which cross occupied routes
            //
            var routesNoCross = routesNoMaintenance.FilterNoCrossingOccupied(_routeList);

            if (isOpposideCheck)
            {
                if (routesNoCross.Count == 0)
                    return null;

                var idx = GetRndBetween(routesNoCross.Count);
                var r = routesNoCross[idx];
                return r;
            }

            Route.Route nextRoute;
            if (routesNoCross.Count == 0)
            {
                nextRoute = GetCleaningNextRoute(occBlock, out _, true);
            }
            else
            {
                var idx = GetRndBetween(routesNoCross.Count);
                nextRoute = routesNoCross[idx];
            }

            if (nextRoute == null)
                LogInfo($"No route available for Locomotive({locDataEcos.Name}).");

            return nextRoute;
        }

        /// <summary>
        /// Finds the best route to reach a block
        /// which is targeting by the user itself
        /// via drag & drop of a locomotive object
        /// between blocks in the web ui.
        /// </summary>
        /// <param name="occBlock"></param>
        /// <param name="locomotiveObjectId"></param>
        /// <returns></returns>
        public Route.Route GetUserNextRoute(
            OccBlock occBlock,
            out int locomotiveObjectId)
        {
            locomotiveObjectId = 0;

            var occFromBlock = occBlock.FromBlock;
            if (string.IsNullOrEmpty(occFromBlock)) return null;

            var occFromFinal = occBlock.FinalBlock;
            if (string.IsNullOrEmpty(occFromFinal)) return null;

            var occLocOid = occBlock.Oid;
            var locDataEcos = _dataProvider.GetObjectBy(occLocOid) as Locomotive;
            var locData = _metadata.LocomotivesData.GetData(occLocOid);

            if (locDataEcos == null) return null;
            if (locData == null) return null;

            //
            // do not start any loc on any route when the loc is locked (i.e. not allowed to start)
            //
            if (locData.IsLocked) return null;

            //
            // do not start any loc on any route when the loc is "IsStopped:=true"
            //
            // REMARK we have to distinguish IsLocked and IsStopped somehow
            //
            if (locData.IsStopped) return null;

            var sideToLeave = locData.EnterBlockSide.IndexOf("+", StringComparison.Ordinal) != -1
                ? SideMarker.Minus
                : SideMarker.Plus;

            var routesFrom2 = _routeList.GetRoutesWithFromBlock(occFromBlock, sideToLeave, true);

            foreach (var route in routesFrom2)
            {
                if (route == null) continue;
                if (route.IsDisabled) continue;
                var name = route.Name;
                if (string.IsNullOrEmpty(name)) continue;
                if (name.IndexOf($"_{occFromFinal}", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    locomotiveObjectId = occLocOid;

                    return route;
                }
            }

            return null;
        }

        public Route.Route GetNextRoute(
            OccBlock occBlock,
            out int locomotiveObjectId,
            bool isOpposideCheck = false)
        {
            locomotiveObjectId = 0;

            var occFromBlock = occBlock.FromBlock;
            if (string.IsNullOrEmpty(occFromBlock)) return null;

            var occLocOid = occBlock.Oid;
            locomotiveObjectId = occLocOid;
            var locDataEcos = _dataProvider.GetObjectBy(occLocOid) as Locomotive;
            Locomotives.Data locData;
            lock (_metadataLock)
                locData = _metadata.LocomotivesData.GetData(occLocOid);

            if (locDataEcos == null) return null;
            if (locData == null) return null;

            //
            // NOTE check if the OCC has waited long enough for a new start
            // 
            var lastReachedTime = occBlock.ReachedTime;
            var allowedMinimumTime = lastReachedTime.AddSeconds(occBlock.SecondsToWait);
            if (allowedMinimumTime > DateTime.Now)
                return null;

            //
            // do not start any loc on any route when the loc is locked (i.e. not allowed to start)
            //
            if (locData.IsLocked) return null;

            //
            // do not start any loc on any route when the loc is "IsStopped:=true"
            //
            // REMARK we have to distinguish IsLocked and IsStopped somehow
            //
            if (locData.IsStopped) return null;

            var sideToLeave = locData.EnterBlockSide.IndexOf("+", StringComparison.Ordinal) != -1
                ? SideMarker.Minus
                : SideMarker.Plus;

            var originalSideEntered = sideToLeave == SideMarker.Minus
                ? SideMarker.Plus
                : SideMarker.Minus;

            RouteList routesFrom;

            if (!isOpposideCheck)
            {
                routesFrom = _routeList.GetRoutesWithFromBlock(occFromBlock, sideToLeave, true);
            }
            else
            {
                var r = CheckOpposide(_routeList,
                    occBlock, sideToLeave, originalSideEntered,
                    locDataEcos, locData, out routesFrom, false);
                if (!r) return null;
            }

            // 
            // filter by "BlockEnabled" option
            //
            var routesFrom1 = FilterByBlockEnabled(routesFrom, sideToLeave, locData);

            //
            // filter routes by allowed options, e.g. "mainline", "intercity", ...
            // 
            var routesFrom2 = FilterByAllowedOptions(routesFrom1, sideToLeave, locData);

            //
            // check if routes have target blocks which are locked by other blocks
            // if fromBlock is referenced, the target is allowed
            //
            var routesFrom3 = FilterByBlockedRoutes(routesFrom2, sideToLeave);

            //
            // filter routes/blocks which are not allowed for the current locomotive
            //
            RouteList routesFromFiltered;
            lock (_metadataLock)
                routesFromFiltered = routesFrom3.FilterBy(locDataEcos, locData, _metadata.FeedbacksData);

            //
            // filter routes which are occupied or locked
            //
            RouteList routesFromNotOccupied;
            lock (_metadataLock)
                routesFromNotOccupied = routesFromFiltered.FilterNotOccupiedOrLocked(_metadata.Occ);

            //
            // filter routes if any accessory is in "maintenance" mode
            //
            RouteList routesNoMaintenance;
            lock (_metadataLock)
                routesNoMaintenance = routesFromNotOccupied.FilterSwitchesMaintenance(_metadata.Metamodel);

            //
            // filter all routes which cross occupied routes
            //
            var routesNoCross = routesNoMaintenance.FilterNoCrossingOccupied(_routeList);

            if (isOpposideCheck)
            {
                if (routesNoCross.Count == 0)
                    return null;

                var idx = GetRndBetween(routesNoCross.Count);
                var r = routesNoCross[idx];
                return r;
            }

            Route.Route nextRoute;
            if (routesNoCross.Count == 0)
            {
                nextRoute = GetNextRoute(occBlock, out _, true);
            }
            else
            {
                var idx = GetRndBetween(routesNoCross.Count);
                nextRoute = routesNoCross[idx];
            }

            if (nextRoute == null)
                LogInfo($"No route available for Locomotive({locDataEcos.Name}).");

            return nextRoute;
        }

        #region Helper

        internal void LogInfo(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _ctx?.Logger?.Log?.Info($"{msg}");
            _ctx?.SendDebugMessage($"{msg}");
        }

        private static int GetRndBetween(int max = int.MaxValue, int from = 0)
        {
            var r = new Random(Guid.NewGuid().GetHashCode());
            return r.Next(from, max);
        }

        private static PlanField GetPlanField(Metadata metadata)
        {
            var metamodel = metadata?.Metamodel;
            if (metamodel == null) return null;
            var planfield = JsonConvert.DeserializeObject<Dictionary<string, PlanField>>(metamodel.ToString(Formatting.None));
            return planfield?["planField"];
        }

        #endregion
    }
}
