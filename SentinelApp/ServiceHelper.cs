using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SentinelApp
{
    public static class ServiceHelper
    {

        public static bool ServiceExists(string serviceName)
        {
            return ServiceController.GetServices()
                .Any(serviceController => serviceController.ServiceName.Equals(serviceName));
        }

        public static void StartService(string ServiceName)
        {
            ServiceController sc = new ServiceController();

            sc.ServiceName = ServiceName;

            Console.WriteLine("The {0} service status is currently set to {1}", ServiceName, sc.Status.ToString());

            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                // Start the service if the current status is stopped.
                Console.WriteLine("Starting the {0} service ...", ServiceName);
                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);

                    // Display the current service status.
                    Console.WriteLine("The {0} service status is now set to {1}.", ServiceName, sc.Status.ToString());
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine("Could not start the {0} service.", ServiceName);
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine("Service {0} already running.", ServiceName);
            }
        }

        public static void StopService(string ServiceName)
        {
            ServiceController sc = new ServiceController();
            sc.ServiceName = ServiceName;

            Console.WriteLine("The {0} service status is currently set to {1}", ServiceName, sc.Status.ToString());

            if (sc.Status == ServiceControllerStatus.Running)
            {
                // Start the service if the current status is stopped.
                Console.WriteLine("Stopping the {0} service ...", ServiceName);
                try
                {
                    // Start the service, and wait until its status is "Running".
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);

                    // Display the current service status.
                    Console.WriteLine("The {0} service status is now set to {1}.", ServiceName, sc.Status.ToString());
                }
                catch (InvalidOperationException e)
                {
                    Console.WriteLine("Could not stop the {0} service.", ServiceName);
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine("Cannot stop service {0} because it's already inactive.", ServiceName);
            }
        }

        public static bool ServiceIsRunning(string ServiceName)
        {
            ServiceController sc = new ServiceController();
            sc.ServiceName = ServiceName;

            if (sc.Status == ServiceControllerStatus.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public static void RebootService(string ServiceName)
        {
            if (ServiceExists(ServiceName))
            {
                if (ServiceIsRunning(ServiceName))
                {
                    StopService(ServiceName);
                }
                else
                {
                    StartService(ServiceName);
                }
            }
            else
            {
                Console.WriteLine(@"The given service {0} doesn't exists", ServiceName);
            }
        }
    }
}