using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ACE.DB.Classes;

namespace ACE.Writers
{
    /// <summary>
    /// 
    /// This class will inherit the IApplicable interface and serve as our default class for 
    /// upserting records (parsed from the raw payload) into target columns on the database.
    ///     
    public class AceApplyManager : IDisposable, IApplicable
    {
        private SqlConnection DbConn;

        private SqlCommand CountStatement;
        private SqlCommand InsertStatement;
        private SqlCommand UpdateStatement;
        private SqlCommand RetrieveStatement;

        private AceAPIBucket BucketConfiguration;

        public AceApplyManager(AceAPIBucket poBucketConfiguration)
        {
            DbConn = null;

            CountStatement = InsertStatement = UpdateStatement = CountStatement = null;

            BucketConfiguration = poBucketConfiguration;

            InitPreparedStatements(poBucketConfiguration);
        }

        #region IDisposable() method
        public void Dispose()
        {
            if (DbConn != null)
            {
                DbConn.Close();
                DbConn = null;
            }
        }
        #endregion

        #region IApplicable methods
        public bool InitPreparedStatements(AceAPIBucket poBucketConfiguration)
        {
            // Finish implementation
            return true;
        }


        public bool CompareOldVersusNew(Hashtable poOldRecord, Hashtable poNewRecord)
        {
            // Finish implementation
            return true;
        }


        public bool UpsertRecord(Hashtable poRecord)
        {
            // Finish implementation
            return true;
        }
        #endregion
    }
}
