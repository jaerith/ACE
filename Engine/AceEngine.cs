using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ACE.DB.Classes;
using ACE.DB.Factory;
using ACE.Readers;
using ACE.Writers;

namespace ACE.Engine
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
        /// After the enumeration of the REST API is complete, this method will enumerate through the raw payloads that
        /// were retrieved; it will then use metadata in order to parse and then load the data into specific table columns.
        /// 
        /// <param name="poProcess">The structure that represents the Process being currently run</param>
        /// <returns>None</returns>
        /// </summary>
        private void ApplyDataSnapshot(AceProcess poProcess)
        {
            int nTotalRecords  = 0;
            int nFailedRecords = 0;

            string sSubject = "PmdAceConsumptionServiceImpl::ApplyDataSnapshot()";

            Hashtable CurrRecord = new Hashtable();

            try
            {
                Dictionary<string, IApplicable> BucketApplyManagers = CreateApplyManagers(poProcess);

                using (AceChangeRecordReader oRecordReader = new AceChangeRecordReader(moStgConnectionMetadata, poProcess))
                {
                    foreach (Hashtable oTempRecord in oRecordReader)
                    {
                        // Finish implementation here
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(sSubject,
                         "ERROR!  An error has taken place with record -> Contents: (" + CurrRecord.ToString() + ")",
                         ex);
            }

        } // method()

        /// <summary>
        /// 
        /// Using the metadata of the designated process, this method will create a mapping between the logical buckets 
        /// of a raw data payload (like a composite XML body of a document) to a table and its various columns.
        /// 
        /// <param name="poProcess">The structure that represents the Process being currently run</param>
        /// <returns>None</returns>
        /// </summary>
        private Dictionary<string, IApplicable> CreateApplyManagers(AceProcess poProcess)
        {
            Dictionary<string, IApplicable> ApplyManagers = new Dictionary<string, IApplicable>();

            /*
             * Complete your own implementation here
             */

            return ApplyManagers;
        }

        /// <summary>
        /// 
        /// This method will do the actual work of enumerating through the REST API and making the web requests for data,
        /// both the change manifests that direct calls and/or the actual data.
        /// 
        /// <param name="poProcessWriter">The writer that will record the progress of this instance for the configured Process (represented by 'poTempProcess')</param>
        /// <param name="poProductWriter">The writer that will record the raw payload for each record retrieved in this run</param>
        /// <param name="poTempProcess">The structure that represents the Process being currently run</param>
        /// <returns>The boolean that indicates whether or not the enumeration and persistence succeeded</returns>
        /// </summary>
        private bool EnumerateApiAndPersistRawData(AceChangeProcessWriter poProcessWriter, AceChangeRecordWriter poProductWriter, AceProcess poTempProcess)
        {
            bool   bSuccess       = true;
            int    nTotalRecords  = 0;
            long   nTmpKey        = 0;
            string sTmpKey        = "";
            string sTmpChangeBody = "";
            string sTmpDataBody   = "";
            string sSubject       = "AceEngine::EnumerateApiAndPersistRawData()";
            string sErrMsg        = "";

            Dictionary<string, string> oTmpFilterArgs = new Dictionary<string, string>();

            using (AceXmlReader oChangeDataReader = new AceXmlReader(poTempProcess.ChangeAPIConfiguration))
            {
                oChangeDataReader.FoundNewAnchorCallback = poProcessWriter.UpsertAnchor;

                try
                {
                    foreach (Hashtable oTmpRecord in oChangeDataReader)
                    {
                        try
                        {
                            sTmpKey        = (string) oTmpRecord[poTempProcess.ChangeAPIConfiguration.TargetKeyTag];
                            sTmpChangeBody = (string) oTmpRecord[AceXmlReader.CONST_RESPONSE_XML_BODY_KEY];

                            try
                            {
                                nTmpKey = Convert.ToInt64(sTmpKey);
                            }
                            catch (Exception ex)
                            {
                                LogError(sSubject, "ERROR!  Could not convert EAN (" + sTmpKey + ") to a number", ex);
                            }

                            if (!String.IsNullOrEmpty(sTmpKey))
                            {
                                oTmpFilterArgs = AceXmlReader.ExtractFilterArgs(sTmpChangeBody, poTempProcess.DataAPIConfiguration.RequestFilterArgs);

                                try
                                {
                                    sTmpDataBody = AceXmlReader.PullData(poTempProcess.DataAPIConfiguration.BaseURL, oTmpFilterArgs);
                                }
                                catch (WebException ex)
                                {
                                    sErrMsg = "ERROR!  Web connection issues occurred when handling EAN (" + sTmpKey + ")";

                                    LogError(sSubject, sErrMsg, ex);

                                    sTmpDataBody = CONST_DATA_URL_ISSUE_ERR_MSG;
                                }

                                poProductWriter.InsertProductInstance(poTempProcess.ChangeSeq, nTmpKey, sTmpChangeBody, sTmpDataBody);
                            }
                            else
                                LogError(sSubject, "ERROR!  A provided key was null.");

                        }
                        catch (Exception ex)
                        {
                            sErrMsg = "ERROR!  Could not handle product instance for EAN (" + sTmpKey + ")";

                            LogError(sSubject, sErrMsg, ex);
                        }
                        finally
                        {
                            ++nTotalRecords;
                        }

                        if ((nTotalRecords % 1000) == 0)
                        {
                            LogInfo(sSubject, "Pulled (" + nTotalRecords + ") snapshots to the change_product table");
                            Console.Out.Flush();
                        }
                    } // foreach loop
                }
                catch (WebException ex)
                {
                    sErrMsg = "ERROR!  Web connection issues when attempting to get catalog data...pulling snapshot is stopping now.";
                    LogError(sSubject, sErrMsg, ex);

                    bSuccess = false;
                }
                catch (SqlException ex)
                {
                    sErrMsg = "ERROR!  Database issues when attempting to get catalog data...pulling snapshot is stopping now.";
                    LogError(sSubject, sErrMsg, ex);

                    bSuccess = false;
                }
                catch (Exception ex)
                {
                    sErrMsg = "ERROR!  General issue when attempting to get catalog data...pulling snapshot is stopping now.";
                    LogError(sSubject, sErrMsg, ex);

                    bSuccess = false;
                }
            }

            return bSuccess;
        }

        /// <summary>
        /// 
        /// According to the direction of the metadata of the configured process, this method will retrieve data through
        /// a specified REST API in regard to a particular list of identified records.
        /// 
        /// <param name="poProcessWriter">The writer that will record the progress of this instance for the configured Process (represented by 'poProcess')</param>
        /// <param name="poRecordWriter">The writer that will record the raw payload for each record retrieved in this run</param>
        /// <param name="poProcess">The structure that represents the Process being currently run</param>
        /// <returns>The ChangeSeq (i.e., PID) assigned to this particular instance of the Process being run</returns>
        /// </summary>
        private long PullDataForHardCodedKeys(AceChangeProcessWriter poProcessWriter, AceChangeRecordWriter poRecordWriter, AceProcess poProcess)
        {
            long   nTmpKey        = -1;
            string sTmpChangeBody = "";
            string sTmpDataBody   = "";
            string sInfoMsg       = "";
            string sErrMsg        = "";
            string sSubject       = "AceEngine::PullDataForHardCodedKeys()";

            Dictionary<string, string> oTmpFilterArgs = new Dictionary<string, string>();

            if ((poProcess.ChangeAPIConfiguration.KeyList == null) || (poProcess.ChangeAPIConfiguration.KeyList.Count <= 0))
                throw new Exception("ERROR!  No expected items found in the hard-coded key list.");

            poProcess.ChangeSeq = poProcessWriter.InsertProcessInstance(poProcess.ProcessID);

            foreach (string sTmpKey in poProcess.ChangeAPIConfiguration.KeyList)
            {
                // The format for this change manifest request can be hard-coded (as it is here) or it could be a part of the configurable metadata
                sTmpChangeBody = String.Format(AceXmlReader.CONST_DEFAULT_CHG_MANIFEST_REQUEST_XML_BODY, sTmpKey);

                try
                {
                    nTmpKey = Convert.ToInt64(sTmpKey);
                }
                catch (Exception ex)
                {
                    LogError(sSubject,  "ERROR!  Could not convert EAN (" + sTmpKey + ") to a number",  ex);
                }

                oTmpFilterArgs = AceXmlReader.ExtractFilterArgs(sTmpChangeBody, poProcess.DataAPIConfiguration.RequestFilterArgs);
                sTmpDataBody   = AceXmlReader.PullData(poProcess.DataAPIConfiguration.BaseURL, oTmpFilterArgs);

                if (!String.IsNullOrEmpty(sTmpKey))
                    poRecordWriter.InsertProductInstance(poProcess.ChangeSeq, nTmpKey, sTmpChangeBody, sTmpDataBody);
                else
                    LogError(sSubject, "ERROR!  Provided key was null");
            }

            return poProcess.ChangeSeq;
        }

        /// <summary>
        /// 
        /// According to the direction of the metadata for each configured process, this method will then determine the next actions
        /// that should be taken, using various state data (like the status of the last run) to make a determination.  Then, it will
        /// retrieve data through a specified REST API and then persist the returned raw payloads into a table for later usage.
        /// 
        /// <param name="poProcessWriter">The writer that will record the progress of this instance for the configured Process (represented by 'poTempProcess')</param>
        /// <param name="poProductWriter">The writer that will record the raw payload for each record retrieved in this run</param>
        /// <param name="poTempProcess">The structure that represents the Process being currently run</param>
        /// <returns>The ChangeSeq (i.e., PID) assigned to this particular instance of the Process being run</returns>
        /// </summary>
        private long PullDataSnapshot(AceChangeProcessWriter poProcessWriter, AceChangeRecordWriter poProductWriter, AceProcess poTempProcess)
        {
            int    nTotalRecords = 0;
            string sSubject      = "AceEngine::PullDataSnapshot()";

            StringBuilder sbLastAnchor = new StringBuilder();

            Dictionary<string, string> oTmpFilterArgs = new Dictionary<string, string>();

            // If there is no change URL specified, we can assume that the records are retrieved as either a hard-coded list or a full set
            if (String.IsNullOrEmpty(poTempProcess.ChangeAPIConfiguration.BaseURL.Trim()))
            {
                long nChangeSeq = -1;

                try
                {
                    nTotalRecords = poTempProcess.ChangeAPIConfiguration.KeyList.Count;

                    nChangeSeq = PullDataForHardCodedKeys(poProcessWriter, poProductWriter, poTempProcess);
                    return nChangeSeq;
                }
                finally
                {
                    if (nChangeSeq > 0)
                        poProcessWriter.SetProcessComplete(poTempProcess.ChangeSeq, poTempProcess.ProcessID, nTotalRecords);
                }
            }

            // If there is a change URL specified, we process normally by acquiring the delta manifest and then retrieving the delta records
            if ((poTempProcess.ChangeSeq = poProcessWriter.DetectPreviousFailure(poTempProcess.ProcessID, sbLastAnchor)) > 0)
            {
                string sLastAnchor = sbLastAnchor.ToString();

                if (!String.IsNullOrEmpty(sLastAnchor))
                    poTempProcess.ChangeAPIConfiguration.CurrentAnchor = sLastAnchor;
            }
            else
                poTempProcess.ChangeSeq = poProcessWriter.InsertProcessInstance(poTempProcess.ProcessID);

            // Indicates that the first pull of the delta data has not yet happened
            if (String.IsNullOrEmpty(poTempProcess.ChangeAPIConfiguration.CurrentAnchor))
            {
                sbLastAnchor = new StringBuilder();

                DateTime oMaxStartDtime = poProcessWriter.GetMaxStartDtime(poTempProcess.ProcessID, sbLastAnchor);

                if (!String.IsNullOrEmpty(poTempProcess.ChangeAPIConfiguration.SinceURLArg))
                {
                    string sSinceURLArg = poTempProcess.ChangeAPIConfiguration.SinceURLArg;

                    if (sbLastAnchor.Length <= 0)
                        poTempProcess.ChangeAPIConfiguration.CurrentAnchor = sbLastAnchor.ToString();
                    else if (poTempProcess.ChangeAPIConfiguration.RequestFilterArgs.ContainsKey(sSinceURLArg))
                    {
                        string sSinceValue = poTempProcess.ChangeAPIConfiguration.RequestFilterArgs[sSinceURLArg];
                        if (sSinceValue.EndsWith("d"))
                        {
                            int nDays = Convert.ToInt32(sSinceValue.Remove(sSinceValue.IndexOf('d')));
                            DateTime oSinceDtime = DateTime.Now.Subtract(new TimeSpan(nDays, 0, 0, 0, 0));

                            // For the Epoch calculation, our local time needs to be converted to universal time (i.e., GMT)
                            TimeSpan epochTimespan = oSinceDtime.ToUniversalTime() - new DateTime(1970, 1, 1);
                            long nMillisecondsSinceEpoch = (long)(epochTimespan.TotalSeconds * 1000);

                            poTempProcess.ChangeAPIConfiguration.RequestFilterArgs[sSinceURLArg] = Convert.ToString(nMillisecondsSinceEpoch);
                        }
                    }
                    else
                    {
                        // For the Epoch calculation, our local time needs to be converted to universal time (i.e., GMT)
                        DateTime tenMinutesEarlier = oMaxStartDtime.Subtract(new TimeSpan(0, 0, 15, 0, 0));
                        TimeSpan epochTimespan = tenMinutesEarlier.ToUniversalTime() - new DateTime(1970, 1, 1);
                        long nMillisecondsSinceEpoch = (long)(epochTimespan.TotalSeconds * 1000);

                        poTempProcess.ChangeAPIConfiguration.RequestFilterArgs[sSinceURLArg] = Convert.ToString(nMillisecondsSinceEpoch);
                    }
                }
            }

            poProcessWriter.CurrentProcessID = poTempProcess.ProcessID;
            poProcessWriter.CurrentChangeSeq = poTempProcess.ChangeSeq;

            if (EnumerateApiAndPersistRawData(poProcessWriter, poProductWriter, poTempProcess))
            {
                LogInfo(sSubject, "Setting the Change ID [" + poTempProcess.ChangeSeq + "] to a success!");
                poProcessWriter.SetProcessComplete(poTempProcess.ChangeSeq, poTempProcess.ProcessID, nTotalRecords);
            }
            else
            {
                LogInfo(sSubject, "Setting the Change ID [" + poTempProcess.ChangeSeq + "] to a failure!");
                poProcessWriter.SetProcessFailure(poTempProcess.ChangeSeq, poTempProcess.ProcessID, poTempProcess.ChangeAPIConfiguration.CurrentAnchor);
            }

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

                                ApplyDataSnapshot(TempProcess);
                                System.Console.Out.Flush();
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
