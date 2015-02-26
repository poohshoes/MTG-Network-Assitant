using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GameName1
{
    class Settings
    {
        public string netType;
        public string ip;
        public int port;
        public int width;
        public int height;

        public Settings()
        {
            Load();
        }

        public void Load()
        {
            int index = 0;
            string[] networkSettings = File.ReadAllLines("networkSettings.txt");
            netType = networkSettings[index++];
            Debug.Assert(netType == "server" || netType == "client");
            if(netType == "client")
                ip = networkSettings[index++];
            port = Int32.Parse(networkSettings[index++]);
            width = Int32.Parse(networkSettings[index++]);
            height = Int32.Parse(networkSettings[index++]);
        }
    }
}
