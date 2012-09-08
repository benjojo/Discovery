using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PcapDotNet.Core;

namespace Discovery
{
    class Program
    {
        static PacketDevice device;
        static PacketCommunicator communicator;

        static IP startIP;
        static IP endIP;

        static int delay;
        static string outPath;

        static void Main(string[] args)
        {
            if (!parseArgs(args)) return;

            var end = endIP++;
            for (IP i = startIP; i < end; i++)
            {
                Console.WriteLine("IP: {0}, {1}, {2}, {3}",
                    i.Values[0], i.Values[1], i.Values[2], i.Values[3]);
            }

            Console.ReadLine();
        }

        private static bool parseArgs(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: discovery <start ip> <end ip> <delay (ms)> <output>");
                return false;
            }
            try
            {
                startIP = new IP(args[0]);
                endIP = new IP(args[1]);
                delay = int.Parse(args[2]);
                outPath = args[3];
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
