﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScanMaster.Acquire.Plugin;
using TransferCavityLock2012;
using DAQ.TransferCavityLock2012;
using System.Xml.Serialization;
using DAQ.Environment;
using System.Threading;
using System.Runtime.Remoting;
using System.Net;
using System.Net.Sockets;

namespace ScanMaster.Acquire.Plugins
{
    [Serializable]
    public class TCLOutputPlugin : ScanOutputPlugin
    {

        [NonSerialized]
        private double scanParameter = 0;
        private string computer;
        private string name;
        private string hostName = (String)System.Environment.GetEnvironmentVariables()["COMPUTERNAME"];
        private string scannedParameter;
        private double initialVoltage = 0.0;
        private double initialSetPoint = 0.0;
        [NonSerialized]
        private TransferCavityLock2012.Controller tclController;


        protected override void InitialiseSettings()
        {
            settings["channel"] = "laser";
            settings["cavity"] = "Hamish";
            settings["computer"] = hostName;
            settings["scannedParameter"] = "setpoint";
        }



        public override void AcquisitionStarting()
        {
             //connect the TCL controller over remoting network connection


            if (settings["computer"] == null)
            {
                computer = hostName;
            }
            else
            {
                computer = (String)settings["computer"];
            }

            if (settings["scannedParameter"] == null)
            {
                scannedParameter = "setpoint";
            }
            else
            {
                scannedParameter = (String)settings["scannedParameter"];
            }

            IPHostEntry hostInfo = Dns.GetHostEntry(computer);

            foreach (var addr in Dns.GetHostEntry(computer).AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                name = addr.ToString();
            }

            EnvironsHelper eHelper = new EnvironsHelper(computer);

            string tcpChannel = ((TCLConfig)eHelper.Hardware.GetInfo(settings["cavity"])).TCPChannel.ToString();

            tclController = (TransferCavityLock2012.Controller)(Activator.GetObject(typeof(TransferCavityLock2012.Controller), "tcp://"+ name + ":" + tcpChannel + "/controller.rem"));

            scanParameter = 0;

            setV((double)settings["start"], 200, scannedParameter);
        }
        

        public override void ScanStarting()
        {
            initialVoltage = tclController.SlaveLasers[(string)settings["channel"]].VoltageToLaser;
            initialSetPoint = tclController.SlaveLasers[(string)settings["channel"]].LaserSetPoint;
            if (scannedParameter == "voltage")
            {
                tclController.UnlockLaser((string)settings["channel"]);
            }
            
        }

        public override void ScanFinished()
        {
            setV((double)settings["start"], 200, scannedParameter);
        }

        public override void AcquisitionFinished()
        {
            setV(initialVoltage, 200, "voltage");
            tclController.LockLaser((string)settings["channel"]);
            setV(initialSetPoint, 200, "setpoint");
        }

        [XmlIgnore]
        public override double ScanParameter
        {
            set
            {
                scanParameter = value;
                if (!Environs.Debug) setV(value, 50, scannedParameter);
            }
            get { return scanParameter; }
        }

        private void setV(double v, int waitTime,string scannedOutput)
        {
            switch (scannedOutput)
            {
            case "setpoint":
                tclController.SetLaserSetpoint((string)settings["channel"], v);
                break;
            case "voltage":
                tclController.SetLaserOutputVoltage((string)settings["channel"], v);
                break;
            }
            Thread.Sleep(waitTime);
        }




    }
}
