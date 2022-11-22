using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessWrapper
{
    public class MinecraftWrapper
    {
        public readonly HttpClient Client = new HttpClient();

        public ConsoleWrapper ServerConsole;

        private static readonly string StopURL = "https://prod-22.canadacentral.logic.azure.com/workflows/33bc0461d58d4d8691ea87ada674326a/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=Aswssl0hSrVph16TcrPoIN87GC9Y5YqJ4xazJW0EeY8";


        public bool IsServerLoaded { get; set; } = false;
        public int PlayerCount { get; private set; } = 0;
        public DateTime ServerStartTime { get; private set; } = DateTime.Now;
        public DateTime ServerStopTime { get; private set; } = DateTime.Now.AddHours(2);

        #region Timers

        System.Timers.Timer ShutdownCheckTimer = new System.Timers.Timer(60000);

        #endregion

        public MinecraftWrapper(string fileName, string arguments)
        {
            ServerConsole = new ConsoleWrapper(fileName, arguments);

            ServerConsole.StandardOutputReceived += ServerConsole_StandardOutputReceived;

            ShutdownCheckTimer.Elapsed += ShutdownCheckTimer_Elapsed;
            ShutdownCheckTimer.AutoReset = true;
            ShutdownCheckTimer.Enabled = true;

            Console.ForegroundColor = ConsoleColor.Yellow;
        }

        private async void ShutdownCheckTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // If the server is offline, issue a shutdown request to the host
            if (ServerConsole.HasExited)
            {
                Console.WriteLine($"[{DateTime.Now}] Shutting host down in 60 seconds");

                // Wait 60 seconds
                Thread.Sleep(60000);

                // Send shutdown request to ServerHost
                var result = await ShutdownServerHost();
            }
            else
            {
                EmptyServerCheck();
            }
        }

        // Listener for the console wrapper output
        private void ServerConsole_StandardOutputReceived(object? sender, ConsoleWrapper.StandardOutputReceivedEventArgs e)
        {
            var output = e.StandardOutput;

            if (output != null)
            {
                ProcessOutput(output);

                var currentColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(output);
                Console.ForegroundColor = currentColor;
            }
        }

        private void ProcessOutput(string output)
        {
            // Check if the last server output can be used to update any server information
            
            // Update player count
            UpdatePlayerCount(output);

            // Update whether the server has been loaded
            UpdateServerLoaded(output);
        }

        private async Task<bool> ShutdownServerHost()
        {
            /*
            var values = new Dictionary<string, string>
            {
                { "username", "" },
                { "password", "" }
            };
            var content = new FormUrlEncodedContent(values);
            */

            Console.WriteLine($"[{DateTime.Now}] Sending shutdown request to server host.");
            
            var response = await Client.PostAsync(StopURL, null);

            var responseString = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode;
        }

        #region StateChecks

        private void EmptyServerCheck()
        {
            // Shutdown if no players and shutdown time has been reached
            if (PlayerCount <= 0 && ServerStopTime < DateTime.Now)
            {
                ServerConsole.WriteToStandardInput("stop");
            }
            else if (PlayerCount > 0)
            {
                ServerStopTime = DateTime.Now.AddHours(1);
            }
            else if ((DateTime.Now - ServerStopTime) < TimeSpan.FromMinutes(10))
            {
                Console.WriteLine($"[{DateTime.Now}] Shutting down in {(ServerStopTime - DateTime.Now).Minutes} minutes");
            }
        }

        #endregion

        #region ProcessOutputHelpers

        private void UpdatePlayerCount(string line)
        {
            if (line.Contains("[Server thread/INFO]")) 
            {
                if (line.Contains("joined the game"))
                {
                    Console.WriteLine($"[{DateTime.Now}] Player count incremented.");
                    PlayerCount++;
                }
                else if (line.Contains("left the game"))
                {
                    Console.WriteLine($"[{DateTime.Now}] Player count decremented.");
                    PlayerCount--;
                }
            }
        }

        private void UpdateServerLoaded(string line)
        {
            if (line.Contains("[Server thread/INFO]: Done"))
            {
                Console.WriteLine($"[{DateTime.Now}] Server loaded");
                IsServerLoaded = true;
            }
        }

        #endregion
    }
}
