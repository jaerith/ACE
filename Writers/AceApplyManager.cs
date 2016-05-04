using System;
using System.Collections;
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
    /// This class will inherit the IApplicable interface and serve as our default class for 
    /// upserting records (parsed from the raw payload) into target columns on the database.
    ///     
    public class AceApplyManager : IDisposable, IApplicable
    {
        private SqlConnection DbConn;

        private SqlCommand CountCommand;
        private SqlCommand InsertCommand;
        private SqlCommand UpdateCommand;
        private SqlCommand RetrieveCommand;

        private AceAPIBucket BucketConfiguration;

        public AceApplyManager(AceConnectionMetadata poConnMetadata, AceAPIBucket poBucketConfiguration)
        {
            DbConn = new SqlConnection(poConnMetadata.DBConnectionString);
            DbConn.Open();

            CountCommand = InsertCommand = UpdateCommand = RetrieveCommand = null;

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
            CountCommand    = InitCountCommand(poBucketConfiguration);
            RetrieveCommand = InitSelectCommand(poBucketConfiguration);
            InsertCommand   = InitInsertCommand(poBucketConfiguration);
            UpdateCommand   = InitUpdateCommand(poBucketConfiguration);

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

        #region Support Methods

        private SqlCommand InitCountCommand(AceAPIBucket poBucketConfiguration)
        {
            StringBuilder CountStatement = new StringBuilder("SELECT COUNT(*) FROM " + poBucketConfiguration.TableName);

            return new SqlCommand(CountStatement.ToString(), DbConn);
        }

        private SqlCommand InitSelectCommand(AceAPIBucket poBucketConfiguration)
        {
            StringBuilder SelectStatement = new StringBuilder("SELECT ");
            StringBuilder ClauseStatement = new StringBuilder();

            foreach (string sTmpColumn in poBucketConfiguration.SoughtColumns.Keys)
            {
                if (SelectStatement.Length > 0)
                    SelectStatement.Append(", ");

                SelectStatement.Append(sTmpColumn);

                if (poBucketConfiguration.ColKeys.Contains(sTmpColumn))
                {
                    if (ClauseStatement.Length > 0)
                        ClauseStatement.Append(" AND ");
                    else
                        ClauseStatement.Append(" WHERE ");

                    ClauseStatement.Append(sTmpColumn + " = @" + sTmpColumn);
                }
            }

            SelectStatement.Append(" FROM " + poBucketConfiguration.TableName);
            SelectStatement.Append(ClauseStatement.ToString());

            SqlCommand RetrieveCommand = new SqlCommand(SelectStatement.ToString(), DbConn);

            foreach (string sTmpColumn in poBucketConfiguration.SoughtColumns.Keys)
            {
                if (poBucketConfiguration.ColKeys.Contains(sTmpColumn))
                {
                    SqlDbType TargetType = poBucketConfiguration.SoughtColumns[sTmpColumn];
                    if (poBucketConfiguration.SoughtColLengths.Keys.Contains(sTmpColumn))
                    {
                        int TargetLen = poBucketConfiguration.SoughtColLengths[sTmpColumn];
                        RetrieveCommand.Parameters.Add(new SqlParameter(sTmpColumn, TargetType, TargetLen));
                    }
                    else
                        RetrieveCommand.Parameters.Add(new SqlParameter(sTmpColumn, TargetType));
                }
            }

            return new SqlCommand(SelectStatement.ToString(), DbConn);
        }

        private SqlCommand InitInsertCommand(AceAPIBucket poBucketConfiguration)
        {
            StringBuilder InsertStatement = new StringBuilder("INSERT INTO " + poBucketConfiguration.TableName + "(");

            // Finish implementation

            return new SqlCommand(InsertStatement.ToString(), DbConn);
        }

        private SqlCommand InitUpdateCommand(AceAPIBucket poBucketConfiguration)
        {
            StringBuilder UpdateStatement = new StringBuilder("UPDATE " + poBucketConfiguration.TableName + " SET ");

            // Finish implementation

            return new SqlCommand(UpdateStatement.ToString(), DbConn);
        }

        #endregion

    }
}
