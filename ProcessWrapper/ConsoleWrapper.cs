using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessWrapper
{
    public class ConsoleWrapper
    {
        private Process? _process;

        public bool HasExited => _process?.HasExited ?? false;

        public event EventHandler<StandardOutputReceivedEventArgs> StandardOutputReceived;

        public class StandardOutputReceivedEventArgs : EventArgs
        {
            public string? StandardOutput { get; set; }
        }

        public ConsoleWrapper(string fileName, string arguments)
        {
            var p = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            StartProcess(p);
        }

        public bool StartProcess(ProcessStartInfo p)
        {
            try
            {
                _process = Process.Start(p);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            if (_process == null) return false;

            // Bubble up standard output event
            _process.BeginOutputReadLine();
            _process.OutputDataReceived += (s, e) => 
                StandardOutputReceived?
                    .Invoke(this, new StandardOutputReceivedEventArgs() { StandardOutput = e?.Data ?? string.Empty}); ;

            // Output any errors
            _process.BeginErrorReadLine();
            _process.ErrorDataReceived += (s, e) => Console.Error.WriteLine(e.Data);

            // Seperate thread for reading input so it does not block non-process output
            Thread thread = new Thread(new ThreadStart(ReadInput));
            thread.Start();

            return true;
        }

        #region InputHandling

        private void ReadInput()
        {
            string l;
            while (true)
            {
                Thread.Sleep(500);

                // Read input and submit to process standard input
                if ((l = Console.ReadLine()) == null) continue;
                else WriteToStandardInput(l);
            }
        }

        public void WriteToStandardInput(string l)
        {
            if (_process == null) return;

            _process.StandardInput.WriteLine(l);
            _process.StandardInput.Flush();
        }

        #endregion
    }
}
