using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsService.Services;

namespace WindowsService
{
    public partial class Service1 : ServiceBase
    {
        public static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            new Thread(StartService).Start();
        }
        internal void StartService()
        {
            /*
            This is the true composition root for a service,
            so initialize everything in here
            */
            logger.Info("*************** Starting service NOMBRE DEL SERVICIO ***************");

            this.ScheduleService();


        }

        protected override void OnStop()
        {
            logger.Info("Softbrilliance.NOMBREAQUI stopped ");
            this.Schedular.Dispose();

            logger.Info("*************** Finishing service NOMBRE DEL SERVICIO ***************");
        }


        public void working()
        {
            TaskService _service = new TaskService();
            _service.Inicio();
        }

        private Timer Schedular;

        public void ScheduleService()
        {
            try
            {
                logger.Info("Softbrilliance.NOMBREAQUI: Entro a ejecutar método");

                Schedular = new Timer(new TimerCallback(SchedularCallback));
                string mode = ConfigurationManager.AppSettings["Mode"].ToUpper();
                logger.Info("Softbrilliance.NOMBREAQUI Mode: " + mode + " ");

                //Set the Default Time.
                DateTime scheduledTime = DateTime.MinValue;

                if (mode == "DAILY")
                {
                    //Get the Scheduled Time from AppSettings.
                    scheduledTime = DateTime.Parse(System.Configuration.ConfigurationManager.AppSettings["ScheduledTime"]);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next day.
                        scheduledTime = scheduledTime.AddDays(1);
                    }
                }

                if (mode.ToUpper() == "INTERVAL")
                {
                    //Get the Interval in Minutes from AppSettings.
                    int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);

                    //Set the Scheduled Time by adding the Interval to Current Time.
                    scheduledTime = DateTime.Now.AddMinutes(intervalMinutes);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next Interval.
                        scheduledTime = scheduledTime.AddMinutes(intervalMinutes);
                    }
                }

                TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);
                string schedule = string.Format(" day(s) {1} hour(s) {2} minute(s) {3} seconds(s)", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

                logger.Info("Softbrilliance.NOMBREAQUI scheduled to run after: " + schedule + " ");

                //Get the difference in Minutes between the Scheduled and Current Time.
                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                Schedular.Change(dueTime, Timeout.Infinite);

            }
            catch (Exception ex)
            {
                logger.Info("Softbrilliance.NOMBREAQUI Error on:  " + ex.Message + ex.StackTrace);


                //Stop the Windows Service.
                using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController("Softbrilliance.NOMBREAQUI"))
                {
                    serviceController.Stop();
                }
            }
        }

        private void SchedularCallback(object e)
        {
            logger.Info("Softbrilliance.NOMBREAQUI: Entro a ejecutar método");
            working();

            this.ScheduleService();
        }

    }
}