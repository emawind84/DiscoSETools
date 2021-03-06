﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Threading;

namespace DiscoSETools
{
    /// <summary>
    /// <remarks>
    /// Naming Conventions: 
    /// https://msdn.microsoft.com/en-us/library/ms229043%28v=vs.110%29.aspx
    /// http://www.dofactory.com/reference/csharp-coding-standards
    /// </remarks>
    /// </summary>
    class Utils
    {
        private static System.IO.StreamWriter stream = null;
        private static String netLogFile = Properties.Settings.Default.LogFilePath;
        private StringBuilder output = new StringBuilder();
        private static bool errorRedirect = false;
        private static bool errorsWritten = false;

        public string Output
        {
            get { return output.ToString(); }
        }

        //public abstract void CustomOutputHandler(Object processName, System.Diagnostics.DataReceivedEventArgs outLine);

        /// <summary>
        /// Execute command line process
        /// </summary>
        /// <param name="processName"></param>
        /// <param name="processArgs"></param>
        /// <param name="outputAppend"></param>
        public void ExecuteCommand(string processName, string processArgs, bool outputAppend)
        {
            if (output == null || !outputAppend)
            {
                output = new StringBuilder();
                //consoleResultTextBox.Clear();
            }

            //output.Append("Executing process: " + processName);
            //output.Append(Environment.NewLine + processArgs);

            System.Diagnostics.ProcessStartInfo pStartInfo = new System.Diagnostics.ProcessStartInfo(processName, processArgs);

            System.Diagnostics.Process process = new System.Diagnostics.Process();

            //pStartInfo.WorkingDirectory = @"C:\Users\Public\Documents\se_backup";
            pStartInfo.RedirectStandardOutput = true;
            pStartInfo.UseShellExecute = false;
            pStartInfo.CreateNoWindow = true;

            pStartInfo.RedirectStandardError = true;

            process.EnableRaisingEvents = false;
            process.StartInfo = pStartInfo;

            
            process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(OutputDataHandler);
            process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(OutputDataHandler);

            // LOG ON FILE
            process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(StreamDataHandler);
            process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(StreamDataHandler);

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            //System.IO.StreamReader myStreamReader = process.StandardOutput;
            //string myString = myStreamReader.ReadLine();
            //Console.WriteLine(myString);
            //string result = process.StandardOutput.ReadToEnd();

            //process.BeginOutputReadLine();
            //string error = process.StandardError.ReadToEnd();

            process.WaitForExit();
            process.Close();
        }

        public void ExecuteCommand(string processName, string processArgs)
        {
            ExecuteCommand(processName, processArgs, false);
        }

        /// <summary>
        /// Output handler for command line process
        /// </summary>
        /// <param name="sendingProcess"></param>
        /// <param name="outLine"></param>
        private void OutputDataHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            // Collect the net view command output. 
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                // Add the text to the collected output.
                output.Append(outLine.Data + Environment.NewLine);
            }

            //Console.WriteLine("OutputDataHandler terminated");
        }

        /// <summary>
        /// Write error handler to file for command line process
        /// </summary>
        /// <param name="sendingProcess"></param>
        /// <param name="outLine"></param>
        private void StreamDataHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            // Write the error text to the file if there is something 
            // to write and an error file has been specified. 

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                if (!errorsWritten)
                {
                    if (stream == null)
                    {
                        // Open the file. 
                        try
                        {
                            stream = new System.IO.StreamWriter(netLogFile, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not open file!");
                            Console.WriteLine(e.Message.ToString());
                        }
                    }

                    if (stream != null)
                    {
                        // Write a header to the file if this is the first 
                        // call to the error output handler.
                        stream.WriteLine();
                        stream.WriteLine(DateTime.Now.ToString());
                        stream.WriteLine("Net View output:");
                    }
                    errorsWritten = true;
                }

                if (stream != null)
                {
                    // Write redirected errors to the file.
                    stream.WriteLine(outLine.Data);
                    stream.Flush();
                }
            }
        }

        /// <summary>
        /// Return steam server information given address:port
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static string GetSteamServerInfo(string addr, EventHandler<string> handler)
        {
            string scriptDir = Properties.Settings.Default.ScriptDirPath;

            Utils utils = new Utils();

            ExecuteCommandThread thrd = new ExecuteCommandThread
            {
                CommandWithArgs = String.Format("/C {0}\\steam_server_info.bat \"{1}\" \"{2}\"",
                    scriptDir,
                    scriptDir,
                    addr
                )
            };
            thrd.CommandExecuted += handler;

            Thread oThread = new Thread(new ThreadStart(thrd.Execute) );
            oThread.Start();

            return "";
        }

        public static string StartService(string serviceName, int timeoutMilliseconds)
        {
            if (String.IsNullOrEmpty(serviceName))
            {
                return "Service not set";
            }

            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);

                return "Service status = " + service.Status;
            }
            catch (Exception e)
            {
                //Console.WriteLine("{0} Exception caught.", e);
                return String.Format("{0} Exception caught.", e);
            }
            //UpdateServicesStatus();
        }

        public static string StopService(string serviceName, int timeoutMilliseconds)
        {
            if (String.IsNullOrEmpty(serviceName))
            {
                return "Service not set";
            }

            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

                return "Service status = " + service.Status;
            }
            catch (Exception e)
            {
                //Console.WriteLine("{0} Exception caught.", e);
                return String.Format("{0} Exception caught.", e);
            }
            //UpdateServicesStatus();
        }

        public static string StatusService(string serviceName)
        {
            if (String.IsNullOrEmpty(serviceName))
            {
                return "N/A";
            }

            try
            {
                ServiceController service = new ServiceController(serviceName);

                return service.Status.ToString();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                return "Service Failed";
            }
        }
    }

    class ExecuteCommandThread
    {
        string commandWithArgs;

        public string CommandWithArgs
        {
            get { return commandWithArgs; }
            set { commandWithArgs = value; }
        }

        //public delegate void OnExecuteHandlerEventHandler(object sender, string output);

        public event EventHandler<string> CommandExecuted;

        public void Execute()
        {
            Utils utils = new Utils();
            utils.ExecuteCommand("cmd", commandWithArgs);
            EventHandler<string> handler = CommandExecuted;
            if (handler != null)
            {
                handler(this, utils.Output);
            }
        }

        // Wrapper method for use with thread pool. 
        public void ThreadPoolCallback(Object threadContext)
        {
            int threadIndex = (int)threadContext;
            Console.WriteLine("thread {0} started...", threadIndex);
            Execute();
            Console.WriteLine("thread {0} result calculated...", threadIndex);
        }
    }

    public class ServiceThread
    {
        string service;
        int timeout;
        string output;

        // Declare the delegate (if using non-generic pattern). 
        public delegate void CompletedEventHandler(object sender, string output);

        // Declare the event. 
        public event CompletedEventHandler CompletedEvent;

        public int Timeout
        {
            get { return timeout; }
            set
            {
                timeout = value;
            }
        }

        public string ServiceName
        {
            get { return service; }
            set
            {
                service = value;
            }
        }

        public void Status()
        {
            output = Utils.StatusService(service);
            if (CompletedEvent != null)
            {
                CompletedEvent(this, output);
            }
        }

        public void Start()
        {
            output = Utils.StartService(service, timeout);
            if (CompletedEvent != null)
            {
                CompletedEvent(this, output);
            }
        }

        public void Stop()
        {
            output = Utils.StopService(service, timeout);
            if (CompletedEvent != null)
            {
                CompletedEvent(this, output);
            }
        }

    }
}
