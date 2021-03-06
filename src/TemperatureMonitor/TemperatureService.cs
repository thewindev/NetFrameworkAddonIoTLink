﻿using System;
using System.Linq;
using System.Timers;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.HomeAssistant;
using LibreHardwareMonitor.Hardware;
using Timer = System.Timers.Timer;

namespace TemperatureMonitor
{
    public class TemperatureService: ServiceAddon
    {
        private Timer _monitorTimer;
        private string _temperatureTopic;

        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);
            _temperatureTopic = "Stats/CPU/Temperature";
            GetManager().PublishDiscoveryMessage(this, _temperatureTopic, "CPU", new HassDiscoveryOptions
            {
                Id = "Temperature",
                Unit = "°C",
                Name = "Temperature",
                Component = HomeAssistantComponent.Sensor,
                Icon = "mdi:thermometer"
            });
            _monitorTimer = new Timer();
            _monitorTimer.Interval = 10000;
            _monitorTimer.Elapsed += TimerElapsed;
            _monitorTimer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var averageTemperature = GetTemperature();
                LoggerHelper.Info($"Sending {averageTemperature} celsius");
                GetManager().PublishMessage(this, _temperatureTopic, averageTemperature.ToString());
            }
            catch (Exception exception)
            {
                LoggerHelper.Error("Failed to send temperature " + exception);
            }
        }

        public static int GetTemperature()
        {
            Computer computer = new Computer
            {
                IsCpuEnabled = true
            };

            computer.Open();
            try
            {
                var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                var temperatureSensors = cpu?.Sensors.Where(s => s.SensorType == SensorType.Temperature).ToList();
                var cpuTempSensor = temperatureSensors?.FirstOrDefault(t => t.Name.ToLower() == "core average") ??
                                    temperatureSensors?.First();
                if (cpuTempSensor?.Value != null) 
                    return (int) cpuTempSensor.Value;
                return 0;
            }
            catch (Exception e)
            {
                LoggerHelper.Error("Failed to read temperature", e);
                return 0;
            }
        }
    }
}
