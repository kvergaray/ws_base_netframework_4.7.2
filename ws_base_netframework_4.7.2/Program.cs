using System;
using System.ServiceProcess;

namespace WindowsService
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicacion.
        /// </summary>
        private static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                using (var service = new Service1())
                {
                    try
                    {
                        service.RunConsoleAsync(args).GetAwaiter().GetResult();
                        Console.WriteLine("Ejecucion completada. Revise los registros en la carpeta Logs.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error durante la ejecucion manual: " + ex.Message);
                    }
                    finally{
                        Console.WriteLine("Presione cualquier tecla para salir...");
                    }  
                }

                return;
            }
            else
            {
                ServiceBase.Run(new ServiceBase[]
                {
                    new Service1()
                });
            }
        }
    }
}
