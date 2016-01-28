using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ACE.DB.Classes;

namespace ACE.DB.Factory
{
    /// <summary>
    /// 
    /// This class serves to load the configuration data from the database tables 
    /// and assemble it within the cache.
    ///     
    /// </summary>
    public class AceMetadataFactory : IDisposable
    {
        #region Constants

        private char[] CONST_BUCKET_LIST_DELIM = new char[] {','};
        private char[] CONST_PARAM_VAL_DELIM   = new char[] {'='};

        private const string CONST_API_TYPE_CHANGE = "C";
        private const string CONST_API_TYPE_DATA   = "D";

        private const string CONST_DB_TYPE_VARCHAR = "Varchar2";
        private const string CONST_DB_TYPE_CLOB    = "Clob";
        private const string CONST_DB_TYPE_INT     = "Int32";
        private const string CONST_DB_TYPE_LONG    = "Int64";
        private const string CONST_DB_TYPE_DOUBLE  = "Double";

        #endregion

        #region SQL

        #region Select Active ACE Processes

        private const string CONST_RETRIEVE_ALL_KEYS_FORMAT_SQL =
@"
SELECT {0}
  FROM {1}
ORDER BY add_dtime ASC
";

        #endregion

        #region Select Active ACE Processes

        private const string CONST_RETRIEVE_ACTIVE_PROCESSES_SQL =
@"
SELECT process_id
  FROM ace_cfg_process
 WHERE active_ind = 'Y'
ORDER BY process_id ASC
";

        #endregion

        #region Select an Individual Process

        private string CONST_RETRIEVE_PROCESS_DETAILS_SQL =
@"
SELECT aca.api_type as aca_api_type
       , aca.base_url as aca_base_url
       , aca.bucket_list as aca_bucket_list
       , aca.since_url_arg_nm as aca_since_url_arg_nm
       , aca.anchor_ind_tag_nm as aca_anchor_ind_tag_nm
       , aca.anchor_val_tag_nm as aca_anchor_val_tag_nm
       , aca.anchor_filter_args as aca_anchor_filter_args
       , aca.request_filter_args as aca_request_filter_args
       , aca.xpath_resp_filter as aca_xpath_resp_filter
       , aca.target_chld_tag as aca_target_chld_tag
       , aca.target_chld_key_tag as aca_target_chld_key_tag
       , aca.target_key_list as aca_target_key_list
       , aca.pulls_single_item_flag as aca_pulls_single_item_flag
  FROM ace_cfg_api aca
 WHERE aca.process_id = @pid
ORDER BY aca.api_type ASC
";

        #endregion

        #region Select Process Details

        private const string CONST_RETRIEVE_API_DETAILS_SQL =
@"
SELECT
    bucket_nm
    , target_table_nm
    , attr_nm
    , attr_ora_type
    , attr_ora_type_len
    , attr_is_key
    , attr_xpath
    , attr_is_xml_body
FROM
    ace_cfg_bucket
WHERE
    process_id = @pid
AND
    api_type = @at
ORDER BY 
    bucket_nm ASC, attr_is_key DESC, attr_nm ASC
";

        #endregion

        #endregion

        #region Private Members
        private SqlConnection DbConnection;
        private SqlCommand    GetActiveProcessesCmd;
        private SqlCommand    GetProcessDetailsCmd;
        private SqlCommand    GetAPIDetailsCmd;

        private AceConnectionMetadata ConnectionMetadata;

        #endregion 

