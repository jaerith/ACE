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

            InitPreparedStatements();
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
        /// <returns>The ID representing the instance of the Process that failed previously</returns>
        /// </summary>
        public bool InitPreparedStatements()
        {
            CountCommand    = InitCountCommand(BucketConfiguration);
            RetrieveCommand = InitSelectCommand(BucketConfiguration);
            InsertCommand   = InitInsertCommand(BucketConfiguration);
            UpdateCommand   = InitUpdateCommand(BucketConfiguration);

            return true;
        }

        /// <summary>
        /// 
        /// This method will make an accurate comparison the contents of the two records, the old and the new.
        /// It will be accurate by using comparisons based on type.  For example, it will convert certain strings 
        /// to numeric values and then compare them.
        /// 
        /// <param name="poOldRecord">The old record</param>
        /// <param name="poNewRecord">The new record</param>
        /// <returns>The boolean indicating whether or not the two records are exactly the same</returns>
        /// </summary>
        public bool CompareOldVersusNew(Hashtable poOldRecord, Hashtable poNewRecord)
        {
            bool bSameIndicator = true;

            foreach (string sNewKey in poNewRecord.Keys)
            {
                if (poOldRecord.ContainsKey(sNewKey))
                {
                    string sOldValue = (string) poOldRecord[sNewKey];
                    string sNewValue = (string) poNewRecord[sNewKey];

                    // Finish implementation
                }
            }
            
            return bSameIndicator;
        }

        /// <summary>
        /// 
        /// This method upsert the record into the target table and columns defined by a logical Bucket.
        /// 
        /// <param name="poRecord">The record whose contents will be upserted into the table</param>
        /// <returns>The boolean indicating whether or not the upsert succeeded</returns>
        /// </summary>
        public bool UpsertRecord(Hashtable poRecord)
        {
            bool bSuccess = false;

            SetParemeters(UpdateCommand, BucketConfiguration, poRecord);
            if (UpdateCommand.ExecuteNonQuery() <= 0)
            {
                SetParemeters(InsertCommand, BucketConfiguration, poRecord);
                if (InsertCommand.ExecuteNonQuery() <= 0)
                    bSuccess = false;
            }

            return bSuccess;
        }
        #endregion

        #region Support Methods

        /// <summary>
        /// 
        /// This method will initialize the Count prepared statement.
        /// 
        /// <param name="poBucketConfiguration">The configuration for the logical bucket (i.e., a table and a subset of its columns)</param>
        /// <returns>Prepared statement that returns the Count of rows in the target table</returns>
        /// </summary>
        private SqlCommand InitCountCommand(AceAPIBucket poBucketConfiguration)
        {
            StringBuilder CountStatement = new StringBuilder("SELECT COUNT(*) FROM " + poBucketConfiguration.TableName);

            return new SqlCommand(CountStatement.ToString(), DbConn);
        }

        /// <summary>
        /// 
        /// This method will initialize the Select prepared statement.
        /// 
        /// <param name="poBucketConfiguration">The configuration for the logical bucket (i.e., a table and a subset of its columns)</param>
        /// <returns>Prepared statement that returns a particular row based on the provided key</returns>
        /// </summary>
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

            PrepareParemeters(RetrieveCommand, poBucketConfiguration);

            return RetrieveCommand;
        }

        /// <summary>
        /// 
        /// This method will initialize the Insert prepared statement.
        /// 
        /// <param name="poBucketConfiguration">The configuration for the logical bucket (i.e., a table and a subset of its columns)</param>
        /// <returns>Prepared statement that inserts a row to the logical Bucket</returns>
        /// </summary>
        private SqlCommand InitInsertCommand(AceAPIBucket poBucketConfiguration)
        {
            StringBuilder InsertStatement = new StringBuilder();
            StringBuilder ValuesClause    = new StringBuilder();

            foreach (string sTmpColumn in poBucketConfiguration.SoughtColumns.Keys)
            {
                if (InsertStatement.Length > 0)
                {
                    InsertStatement.Append(", ");
                    ValuesClause.Append(",");
                }
                else
                {
                    InsertStatement.Append("INSERT INTO " + poBucketConfiguration.TableName + "(");
                    ValuesClause.Append(" VALUES(");
                }

                InsertStatement.Append(sTmpColumn);
                ValuesClause.Append("@" + sTmpColumn);
            }

            
            ValuesClause.Append(")");
            InsertStatement.Append(")" + ValuesClause.ToString());

            SqlCommand NewInsertCommand = new SqlCommand(InsertStatement.ToString(), DbConn);

            PrepareParemeters(NewInsertCommand, poBucketConfiguration);

            return NewInsertCommand;
        }

        /// <summary>
        /// 
        /// This method will initialize the Insert prepared statement.
        /// 
        /// <param name="poBucketConfiguration">The configuration for the logical bucket (i.e., a table and a subset of its columns)</param>
        /// <returns>Prepared statement that inserts a row to the logical Bucket</returns>
        /// </summary>
        private SqlCommand InitUpdateCommand(AceAPIBucket poBucketConfiguration)
        {
            StringBuilder UpdateStatement = new StringBuilder();
            StringBuilder UpdateClause    = new StringBuilder();

            foreach (string sTmpColumn in poBucketConfiguration.SoughtColumns.Keys)
            {
                if (UpdateStatement.Length > 0)
                {
                    UpdateStatement.Append(", ");
                    UpdateClause.Append(" AND ");
                }
                else 
                {
                    UpdateStatement.Append("UPDATE " + poBucketConfiguration.TableName + " SET ");
                    UpdateClause.Append(" WHERE ");
                }

                if (!poBucketConfiguration.ColKeys.Contains(sTmpColumn))
                    UpdateStatement.Append(sTmpColumn + " = @" + sTmpColumn);
                else
                    UpdateClause.Append(sTmpColumn + " = @" + sTmpColumn);
            }

            UpdateStatement.Append(")" + UpdateClause.ToString());

            SqlCommand NewUpdateCommand = new SqlCommand(UpdateStatement.ToString(), DbConn);

            PrepareParemeters(NewUpdateCommand, poBucketConfiguration);

            return new SqlCommand(UpdateStatement.ToString(), DbConn);
        }

        /// <summary>
        /// 
        /// This method will create the parameters for the prepared statement.
        /// 
        /// <param name="poCommand">The preparement statement to which we will add the needed parameters</param>
        /// <param name="poBucketConfiguration">The configuration for the logical bucket (i.e., a table and a subset of its columns)</param>
        /// <returns>None</returns>
        /// </summary>
        private void PrepareParemeters(SqlCommand poCommand, AceAPIBucket poBucketConfiguration)
        {
            foreach (string sTmpColumn in poBucketConfiguration.SoughtColumns.Keys)
            {
                SqlDbType TargetType = poBucketConfiguration.SoughtColumns[sTmpColumn];
                if (poBucketConfiguration.SoughtColLengths.Keys.Contains(sTmpColumn))
                {
                    int TargetLen = poBucketConfiguration.SoughtColLengths[sTmpColumn];
                    poCommand.Parameters.Add(new SqlParameter(sTmpColumn, TargetType, TargetLen));
                }
                else
                    poCommand.Parameters.Add(new SqlParameter(sTmpColumn, TargetType));
            }
        }

        /// <summary>
        /// 
        /// This method will set the parameters' values for the prepared statement.
        /// 
        /// <param name="poCommand">The preparement statement to which we will set the needed parameters</param>
        /// <param name="poBucketConfiguration">The configuration for the logical bucket (i.e., a table and a subset of its columns)</param>
        /// <param name="poRecord">The record whose values will be used to set the prepared statement</param>
        /// <returns>None</returns>
        /// </summary>
        private void SetParemeters(SqlCommand poCommand, AceAPIBucket poBucketConfiguration, Hashtable poRecord)
        {
            foreach (string sTmpColumn in poBucketConfiguration.SoughtColumns.Keys)
            {
                if (poRecord.ContainsKey(sTmpColumn))
                {
                    string sValue = (string) poRecord[sTmpColumn];

                    if (!String.IsNullOrEmpty(sValue))
                        poCommand.Parameters[sTmpColumn].Value = sValue;
                    else
                        poCommand.Parameters[sTmpColumn].Value = DBNull.Value;
                }
            }
        }

        #endregion

    }
}
