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
        /// <summary>
        /// 
        /// This method will intialize the prepared statements that will be constructed via direction 
        /// from the provided metadata.  These statements will then be used to 
        /// 
        /// <param name="poBucketConfiguration">The data from the configured Process</param>
        /// <returns>The ID representing the instance of the Process that failed previously</returns>
        public bool InitPreparedStatements(AceAPIBucket poBucketConfiguration)
        {
            StringBuilder CountStatement  = new StringBuilder("SELECT COUNT(*) FROM " + poBucketConfiguration.TableName);
            StringBuilder SelectStatement = new StringBuilder("SELECT ");
            StringBuilder InsertStatement = new StringBuilder("INSERT INTO " + poBucketConfiguration.TableName + "(");
            StringBuilder UpdateStatement = new StringBuilder("UPDATE " + poBucketConfiguration.TableName + " SET ");

            foreach (string sTmpColumn in poBucketConfiguration.ColKeys)
            {
                if (SelectStatement.Length > 0)
                    SelectStatement.Append(", ");

                SelectStatement.Append(sTmpColumn);
                InsertStatement.Append(sTmpColumn);
            }

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
