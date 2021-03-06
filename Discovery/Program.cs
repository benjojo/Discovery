﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PcapDotNet.Core;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Packets;
using System.Threading;
using System.Diagnostics;
using System.Net.NetworkInformation;
using PcapDotNet.Packets.IpV4;
using System.Net;
using PcapDotNet.Packets.Icmp;
using System.IO;

namespace Discovery
{
    class Program
    {
        static PacketDevice device;
        static PacketCommunicator communicator;
        static PacketBuilder icmpBuilder;
        static IpV4Layer ipLayer;
        static IcmpEchoLayer icmpLayer;
        

        static PacketBuilder TCPBuilder;
        static TcpLayer TCPLayer;

        static bool RandomMode = false;
        static int RandomLimit = 0;

        static bool TCPMode = false;
        static int TCPPort = 80;

        static IP startIP;
        static IP endIP;

        static int delay;
        static string outPath;

        static void Main(string[] args)
        {
            if (!parseArgs(args)) return;
            preparePcap();

            new Thread(() => { communicator.ReceivePackets(0, receiver); }).Start();
            if (!RandomMode)
            {
                var end = endIP++;
                for (IP i = startIP; i < end; i++)
                {
                    if (!TCPMode)
                    {
                        Console.WriteLine("Sending ICMP to {0}", i);
                        SendICMP(i.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Sending TCP SYN to {0}", i);
                        SendSYN(i.ToString());
                    }
                    Thread.Sleep(delay);
                }
            }
            else
            {
                for (int x = 0; x < RandomLimit; x++ )
                {
                    string i = string.Format("{0}.{1}.{2}.{3}", rand.Next(1, 240), rand.Next(1, 255), rand.Next(1, 255), rand.Next(1, 255));
                    if (!TCPMode)
                    {
                        Console.WriteLine("Sending ICMP to {0}", i);
                        SendICMP(i);
                    }
                    else
                    {
                        Console.WriteLine("Sending TCP SYN to {0}", i);
                        SendSYN(i);
                    }
                    Thread.Sleep(delay);
                }
            }
            Thread.Sleep(1000);
            Console.WriteLine("Done.");
        }

        private static void receiver(Packet packet)
        {
            //if (packet.IpV4.Icmp == null || packet.IpV4.Icmp.MessageType != IcmpMessageType.EchoReply) return;
            IP source = new IP(packet.Ethernet.IpV4.Source.ToString());
            if (!TCPMode)
            {
                Console.WriteLine("icmp reply from {0}", source.ToString());
            }
            else
            {
                Console.WriteLine("SYN reply from {0}", source.ToString());
            }
            File.AppendAllText(outPath, source.ToString() + "\n");
        }

        private static void preparePcap()
        {
            var source = new MacAddress(getClientMAC());
            var dest = new MacAddress(getRouterMAC());

            device = LivePacketDevice.AllLocalMachine[0];
            communicator = device.Open(100, PacketDeviceOpenAttributes.Promiscuous, 1000);

            EthernetLayer ethLayer = new EthernetLayer { Source = source, Destination = dest };
            ipLayer = new IpV4Layer { Source = new IpV4Address(LocalIPAddress()), Ttl = 128 };
            icmpLayer = new IcmpEchoLayer();
            icmpBuilder = new PacketBuilder(ethLayer, ipLayer, icmpLayer);

            TCPLayer = new TcpLayer();
            TCPBuilder = new PacketBuilder(ethLayer, ipLayer, TCPLayer);

            if (!TCPMode)
            {
                communicator.SetFilter("icmp[0] = 0");
            }
            else
            {
                //tcp[13] = 18
                communicator.SetFilter("tcp[13] = 18 and port " + TCPPort);
            }
        }
        
        static string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
        private static string getClientMAC()
        {
            return BitConverter.ToString(
                NetworkInterface.GetAllNetworkInterfaces()
                .First(t => t.Name.Contains("Ethernet"))
                .GetPhysicalAddress()
                .GetAddressBytes())
                .Replace("-", ":");
        }
        private static string getRouterMAC()
        {
            var gateway = NetworkInterface.GetAllNetworkInterfaces().First(t => t.Name.Contains("Ethernet")).GetIPProperties().GatewayAddresses[0].Address;
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "arp";
            p.StartInfo.Arguments = "-a";
            p.Start();
            var output = p.StandardOutput.ReadToEnd().Split('\n');
            p.WaitForExit();

            string gatewayline = output.First(t => t.Contains(gateway.ToString()));
            var split = gatewayline.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Replace("-", ":").Split(' ');
            // kill me now
            return split[3].ToUpper();
        }

        static Random rand = new Random();
        private static void SendICMP(string ip)
        {
            ushort id = (ushort)rand.Next(65000);

            ipLayer.CurrentDestination = new IpV4Address(ip);
            ipLayer.Identification = id;

            icmpLayer.SequenceNumber = id;
            icmpLayer.Identifier = id;

            communicator.SendPacket(icmpBuilder.Build(DateTime.Now));
        }

        private static void SendSYN(string ip)
        {
            ushort id = (ushort)rand.Next(65000);
            

            ipLayer.CurrentDestination = new IpV4Address(ip);
            ipLayer.Identification = id;

            TCPLayer.ControlBits = TcpControlBits.Synchronize;
            TCPLayer.DestinationPort = (ushort)TCPPort;
            TCPLayer.SourcePort = (ushort)rand.Next(60000, 65000);

            communicator.SendPacket(TCPBuilder.Build(DateTime.Now));
        }

        private static bool parseArgs(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: discovery <start ip> <end ip> <delay (ms)> <output>");
                Console.WriteLine("Example: discovery 1.1.1.1 1.1.2.1 100 out.txt");
                Console.WriteLine("For Random Scanning:");
                Console.WriteLine("Example: discovery R <Number Of IPs> 100 out.txt");
                Console.WriteLine("For TCP Scanning:");
                Console.WriteLine("Example: discovery 1.1.1.1 1.1.2.1 100 out.txt <port> -t");
                return false;
            }
            try
            {
              
                if (args[0].ToLower().StartsWith("r"))
                {
                    RandomMode = true;
                    RandomLimit = int.Parse(args[1]);
                }
                else
                {
                    startIP = new IP(args[0]);
                    endIP = new IP(args[1]);
                }
                delay = int.Parse(args[2]);
                outPath = args[3];
                if (args.Length == 6)
                {
                    TCPMode = true;
                    TCPPort = int.Parse(args[4]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid args. {0}\n", ex.Message);
                return false;
            }
            return true;
        }
    }
}
