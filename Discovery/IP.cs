using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discovery
{
    struct IP
    {
        public int[] Values;

        public IP(int val1, int val2, int val3, int val4)
        {
            Values = new int[4];
            Values[0] = val1;
            Values[1] = val2;
            Values[2] = val3;
            Values[3] = val4;
        }

        public IP(string ipstr)
        {
            string[] ipstr_split = ipstr.Split('.');
            if (ipstr_split.Length != 4) throw new Exception("Invalid IP string.");

            Values = new int[4];
            Values[0] = int.Parse(ipstr_split[0]);
            Values[1] = int.Parse(ipstr_split[1]);
            Values[2] = int.Parse(ipstr_split[2]);
            Values[3] = int.Parse(ipstr_split[3]);
        }

        public static IP operator ++(IP ip)
        {
            ip.Values[3]++;
            if (ip.Values[3] > 255)
            {
                int overflow = ip.Values[3] % 255;
                ip.Values[2]++;
                ip.Values[3] = overflow;
            }
            if (ip.Values[2] > 255)
            {
                int overflow = ip.Values[2] % 255;
                ip.Values[1]++;
                ip.Values[2] = overflow;
            }
            if (ip.Values[1] > 255)
            {
                int overflow = ip.Values[1] % 255;
                ip.Values[0]++;
                ip.Values[1] = overflow;
            }
            if (ip.Values[0] > 255) throw new Exception("wtf faget where you trying to go to now");
            return ip;
        }

        public static bool operator <(IP ip, IP ip2)
        {
            if (ip.Values[0] < ip2.Values[0]) return true;
            if (ip.Values[1] < ip2.Values[1]) return true;
            if (ip.Values[2] < ip2.Values[2]) return true;
            if (ip.Values[3] < ip2.Values[3]) return true;
            return false;
        }

        public static bool operator >(IP ip, IP ip2)
        {
            if (ip.Values[0] > ip2.Values[0]) return true;
            if (ip.Values[1] > ip2.Values[1]) return true;
            if (ip.Values[2] > ip2.Values[2]) return true;
            if (ip.Values[3] > ip2.Values[3]) return true;
            return false;
        }

        public override string ToString()
        {
            return String.Format("{0}.{1}.{2}.{3}", Values[0], Values[1], Values[2], Values[3]);
        }
    }
}
