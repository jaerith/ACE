using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ACE.DB.Classes;

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
        private bool   UseScribe         = false;
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

                /*
                InitMembers();

                LogInfo(sSubject, "DEBUG : Starting AceEngine");

                Thread oConsumptionThread = SpawnConsumptionThread();

                if (UseTaskMaxTime)
                    oConsumptionThread.Join(oThreadWaitSpan);
                else
                    oConsumptionThread.Join();
               */
            }
            catch (Exception ex)
            {
                /*
                LogError(sSubject, "----------------------------------------------------------------");
                LogError(sSubject, "ERROR!  An error has taken place.", ex);
                LogError(sSubject, "----------------------------------------------------------------");
                */
            }
            finally
            {
                /*
                LogInfo(sSubject, "DEBUG : Finished AceEngine");
                 */

                Dispose();
            }
        }

        #endregion
    }
}