        public AceMetadataFactory(AceConnectionMetadata ConnMetadata)
        {
            this.ConnectionMetadata = ConnMetadata;
            
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

        public List<long> GetActiveJobIds()
        {
            int nProcessId = -1;

            List<long> ActiveProcessIds = new List<long>();

            if (!ValidateDbConnection())
                InitDbMembers();

            using (SqlDataReader oActiveProcessesReader = GetActiveProcessesCmd.ExecuteReader())
            {
                while (oActiveProcessesReader.Read() && !oActiveProcessesReader.IsDBNull(0))
                {
                    nProcessId = (int) Convert.ToInt64(oActiveProcessesReader[0].ToString());
                    ActiveProcessIds.Add(nProcessId);
                }
            }

            return ActiveProcessIds;
        }

        public void InitDbMembers()
        {
            DbConnection = new SqlConnection(ConnectionMetadata.DBConnectionString);
            DbConnection.Open();

            GetActiveProcessesCmd = new SqlCommand(CONST_RETRIEVE_ACTIVE_PROCESSES_SQL, DbConnection);

            GetProcessDetailsCmd = new SqlCommand(CONST_RETRIEVE_PROCESS_DETAILS_SQL, DbConnection);
            GetProcessDetailsCmd.CommandTimeout = 60;
            GetProcessDetailsCmd.Parameters.Add(new SqlParameter(@"pid", SqlDbType.Int));

            GetAPIDetailsCmd = new SqlCommand(CONST_RETRIEVE_API_DETAILS_SQL, DbConnection);
            GetAPIDetailsCmd.CommandTimeout = 60;
            GetAPIDetailsCmd.Parameters.Add(new SqlParameter(@"pid", SqlDbType.Int));
            GetAPIDetailsCmd.Parameters.Add(new SqlParameter(@"at",  SqlDbType.Char, 1));
        }

        private HashSet<string> GetAllKeysFromTable(string psKeyListTable, string psKeyColumn)
        {
            string          sTmpKey       = null;
            string          sQuery        = String.Format(CONST_RETRIEVE_ALL_KEYS_FORMAT_SQL, psKeyColumn, psKeyListTable);
            HashSet<string> oKeyList      = new HashSet<string>();
            SqlCommand      oKeyRetrieval = new SqlCommand(sQuery, DbConnection);

            using (SqlDataReader oKeyReader = oKeyRetrieval.ExecuteReader())
            {
                while (oKeyReader.Read())
                {
                    if (!oKeyReader.IsDBNull(0))
                    {
                        sTmpKey = oKeyReader[0].ToString();
                        oKeyList.Add(sTmpKey);
                    }
                }
            }

            return oKeyList;
        }

        private SqlDbType GetMappedOraDbType(string psAttrType)
        {
            if (psAttrType == CONST_DB_TYPE_VARCHAR)
                return SqlDbType.VarChar;
            else if (psAttrType == CONST_DB_TYPE_CLOB)
                return SqlDbType.Text;
            else if (psAttrType == CONST_DB_TYPE_INT)
                return SqlDbType.Int;
            else if (psAttrType == CONST_DB_TYPE_LONG)
                return SqlDbType.BigInt;
            else if (psAttrType == CONST_DB_TYPE_DOUBLE)
                return SqlDbType.Decimal;
            else
                return SqlDbType.VarChar;
        }

        private void SetAPIDetails(SqlDataReader poAPIDetailsReader, AceAPIConfiguration poTmpConfig)
        {
            while (poAPIDetailsReader.Read())
            {
                string sBucketName = poAPIDetailsReader["bucket_nm"].ToString();
                string sTableName  = poAPIDetailsReader["target_table_nm"].ToString();

                if (!poTmpConfig.ApplyBuckets.Keys.Contains(sBucketName))
                    poTmpConfig.ApplyBuckets[sBucketName] = new AceAPIBucket(sBucketName, sTableName);

                AceAPIBucket oTmpBucket = poTmpConfig.ApplyBuckets[sBucketName];

                if (String.IsNullOrEmpty(oTmpBucket.BucketName))
                {
                    oTmpBucket.BucketName = sBucketName;
                    oTmpBucket.TableName  = sTableName;
                }

                // Add new row to bucket here
                if (!poAPIDetailsReader.IsDBNull(2))
                {
                    string sAttrName      = poAPIDetailsReader["attr_nm"].ToString();
                    string sAttrType      = poAPIDetailsReader["attr_ora_type"].ToString();
                    string sAttrLen       = poAPIDetailsReader["attr_ora_type_len"].ToString();
                    string sAttrIsKey     = poAPIDetailsReader["attr_is_key"].ToString();
                    string sAttrXPath     = poAPIDetailsReader["attr_xpath"].ToString();
                    string sAttrIsXmlBody = poAPIDetailsReader["attr_is_xml_body"].ToString();

                    int nAttrLen = -1;
                    if (!String.IsNullOrEmpty(sAttrLen))
                        nAttrLen = Convert.ToInt32(sAttrLen);

                    SqlDbType oOraDbType = SqlDbType.VarChar;
                    if (!String.IsNullOrEmpty(sAttrType))
                        oOraDbType = GetMappedOraDbType(sAttrType);

                    bool bIsKey = false;
                    if (!String.IsNullOrEmpty(sAttrIsKey))
                        bIsKey = (sAttrIsKey == "Y") ? true : false;

                    bool bIsXmlBody = false;
                    if (!String.IsNullOrEmpty(sAttrIsXmlBody))
                        bIsXmlBody = (sAttrIsXmlBody == "Y") ? true : false;

                    oTmpBucket.AddAttribute(sAttrName, oOraDbType, bIsKey, nAttrLen, sAttrXPath, bIsXmlBody); 
                }
            }
        }

        public bool ValidateDbConnection()
        {
            return ((DbConnection != null) && (DbConnection.State == System.Data.ConnectionState.Open));
        }
    }
}
