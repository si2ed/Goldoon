using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace Goldoon.WindowsService
{
    static class Program
    {

        static void Main(string[] args)
        {
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new BotHandler()
                };

                if (Environment.UserInteractive)
                {
                    string parameter = string.Concat(args);
                    switch (parameter)
                    {
                        case "--install":
                            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                            break;
                        case "--uninstall":
                            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                            break;
                    }
                }
                else
                {
                    ServiceBase.Run(ServicesToRun);
                }
            }
            catch (Exception ex)
            {
                string sSource = "BotHandler";
                
                EventLog.WriteEntry(sSource, ex.ToString(), EventLogEntryType.Warning, 234);

            }
        }


    }
}
