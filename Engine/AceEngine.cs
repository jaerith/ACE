using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ACE.DB.Classes;
using ACE.DB.Factory;
using ACE.Readers;
using ACE.Writers;

namespace ACE
{
    /// <summary>
    /// 
    /// This class serves as the main body of functionality, in which it will iterate through
    /// different Process configurations in the metadata.  For each Process, it will then:
    /// 
    /// 1.) If applicable, it will point to a target REST API in order to download the change manifest(s) and
    /// persist them to a table.
    /// If not applicable, it will assume that it needs to pull down the entire record catalog and skip to Step 3.
    /// 2.) It will extract information from the change manifest(s) and then know which actual records to
    /// pull through the REST API.
    /// 3.) The records of interest will be pulled down and persisted to a table in their raw form.
    /// 4.) The raw payloads (for the records of interest) will then be parsed and then loaded into a staging table.
    ///     
    /// </summary>
    public class AceEngine : IDisposable
    {
        #region CONSTANTS

        public const int CONST_Max_Retry_Count   = 3;
        public const int CONST_Thread_Sleep_Time = 120000;

        public const string CONST_DATA_URL_ISSUE_ERR_MSG =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><response><errors><error><description>General problem with the data URL.</description></error></errors></response>";

        #endregion

        #region Private Fields

        private bool   DebugFlag         = false;
        private bool   SystemActive      = true;
        private bool   TaskTimedOut      = false;
        private bool   UseTaskMaxTime    = false;
        private string LogDirectory      = "";
        private string CurrentRunLogFile = "";
        private object ThreadLock        = new object();

        private StreamWriter moCurrentRunLogWriter = null;

        private AceConnectionMetadata moStgConnectionMetadata;

        #endregion

        #region Constructor / Dispose

        public AceEngine()
        { }

        public void Dispose()
        { }

        #endregion

        #region Public Methods

        public void ExecuteOnce()
        {
            string sSubject = "AceEngine::ExecuteOnce()";

            try
            {
                var oThreadWaitSpan = new TimeSpan(0, 30, 0);

                InitMembers();

                LogInfo(sSubject, "DEBUG : Starting AceEngine");

                Thread oConsumptionThread = SpawnConsumptionThread();

                if (UseTaskMaxTime)
                    oConsumptionThread.Join(oThreadWaitSpan);
                else
                    oConsumptionThread.Join();
            }
            catch (Exception ex)
            {
                LogError(sSubject, "----------------------------------------------------------------");
                LogError(sSubject, "ERROR!  An error has taken place.", ex);
                LogError(sSubject, "----------------------------------------------------------------");
            }
            finally
            {
                LogInfo(sSubject, "DEBUG : Finished AceEngine");

                Dispose();
            }
        }

        #endregion

        #region Init Methods

        private void InitLogging()
        {
            if (!Directory.Exists(LogDirectory))
                throw new Exception("ERROR!  Directory(" + LogDirectory + ") does not exist.");

            CurrentRunLogFile     = LogDirectory + "\\AceEngine." + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".log";
            moCurrentRunLogWriter = new StreamWriter(CurrentRunLogFile);
            System.Console.SetOut(moCurrentRunLogWriter);
        }

        private void InitMembers()
        {
            string   sFormatString      = "user id={0};password={1};database={2};server={3};connection timeout=120;";
            string[] asConnectionParams = new string[4];

            moStgConnectionMetadata = new AceConnectionMetadata();

            DebugFlag      = Properties.Settings.Default.DEBUG_MODE;
            UseTaskMaxTime = Properties.Settings.Default.USE_TASK_MAX_TIME;
            LogDirectory   = Properties.Settings.Default.LOG_DIRECTORY;

            asConnectionParams[0] = moStgConnectionMetadata.DBUser     = Properties.Settings.Default.ACE_DB_USER;
            asConnectionParams[1] = moStgConnectionMetadata.DBPassword = Properties.Settings.Default.ACE_DB_PASSWORD;
            asConnectionParams[2] = moStgConnectionMetadata.DBTarget   = Properties.Settings.Default.ACE_DB_TARGET;
            asConnectionParams[3] = moStgConnectionMetadata.DBMachine  = Properties.Settings.Default.ACE_DB_MACHINE;
            moStgConnectionMetadata.DBConnectionString = string.Format(sFormatString, asConnectionParams);

            InitLogging();

        } // method()

