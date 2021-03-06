﻿using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using railessentials.Locomotives;
using railessentials.LocomotivesDuration;
using Data = railessentials.LocomotivesDuration.Data;

namespace TrackPlanerTest
{
    [TestClass]
    public class TestDurations
    {
        [TestMethod]
        public void TestAddDuration()
        {
            var instance = new DurationsData(null);
            var data1004 = instance.GetData(1004);
            data1004.Should().BeNull();
            instance.AddDecelerateDuration(1004, "B01[+]", DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(15)).Should().BeTrue();
            var jsonObj = instance.ToJsonString();
            jsonObj.IndexOf("B01[+]", StringComparison.OrdinalIgnoreCase).Should().BeGreaterThan(-1);
        }

        [TestMethod]
        public void TestDecelerationMax()
        {
            var instance = new DurationsData(null);
            var data1004 = instance.GetData(1004);
            data1004.Should().BeNull();
            const string blockName = "B01[+]";
            for(var i = 0; i < 100; ++i)
                instance.AddDecelerateDuration(1004, blockName, DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(15)).Should().BeTrue();
            instance.Entries.Count.Should().Be(1);
            instance.GetNoOfDecelerationsEntries(1004, blockName).Should().Be(Data.MaxDecelerateEntries);
            instance.CleanupDeceleration(1004, blockName).Should().BeTrue();
            instance.GetNoOfDecelerationsEntries(1004, blockName).Should().Be(0);
            instance.Remove(1004).Should().BeTrue();
            instance.Entries.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestDecelerationAverage()
        {
            var instance = new DurationsData(null);
            var data1004 = instance.GetData(1004);
            data1004.Should().BeNull();
            const string blockName = "B01[+]";
            for (var i = 0; i < 100; ++i)
                instance.AddDecelerateDuration(1004, blockName, DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(15)).Should().BeTrue();
            var avr = instance.GetAverageDecelerationSeconds(1004, blockName);
            avr.Should().BeInRange((15 - 0.001), 15 + 0.001);

            instance.GetAverageDecelerationSeconds(1000, "", 1337).Should().Be(1337);
            instance.GetAverageDecelerationSeconds(1004, "", 1337).Should().Be(1337);
            instance.GetAverageDecelerationSeconds(1004, "--", 1337).Should().Be(1337);
        }

        [TestMethod]
        public void TestDecelerationJson()
        {
            var instance = new DurationsData(null);
            var data1004 = instance.GetData(1004);
            data1004.Should().BeNull();
            const string blockName = "B01[+]";
            for (var i = 0; i < 100; ++i)
                instance.AddDecelerateDuration(1004, blockName, DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(15)).Should().BeTrue();

            var json = instance.ToJsonDecelerationDurations();
            var jsonObject = JsonConvert.DeserializeObject<AverageDurations>(json);
            jsonObject.ContainsKey("1004").Should().BeTrue();
            var it1004 = jsonObject["1004"];
            it1004.Should().NotBeNull();
            it1004.Count.Should().Be(1);
            var duration = jsonObject.GetDuration(1004, blockName);
            duration.Should().BeInRange(15 - 0.001, 15 + 0.001);

            jsonObject.ContainsKey("1000").Should().BeFalse();
        }

        [TestMethod]
        [DeploymentItem("Testmodels", "Testmodels")]
        public void TestDurationWithSpeedCurve()
        {
            var testSeconds = 5.0;

            var instance = new DurationsData(null);
            var data1004 = instance.GetData(1004);
            data1004.Should().BeNull();
            const string blockName = "B01[+]";
            for (var i = 0; i < 100; ++i)
                instance.AddDecelerateDuration(1004, blockName, DateTime.Now, DateTime.Now + TimeSpan.FromSeconds(testSeconds)).Should().BeTrue();

            var avrDuration = instance.GetAverageDecelerationSeconds(1004, blockName);
            avrDuration.Should().BeInRange((testSeconds - 0.001), testSeconds + 0.001);

            var locdata = new LocomotivesData(null);
            locdata.Load(@"Testmodels\locomotives.speedCurve.json").Should().BeTrue();
            var locdata1004 = locdata.GetData(1004);
            locdata1004.Should().NotBeNull();
            var speedCurve = locdata1004.SpeedCurve;
            speedCurve.Should().NotBeNull();
            speedCurve.MaxTime.Should().Be(10);
        }
    }
}
