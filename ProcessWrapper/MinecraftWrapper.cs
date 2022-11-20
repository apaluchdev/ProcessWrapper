using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessWrapper
{
    public class MinecraftWrapper
    {
        public static readonly HttpClient Client = new HttpClient();

        ConsoleWrapper ServerConsole;

        private static readonly string StopURL = "https://prod-22.canadacentral.logic.azure.com/workflows/33bc0461d58d4d8691ea87ada674326a/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=Aswssl0hSrVph16TcrPoIN87GC9Y5YqJ4xazJW0EeY8";

        public bool IsServerLoaded { get; set; } = false;
        public int PlayerCount { get; private set; } = 0;
        public DateTime ServerStartTime { get; private set; } = DateTime.Now;
        public DateTime ServerStopTime { get; private set; } = DateTime.Now.AddHours(1);

        public MinecraftWrapper(string fileName, string arguments)
        {
            ServerConsole = new ConsoleWrapper(fileName, arguments);

            ServerConsole.StandardOutputReceived += ServerConsole_StandardOutputReceived;

            Task.Run(() => MinecraftStateLoop());
        }

        private void ServerConsole_StandardOutputReceived(object? sender, ConsoleWrapper.StandardOutputReceivedEventArgs e)
        {
            var output = e.StandardOutput;

            if (output != null)
                ProcessOutput(output);

            Console.WriteLine(output);
        }

        private void ProcessOutput(string output)
        {
            // Check if the last server output can be used to update any server information
            UpdatePlayerCount(output);

            UpdateServerLoaded(output);
        }

        private async Task MinecraftStateLoop()
        {
            // Periodically request various data from the server here so we can update our properties and behave accordingly.
            while (true)
            {
                Thread.Sleep(30000);

                if (PlayerCount <= 0 && ServerStopTime < DateTime.Now)
                {
                    SendStopCommand();
                }
                else if (PlayerCount <= 0 && (ServerStopTime - DateTime.Now) < TimeSpan.FromMinutes(30))
                {
                    Console.WriteLine("Server shutting down in 30 minutes due to player count.");
                }
                else if (PlayerCount > 0)
                {
                    ServerStopTime = DateTime.Now.AddHours(1);
                }
                else
                {
                    Console.WriteLine("Server is scheduled to shutdown at " + ServerStopTime.ToString());
                }

                if (ServerConsole.HasExited)
                {
                    var result = await ShutdownServerHost();

                    if (result) Console.WriteLine("Clean shutdown");
                    else Console.WriteLine("Error while shutting down server host");

                    break;
                }

                Console.WriteLine("Players online: " + PlayerCount.ToString());
            }
        }

        private void SendStopCommand()
        {
            ServerConsole.WriteToStandardInput("stop");
        }

        private async Task<bool> ShutdownServerHost()
        {
            var values = new Dictionary<string, string>
            {
                { "secret", "hello" },
                { "thing2", "world" }
            };
            var content = new FormUrlEncodedContent(values);

            Console.WriteLine("Sending shutdown request to server host.");
            
            var response = await Client.PostAsync(StopURL, content);

            var responseString = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode;
        }

        #region ProcessOutputHelpers

        private void UpdatePlayerCount(string line)
        {
            if (line.Contains("players online:"))
            {
                Console.WriteLine("Updating player count...");
                line = line.Split("]: ")[1];
                PlayerCount = int.Parse(line.Split(" ")[2]);
            }
        }

        private void UpdateServerLoaded(string line)
        {
            if (line.Contains("[Server thread/INFO]: Done"))
            {
                Console.WriteLine("Wrapper recognized server has loaded");
                IsServerLoaded = true;
            }
        }

        #endregion
    }
}
