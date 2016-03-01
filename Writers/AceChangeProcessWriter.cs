using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ACE.DB.Classes;

namespace ACE.Writers
{
    /// <summary>
    /// 
    /// This class serves to record an instance of a particular job running against its 
    /// configured REST API.
    ///     
    /// </summary>
    public class AceChangeProcessWriter : IDisposable 
    {
        private const string CONST_PROCESS_STATUS_COMPLETED = "C";
        private const string CONST_PROCESS_STATUS_FAILED    = "F";

        #region SQL Statements

        #region Gets Maximum Start Time

        private const string msGetMaxStartDtimeSql =
@"
SELECT
    c.change_id, 
    c.last_anchor, 
    c.start_dtime as max_start_dtime
FROM
    ace_change c,     
    (SELECT max(change_id) as max_change_id  
       FROM ace_change 
      WHERE process_id = @pid
        AND status = @status
    ) c2
WHERE
    c2.max_change_id = c.change_id
";

        #endregion

        #region Gets Previous Process Instance Failure

        private const string msGetPreviousProcessFailureSql =
@"
SELECT
    c.change_id, c.last_anchor
FROM
    ace_change c
WHERE
    c.process_id = @pid
AND
    (c.status = 'F' OR c.status = 'P')
";

        #endregion

        #region Insert Process Instance

        private const string msInsertProcessInstanceSql =
@"
INSERT INTO
    ace_change(process_id, start_dtime)
VALUES
   (@pid, SYSDATE)
RETURNING 
   change_id into @cid
";

        #endregion

        #region Set Change Process Complete

        private const string msSetProcessCompleteSql =
@"
UPDATE
    ace_change
SET
    end_dtime = SYSDATE
    , total_records = @total
    , status = 'C'
    , upd_dtime = SYSDATE
WHERE
    change_id = @cid
AND
    process_id = @pid
";

        #endregion

        #region Set Change Process Complete

        private const string msSetProcessFailureSql =
@"
UPDATE
    ace_change
SET
    status = 'F'
    , last_anchor = @anchor
    , upd_dtime = SYSDATE
WHERE
    change_id = @cid
AND
    process_id = @pid
";

        #endregion

        #region Update Anchor

        private const string msUpdateAnchorSql =
@"
UPDATE
    ace_change
SET
    last_anchor = @anchor
    , upd_dtime = SYSDATE
WHERE
    change_id = @cid
AND
    process_id = @pid    
";

        #endregion

        #endregion

        #region Private Members

        private object DbLock;

        private SqlConnection DbConnection;
        private SqlCommand    GetPreviousProcessFailureCmd;
        private SqlCommand    GetMaxStartDtimeCmd;
        private SqlCommand    InsertNewProcessInstanceCmd;
        private SqlCommand    SetProcessCompleteCmd;
        private SqlCommand    SetProcessFailureCmd;
        private SqlCommand    UpdateProcessAnchorCmd;

        private AceConnectionMetadata ConnMetadata;

        #endregion

        #region Properties

        public int  CurrentProcessID { get; set; }
        public long CurrentChangeSeq { get; set; }

        #endregion

        #region Constructors/Initialization

        public AceChangeProcessWriter(AceConnectionMetadata poStgConnMetadata)
        {
            CurrentProcessID = -1;
            CurrentChangeSeq = -1;

            DbLock = new object();

            ConnMetadata = poStgConnMetadata;

            InitDbMembers();
        }

        public void Dispose()
        {
            if (DbConnection != null)
            {
                DbConnection.Close();
                DbConnection = null;
            }
        }


