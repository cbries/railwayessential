﻿// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AutoModeTaskBase.Decelerate.cs

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ecoslib.Entities;
using railessentials.Locomotives;

namespace railessentials.AutoMode
{
    public partial class AutoModeTaskBase
    {
        private async Task DecelerateLocomotiveCurve(
            Locomotive ecosLoc,
            SpeedCurve speedCurve,
            int maxTime = -1, // TODO measure real time between FB enter and FB in
            Func<bool> hasToBeCanceled = null
        )
        {
            Trace.WriteLine("DecelerateLocomotiveCurve()");

            var currentSpeed = (float)ecosLoc.Speedstep;
            var maxSpeed = speedCurve.MaxSpeed;
            var minSpeed = ecosLoc.GetNumberOfSpeedsteps() <= 28 ? 2 : 10;
            var timeSteps = (speedCurve.MaxTime / (float) maxSpeed) * 1000.0;

            await Task.Run(() =>
            {
                // 
                // IMPORTANT NOTE:
                // do not slow down the locomotive completly
                // we still have to reach the fbIn, when reached
                // the train will stop right at this moment
                //

                var idx = -1;
                for(var i = 0; i < speedCurve.Steps.Count - 1; ++i)
                {
                    var s0 = speedCurve.Steps[i];
                    var s1 = speedCurve.Steps[i + 1];
                    if(currentSpeed >= s0.Speed && currentSpeed < s1.Speed)
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx == -1) 
                    idx = speedCurve.Steps.Count - 1;
                
                for (var i = idx; i > minSpeed; --i)
                {
                    var nextSpeed = speedCurve.Steps[i];

                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, (int)nextSpeed.Speed);

                    if (IsCanceled())
                    {
                        Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, 0);

                        break;
                    }

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;

                    System.Threading.Thread.Sleep((int)timeSteps);

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;
                }

            }, CancelSource.Token);
        }

        private async Task DecelerateLocomotive(
            Locomotive ecosLoc,
            int maxSecsToStop = 10,
            Func<bool> hasToBeCanceled = null)
        {
            Trace.WriteLine("DecelerateLocomotive()");

            var currentSpeed = (float)ecosLoc.Speedstep;
            var deltaSpeedSteps = currentSpeed / maxSecsToStop;
            var minSpeed = ecosLoc.GetNumberOfSpeedsteps() <= 28 ? 2 : 10;

            await Task.Run(() =>
            {
                // 
                // IMPORTANT NOTE:
                // do not slow down the locomotive completly
                // we still have to reach the fbIn, when reached
                // the train will stop right at this moment
                //

                for (var i = currentSpeed; i > minSpeed; i -= deltaSpeedSteps)
                {
                    Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, (int)i);

                    if (IsCanceled())
                    {
                        Ctx.GetClientHandler()?.LocomotiveChangeSpeedstep(ecosLoc, 0);

                        break;
                    }

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;

                    var sleepMs = (maxSecsToStop * 1000) / deltaSpeedSteps;
                    System.Threading.Thread.Sleep((int)sleepMs);

                    if (hasToBeCanceled != null)
                        if (hasToBeCanceled())
                            break;
                }

            }, CancelSource.Token);
        }
    }
}