        #endregion

        #region Logging Methods
        private void LogError(string psSubject, string psLogMsg)
        {
            Console.WriteLine(psSubject + " : " + psLogMsg + "..." + DateTime.Now);
            Console.Out.Flush();
        }

        private void LogError(string psSubject, string psLogMsg, Exception poException)
        {
            Console.WriteLine(psSubject + " : " + psLogMsg + " -> (\n" + poException + "\n)..." + DateTime.Now);
            Console.Out.Flush();
        }

        private void LogInfo(string psSubject, string psLogMsg)
        {
            Console.WriteLine(psSubject + " : " + psLogMsg + "..." + DateTime.Now);
            // Console.Out.Flush();
        }
        #endregion

        #region Support Methods

        /// <summary>
        /// 
        /// According to the direction of the metadata for each configured process, this method will retrieve data through
        /// a specified REST API and then persist the returned raw payloads into a table for later usage.
        /// 
        /// <param name="poProcessWriter">The writer that will record the progress of this instance for the configured Process (represented by 'poTempProcess')</param>
        /// <param name="poProductWriter">The writer that will record the raw payload for each record retrieved in this run</param>
        /// <param name="poTempProcess">The structure that represents the Process being currently run</param>
        /// <returns>The ChangeSeq (i.e., PID) assigned to this particular instance of the Process being run</returns>
        /// </summary>
        private long PullDataSnapshot(AceChangeProcessWriter poProcessWriter, AceChangeRecordWriter poProductWriter, AceProcess poTempProcess)
        {
            bool   bSuccess       = true;
            int    nTotalRecords  = 0;
            long   nTmpKey        = 0;
            string sInfoMsg       = "";
            string sErrMsg        = "";
            string sSubject       = "AceEngine::PullDataSnapshot()";
            string sTmpKey        = "";
            string sTmpChangeBody = "";
            string sTmpDataBody   = "";

            StringBuilder sbLastAnchor = new StringBuilder();

            Dictionary<string, string> oTmpFilterArgs = new Dictionary<string, string>();

            /*
             * Implementation here
             */

            return poTempProcess.ChangeSeq;
        }

        #endregion

        #region Threading Methods

        /// <summary>
        /// 
        /// This method serves as the main logic of this function, for each Process performing the 4 main steps addressed 
        /// in the description of the class.  Those 4 steps can be generalized into just 2 steps, both driven by metadata:
        /// 
        /// 1.) The record (i.e., product) data will be pulled down through the REST API in raw form
        /// 2.) The record (i.e., product) data will then be applied (i.e., upserted) to a staging table
        /// 
        /// <returns>None.</returns>
        private void ConsumeData()
        {
            string sSubject = "AceEngine::ConsumeData()";

            try
            {
                using (AceMetadataFactory MetadataFactory = new AceMetadataFactory(moStgConnectionMetadata))
                {
                    using (AceChangeProcessWriter oProcessWriter = new AceChangeProcessWriter(moStgConnectionMetadata))
                    {
                        using (AceChangeRecordWriter oProductWriter = new AceChangeRecordWriter(moStgConnectionMetadata))
                        {
                            List<AceProcess> CurrentJobs = MetadataFactory.GetActiveJobs();

                            foreach (AceProcess TempProcess in CurrentJobs)
                            {
                                LogInfo(sSubject, "Processing Job [" + TempProcess.ProcessID + "] : (" + TempProcess.ProcessName + ")");
                                System.Console.Out.Flush();

                                PullDataSnapshot(oProcessWriter, oProductWriter, TempProcess);
                                System.Console.Out.Flush();

                                /*
                                ApplyDataSnapshot(oTempJob);
                                System.Console.Out.Flush();
                                */
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(sSubject, "ERROR!  An error has taken place with record", ex);
            }
        }

        /// <summary>
        /// 
        /// This method will return the thread that executes the main logic of this program.
        /// 
        /// <returns>The main logical thread of this program</returns>
        private Thread SpawnConsumptionThread()
        {
            Thread oSpawnConsumptionThread = new Thread(ConsumeData);
            oSpawnConsumptionThread.Start();

            return oSpawnConsumptionThread;
        }
        #endregion
    }
}