        private void InitDbMembers()
        {
            lock (DbLock)
            {
                // Works Server
                DbConnection = new SqlConnection(ConnMetadata.DBConnectionString);
                DbConnection.Open();

                GetMaxStartDtimeCmd = new SqlCommand(msGetMaxStartDtimeSql, DbConnection);
                GetMaxStartDtimeCmd.Parameters.Add(new SqlParameter(@"pid",    SqlDbType.BigInt));
                GetMaxStartDtimeCmd.Parameters.Add(new SqlParameter(@"status", SqlDbType.Char, 1));

                GetPreviousProcessFailureCmd = new SqlCommand(msGetPreviousProcessFailureSql, DbConnection);
                GetPreviousProcessFailureCmd.Parameters.Add(new SqlParameter(@"pid", SqlDbType.BigInt));

                InsertNewProcessInstanceCmd = new SqlCommand(msInsertProcessInstanceSql, DbConnection);
                InsertNewProcessInstanceCmd.Parameters.Add(new SqlParameter(@"pid", SqlDbType.BigInt));
                InsertNewProcessInstanceCmd.Parameters.Add(new SqlParameter(@"cid", SqlDbType.BigInt, 24, ParameterDirection.ReturnValue, true, 0, 0, null, DataRowVersion.Current, null));

                SetProcessCompleteCmd = new SqlCommand(msSetProcessCompleteSql, DbConnection);
                SetProcessCompleteCmd.Parameters.Add(new SqlParameter(@"total", SqlDbType.Int));
                SetProcessCompleteCmd.Parameters.Add(new SqlParameter(@"cid",   SqlDbType.BigInt));
                SetProcessCompleteCmd.Parameters.Add(new SqlParameter(@"pid",   SqlDbType.Int));

                SetProcessFailureCmd = new SqlCommand(msSetProcessFailureSql, DbConnection);
                SetProcessFailureCmd.Parameters.Add(new SqlParameter(@"anchor", SqlDbType.VarChar, 512));
                SetProcessFailureCmd.Parameters.Add(new SqlParameter(@"cid",    SqlDbType.BigInt));
                SetProcessFailureCmd.Parameters.Add(new SqlParameter(@"pid",    SqlDbType.Int));

                UpdateProcessAnchorCmd = new SqlCommand(msUpdateAnchorSql, DbConnection);
                UpdateProcessAnchorCmd.Parameters.Add(new SqlParameter(@"anchor", SqlDbType.VarChar, 512));
                UpdateProcessAnchorCmd.Parameters.Add(new SqlParameter(@"cid",    SqlDbType.BigInt));
                UpdateProcessAnchorCmd.Parameters.Add(new SqlParameter(@"pid",    SqlDbType.Int));

            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// This method will retrieve the last run time for our configured process.  If the last process
        /// ran successfully, then only the DateTime will be returned.  However, if the last process failed
        /// during its enumeration of the API (in which it supposedly called the REST API repeatedly to
        /// obtain batch after batch of records), then the recorded URL that was last attempted will be
        /// returned via the provided StringBuilder.
        /// 
        /// <param name="pnProcessID">The ID of the configured Process in which we are interested</param>
        /// <param name="poLastAnchor">The container of the last URL attempt in the previously failed run (i.e., enumeration of the REST API)</param>
        /// <returns>The starting time of the last run for the Process</returns>
        public DateTime GetMaxStartDtime(int pnProcessID, StringBuilder poLastAnchor)
        {
            string   sCompletedChangeId      = "";
            string   sCompletedLastAnchor    = "";
            string   sFailureChangeId        = "";
            string   sFailureLastAnchor      = "";
            DateTime oCompletedMaxStartDtime = DateTime.Now.Subtract(new TimeSpan(30, 0, 0, 0));
            DateTime oFailureMaxStartDtime   = DateTime.Now.Subtract(new TimeSpan(30, 0, 0, 0));
            DateTime oMaxDateStartDtime      = DateTime.Now.Subtract(new TimeSpan(30, 0, 0, 0));

            lock (DbLock)
            {
                GetMaxStartDtimeCmd.Parameters[@"pid"].Value    = pnProcessID;
                GetMaxStartDtimeCmd.Parameters[@"status"].Value = CONST_PROCESS_STATUS_COMPLETED;
                using (SqlDataReader oMaxStartDtimeReader = GetMaxStartDtimeCmd.ExecuteReader())
                {
                    if (oMaxStartDtimeReader.Read())
                    {
                        sCompletedChangeId      = oMaxStartDtimeReader[0].ToString();
                        sCompletedLastAnchor    = oMaxStartDtimeReader[1].ToString();
                        oCompletedMaxStartDtime = oMaxStartDtimeReader.GetDateTime(2);
                    }
                }

                GetMaxStartDtimeCmd.Parameters[@"pid"].Value    = pnProcessID;
                GetMaxStartDtimeCmd.Parameters[@"status"].Value = CONST_PROCESS_STATUS_FAILED;
                using (SqlDataReader oMaxStartDtimeReader = GetMaxStartDtimeCmd.ExecuteReader())
                {
                    if (oMaxStartDtimeReader.Read())
                    {
                        sFailureChangeId      = oMaxStartDtimeReader[0].ToString();
                        sFailureLastAnchor    = oMaxStartDtimeReader[1].ToString();
                        oFailureMaxStartDtime = oMaxStartDtimeReader.GetDateTime(2);
                    }
                }

                if (oFailureMaxStartDtime > oCompletedMaxStartDtime)
                {
                    if ((poLastAnchor != null) && !String.IsNullOrEmpty(sFailureLastAnchor))
                    {
                        oMaxDateStartDtime = oFailureMaxStartDtime;
                        poLastAnchor.Append(sFailureLastAnchor);
                    }
                    else
                        oMaxDateStartDtime = oCompletedMaxStartDtime;                    
                }
                else
                    oMaxDateStartDtime = oCompletedMaxStartDtime;
            }

            return oMaxDateStartDtime;
        }

        /// <summary>
        /// 
        /// This method will insert an instance of the configured Process (i.e., the sought enumeration 
        /// and data retrieval through our target REST API).
        /// 
        /// <param name="pnProcessID">The ID of the configured Process in which we are interested</param>
        /// <returns>The new ID that represents the new instance of this Process</returns>
        public long InsertProcessInstance(int pnProcessID)
        {
            long nNewChangeSeq = -1;

            if (!ValidateDbConnection())
                InitDbMembers();

            lock (DbLock)
            {
                InsertNewProcessInstanceCmd.Parameters[@"process_id"].Value = pnProcessID;

                if (InsertNewProcessInstanceCmd.ExecuteNonQuery() > 0)
                    nNewChangeSeq = Convert.ToInt64(InsertNewProcessInstanceCmd.Parameters[@"newChangeID"].Value.ToString());
                else
                    throw new Exception("ERROR!  Could not create a new Process Instance for ProcessID(" + pnProcessID + ").");
            }

            return nNewChangeSeq;
        }

        /// <summary>
        /// 
        /// This method will update an instance of the configured Process upon a successful run, setting its status 
        /// to Complete and updating the total records handled during this instance's run.
        /// 
        /// <param name="pnChangeSeq">The instance ID of the configured Process</param>
        /// <param name="pnProcessID">The ID of the configured Process in which we are interested</param>
        /// <param name="nTotalRecords">The number of records retrieved through the enumeration of the API</param>
        /// <returns>Indicator of whether or not the update succeeded</returns>
        public bool SetProcessComplete(long pnChangeSeq, int pnProcessID, int nTotalRecords)
        {
            bool bSuccess = true;
            int nErrCd    = 0;

            if (!ValidateDbConnection())
                InitDbMembers();

            lock (DbLock)
            {
                SetProcessCompleteCmd.Parameters[@"totrec"].Value = nTotalRecords;
                SetProcessCompleteCmd.Parameters[@"cid"].Value    = pnChangeSeq;
                SetProcessCompleteCmd.Parameters[@"pid"].Value    = pnProcessID;

                if (SetProcessCompleteCmd.ExecuteNonQuery() <= 0)
                    throw new Exception("ERROR!  AceChangeProcessWriter::SetProcessComplete() -> Could not set the row as 'complete' for ChangeSeq(" + pnChangeSeq + "), ProcessD(" + pnProcessID + ")");

                bSuccess = true;
            }

            return bSuccess;
        }

        /// <summary>
        /// 
        /// This method will update an instance of the configured Process upon a failed run, setting its status 
        /// to Error and recording the current anchor (i.e., parameterized URL) on which the enumeration failed
        /// 
        /// <param name="pnChangeSeq">The instance ID of the configured Process</param>
        /// <param name="pnProcessID">The ID of the configured Process in which we are interested</param>
        /// <param name="psCurrentAnchor">The parameterized URL during which this run failed during the enumeration of the API</param>
        /// <returns>Indicator of whether or not the update succeeded</returns>
        public bool SetProcessFailure(long pnChangeSeq, int pnProcessID, string psCurrentAnchor)
        {
            bool bSuccess = true;
            int nErrCd = 0;

            if (!ValidateDbConnection())
                InitDbMembers();

            lock (DbLock)
            {
                SetProcessFailureCmd.Parameters[@"anchor"].Value = psCurrentAnchor;
                SetProcessFailureCmd.Parameters[@"cid"].Value    = pnChangeSeq;
                SetProcessFailureCmd.Parameters[@"pid"].Value    = pnProcessID;

                if (SetProcessFailureCmd.ExecuteNonQuery() <= 0)
                    throw new Exception("ERROR!  AceChangeProcessWriter::SetProcessFailure() -> Could not set the row as 'failed' for ChangeSeq(" + pnChangeSeq + "), ProcessD(" + pnProcessID + ")");

                bSuccess = true;
            }

            return bSuccess;
        }

        public bool ValidateDbConnection()
        {
            lock (DbLock)
            {
                return ((DbConnection != null) && (DbConnection.State == ConnectionState.Open));
            }
        }

        #endregion
    }
}
