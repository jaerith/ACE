using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using ACE.DB.Classes;

namespace ACE.Writers
{
    /// <summary>
    /// 
    /// This class serves to insert an entry with data regarding a certain record that has 
    /// been pulled down through the API.  (Usually, this is done after the enumeration of
    /// change manifest data, when we're in the enumeration of actual data.)  The record will
    /// include the raw payloads from the API in their original format (JSON, XML, etc.), being
    /// both the change manifest payload regarding a record (if available) and the actual data payload
    /// for a record.  In this way, we will archive an actual snapshot of the data at the time 
    /// of the pull through the API.
    /// 
    /// NOTE: In this code, each record is identified through an EAN, which is a 
    /// standard identifier for books. However, it could easily be replaced by another identifier.
    ///     
    public class AceChangeRecordWriter : IDisposable
    {
        #region SQL

        #region Insert Record Instance

        private const string InsertRecordInstanceSql =
@"
INSERT INTO
    ace_change_product(change_id, ean, notification, data)
VALUES
   (@cid, @ean, @notify_body, @data_body)
";

        #endregion

        #endregion

        private object moDbLock;

        private SqlConnection DbConnection;
        private SqlCommand    InsertNewRecordInstance;

        private AceConnectionMetadata ConnectionMetadata;

        public AceChangeRecordWriter(AceConnectionMetadata poConnMetadata)
        {
            moDbLock = new object();

            ConnectionMetadata = poConnMetadata;

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

        public void InitDbMembers()
        {
            lock (moDbLock)
            {
                // Works Server
                DbConnection = new SqlConnection(ConnectionMetadata.DBConnectionString);
                DbConnection.Open();

                InsertNewRecordInstance = new SqlCommand(InsertRecordInstanceSql, DbConnection);
                InsertNewRecordInstance.Parameters.Add(new SqlParameter(@"cid", SqlDbType.BigInt));
                InsertNewRecordInstance.Parameters.Add(new SqlParameter(@"ean", SqlDbType.BigInt));
                InsertNewRecordInstance.Parameters.Add(new SqlParameter(@"notify_body", SqlDbType.Text));
                InsertNewRecordInstance.Parameters.Add(new SqlParameter(@"data_body", SqlDbType.Text));
            }
        }

        /// <summary>
        /// 
        /// This method will insert an entry on behalf of a record that has been pulled down through the
        /// targeted REST API.
        /// 
        /// <param name="pnChangeSeq">The ID of the running instance for our configured Process</param>
        /// <param name="pnEAN">The ID of the record that has been retrieved through the REST API</param>
        /// <param name="psNotificationBody">The raw payload of the change manifest (if available) that triggered the pull of this EAN</param>
        /// <param name="psDataBody">The raw payload of the product's actual data</param>
        /// <returns>Indicator of whether or not the entry has been inserted successfully</returns>
        public bool InsertProductInstance(long pnChangeSeq, long pnEAN, string psNotificationBody, string psDataBody)
        {
            bool bResult = true;

            if (!ValidateDbConnection())
                InitDbMembers();

            lock (moDbLock)
            {
                InsertNewRecordInstance.Parameters[@"cid"].Value = pnChangeSeq;
                InsertNewRecordInstance.Parameters[@"ean"].Value = pnEAN;

                InsertNewRecordInstance.Parameters[@"notify_body"].Value = psNotificationBody;
                InsertNewRecordInstance.Parameters[@"data_body"].Value   = psDataBody;

                if (InsertNewRecordInstance.ExecuteNonQuery() <= 0)
                    throw new Exception("ERROR!  Could not create a new Product Instance for ChangeSeq(" + pnChangeSeq + "), EAN(" + pnEAN + ").");
            }

            return bResult;
        }

        /// <summary>
        /// 
        /// This method will validate the current status of the member property that is the connection to our database.
        /// 
        /// <returns>Indicator of whether or not the DB connection is still alive</returns>
        public bool ValidateDbConnection()
        {
            lock (moDbLock)
            {
                return ((DbConnection != null) && (DbConnection.State == ConnectionState.Open));
            }
        }
    }
}
