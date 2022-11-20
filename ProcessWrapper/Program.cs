
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;

static class Program
{
    public static bool Stopping = false;

    public static readonly HttpClient Client = new HttpClient();

    public static DateTime LastPlayerZeroTime = DateTime.Now;

    public static Process? Process;

    private static readonly string StopURL = "https://prod-22.canadacentral.logic.azure.com/workflows/33bc0461d58d4d8691ea87ada674326a/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=Aswssl0hSrVph16TcrPoIN87GC9Y5YqJ4xazJW0EeY8";

    public static int PlayerCount = 0;

    static int Main(string[] args)
    {
        var arguments = string.Empty;
        var fileName = string.Empty;

        if (args.Length == 0)
        {
            Console.WriteLine("Insufficient minimum arguments supplied - please provide a command.");
            return -1;
        }
        else if (args.Length == 2)
        {
            arguments = args[1];
        }
        else
        {
            Console.WriteLine("Too many arguments");
            return -1;
        }

        fileName = args[0];

        var p = new ProcessStartInfo
        {
            FileName = fileName,       
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        try
        {
            Process = Process.Start(p);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
        Process.Exited += Process_Exited;

        Process.BeginOutputReadLine();
        Process.OutputDataReceived += (s, e) => ProcessStandardOutput(e.Data);

        Process.BeginErrorReadLine();
        Process.ErrorDataReceived += (s, e) => Console.Error.WriteLine(e.Data);

        // Seperate thread for reading input so it does not block the output of our scheduled functions
        Thread thread = new Thread(new ThreadStart(ReadInput));
        thread.Start();
        
        // Perform various checks and actions here (player count shutdown, backups)
        while (true)
        {
            Thread.Sleep(250);

            // if (PlayerCount == 0 && LastPlayerZeroTime > HOUR) { WriteToStandardInput(stop); }
        }

        return Process.ExitCode;
    }

    private static void CheckShutdown()
    {
        if ((DateTime.Now - LastCheckTime) > TimeSpan.FromSeconds(1000))
        {
            Stopping = true;
            WriteToStandardInput("stop");
        }
        else
        {
            Console.WriteLine(DateTime.Now - LastCheckTime);
        }
    }

    private static void WriteToStandardInput(string l)
    {
        Process.StandardInput.WriteLine(l);
        Process.StandardInput.Flush();
    }

    private static void ReadInput()
    {
        string l;
        while (true)
        {
            Thread.Sleep(500);
            if (!Stopping)
            {
                // Used if we want manual standard input
                if ((l = Console.ReadLine()) == null) continue;
                else WriteToStandardInput(l);
            }
        }
    }

    private static async void Process_Exited(object? sender, EventArgs e)
    {
        Console.WriteLine("Process exiting");
        var result = await SendVMShutdownMessage();
    }

    public static void ProcessStandardOutput(string line)
    {
        try
        {
            CheckForPlayerCount(line);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        

        Console.WriteLine($"SERVER | {line}");
    }

    private static void CheckForPlayerCount(string line)
    {
        if (line.Contains("players online:"))
        {
            line = line.Split("]: ")[1];
            Console.WriteLine("Attempting to parse: " + line);
            PlayerCount = int.Parse(line.Split(" ")[2]);
            Console.WriteLine($"Player count of {PlayerCount} saved.");
        }
    }

    public static async Task<bool> SendVMShutdownMessage()
    {
        var values = new Dictionary<string, string>
        {
            { "secret", "hello" },
            { "thing2", "world" }
        };

        var content = new FormUrlEncodedContent(values);
        Console.WriteLine("Sent shutdown request to VM.");
        return true;
        //var response = await Client.PostAsync(StopURL, content);

        //var responseString = await response.Content.ReadAsStringAsync();

        //return response.IsSuccessStatusCode;
    }

    private static void SendPlayerCountRequest()
    {
        //WriteToStandardInput("test");
    }


}