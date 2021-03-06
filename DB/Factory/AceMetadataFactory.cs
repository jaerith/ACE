﻿using System;
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
       , aca.content_type as aca_content_type
       , aca.request_hdr_args as aca_rq_hdr_args
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

        /// <summary>
        /// 
        /// This method will run a query on the ACE_CFG_PROCESS table and retrieve a list 
        /// of all the IDs for API retrieval jobs that are currently active.
        /// 
        /// <returns>The IDs of the API retrieval jobs that are currently active</returns>
        /// </summary>
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

        /// <summary>
        /// 
        /// This method will run a query on the ACE_CFG_API table and retrieve all 
        /// the necessary metadata for API retrieval jobs that are currently active.
        /// 
        /// <returns>The IDs of the API retrieval jobs that are currently active</returns>
        /// </summary>
        public List<AceProcess> GetActiveJobs()
        {
            List<long>           oJobIds = new List<long>();
            List<AceProcess> oActiveJobs = new List<AceProcess>();

            Dictionary<int, AceProcess> oActiveJobMap = new Dictionary<int, AceProcess>();

            if (!ValidateDbConnection())
                InitDbMembers();

            oJobIds = GetActiveJobIds();

            foreach (int nTmpJobId in oJobIds)
            {
                string sJobName    = "";
                string sAPIType    = "";
                string sChangeURL  = "";
                string sDataURL    = "";

                AceProcess          oTmpJob = null;
                AceAPIConfiguration oTmpConfig = null;

                GetProcessDetailsCmd.Parameters[@"pid"].Value = nTmpJobId;
                using (SqlDataReader oProcessDetailsReader = GetProcessDetailsCmd.ExecuteReader())
                {
                    oTmpConfig = null;

                    while (oProcessDetailsReader.Read())
                    {
                        if (!oActiveJobMap.Keys.Contains(nTmpJobId))
                        {
                            oTmpJob = new AceProcess(nTmpJobId);

                            oActiveJobMap[nTmpJobId] = oTmpJob;
                        }

                        oTmpJob  = oActiveJobMap[nTmpJobId];
                        sAPIType = oProcessDetailsReader[0].ToString();

                        if (sAPIType == CONST_API_TYPE_CHANGE)
                            oTmpConfig = oTmpJob.ChangeAPIConfiguration = new AceAPIConfiguration();
                        else if (sAPIType == CONST_API_TYPE_DATA)
                            oTmpConfig = oTmpJob.DataAPIConfiguration = new AceAPIConfiguration();

                        if (oTmpConfig != null)
                            SetAPIBasicConfiguration(oProcessDetailsReader, oTmpConfig);
                    }
                }

                if (oTmpJob.ChangeAPIConfiguration != null)
                {
                    GetAPIDetailsCmd.Parameters[@"pid"].Value = nTmpJobId;
                    GetAPIDetailsCmd.Parameters[@"at"].Value  = CONST_API_TYPE_CHANGE;
                    using (SqlDataReader oAPIDetailsReader = GetAPIDetailsCmd.ExecuteReader())
                    {
                        SetAPIDetails(oAPIDetailsReader, oTmpJob.ChangeAPIConfiguration);
                    }
                }

                if (oTmpJob.DataAPIConfiguration != null)
                {
                    GetAPIDetailsCmd.Parameters[@"pid"].Value = nTmpJobId;
                    GetAPIDetailsCmd.Parameters[@"at"].Value  = CONST_API_TYPE_DATA;
                    using (SqlDataReader oAPIDetailsReader = GetAPIDetailsCmd.ExecuteReader())
                    {
                        SetAPIDetails(oAPIDetailsReader, oTmpJob.DataAPIConfiguration);
                    }
                }
            }

            foreach (int nJobId in oActiveJobMap.Keys)
                oActiveJobs.Add(oActiveJobMap[nJobId]);

            return oActiveJobs;
        }

        /// <summary>
        /// 
        /// This method will retrieve a list of keys from a table.  If the key for our target table is simply one column,
        /// we can target this specific list when we start to pull data through the API described by the metadata.
        /// 
        /// NOTE: If there is no key list table mentioned or if the key list table is empty, we will follow the 
        /// normal convention of the API configuration and pull all the data that fits our criteria of taking 
        /// the latest updates.
        /// 
        /// <param name="psKeyListTable">The table that has the specific keys of interest</param>
        /// <param name="psKeyColumn">The column that has the specific keys of interest</param>
        /// <returns>The unique list of keys that we will use to pull specific records through the API</returns>
        /// </summary>
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

        /// <summary>
        /// 
        /// This method will map a type name (mentioned in the metadata) to its respective SqlDbType.
        /// 
        /// <param name="psAttrType">The SqlDbType that is mapped to the verbose type name</param>
        /// <returns>The SqlDbType for the metadata's verbose type name (of a target column)</returns>
        /// </summary>
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

        /// <summary>
        /// 
        /// This method will extract the general information (like the URLs and their respective arguments)
        /// needed in order to drive the retrieval of data through the API.
        /// 
        /// <param name="poProcessDetailsReader">The reader that pulls metadata from the configuration tables</param>
        /// <param name="poTmpConfig">The structure that will hold all of the extract config metadata</param>
        /// <returns>None.</returns>
        /// </summary>
        private void SetAPIBasicConfiguration(SqlDataReader poProcessDetailsReader, AceAPIConfiguration poTmpConfig)
        {
            poTmpConfig.BaseURL = poProcessDetailsReader["aca_base_url"].ToString();
            poTmpConfig.ApplyBuckets = new Dictionary<string, AceAPIBucket>();

            string   sBucketList = poProcessDetailsReader["aca_bucket_list"].ToString();
            string[] oBucketList = sBucketList.Split(CONST_BUCKET_LIST_DELIM);
            foreach (string sBucketName in oBucketList)
                poTmpConfig.ApplyBuckets[sBucketName] = new AceAPIBucket();

            poTmpConfig.SinceURLArg     = poProcessDetailsReader["aca_since_url_arg_nm"].ToString();
            poTmpConfig.AnchorIndicator = poProcessDetailsReader["aca_anchor_ind_tag_nm"].ToString();
            poTmpConfig.AnchorElement   = poProcessDetailsReader["aca_anchor_val_tag_nm"].ToString();

            string   sAnchorFilterArgs = poProcessDetailsReader["aca_anchor_filter_args"].ToString();
            string[] oAnchorFilterList = sAnchorFilterArgs.Split(CONST_BUCKET_LIST_DELIM);
            foreach (string sTmpAnchorFilter in oAnchorFilterList)
            {
                if (sTmpAnchorFilter.Contains("="))
                {
                    string[] oAnchorFilterPair = sTmpAnchorFilter.Split(CONST_PARAM_VAL_DELIM);
                    if (oAnchorFilterPair.Length == 2)
                        poTmpConfig.AnchorFilterArgs[oAnchorFilterPair[0]] = oAnchorFilterPair[1];
                }
            }

            string   sRequestFilterArgs = poProcessDetailsReader["aca_request_filter_args"].ToString();
            string[] oRequestFilterList = sRequestFilterArgs.Split(CONST_BUCKET_LIST_DELIM);
            foreach (string sTmpRequestFilter in oRequestFilterList)
            {
                if (sTmpRequestFilter.Contains("="))
                {
                    string[] oRequestFilterPair = sTmpRequestFilter.Split(CONST_PARAM_VAL_DELIM);
                    if (oRequestFilterPair.Length == 2)
                        poTmpConfig.RequestFilterArgs[oRequestFilterPair[0]] = oRequestFilterPair[1];
                }
            }

            poTmpConfig.ResponseFilterPath = poProcessDetailsReader["aca_xpath_resp_filter"].ToString();
            poTmpConfig.TargetTag          = poProcessDetailsReader["aca_target_chld_tag"].ToString();
            poTmpConfig.TargetKeyTag       = poProcessDetailsReader["aca_target_chld_key_tag"].ToString();

            if (!poProcessDetailsReader.IsDBNull(11))
            {
                string sKeyList = poProcessDetailsReader["aca_target_key_list"].ToString();

                if (sKeyList.Contains("_"))
                    poTmpConfig.KeyList = GetAllKeysFromTable(sKeyList, poTmpConfig.TargetKeyTag);
                else
                {
                    string[] oKeyList = poProcessDetailsReader["aca_target_key_list"].ToString().Split(CONST_BUCKET_LIST_DELIM);

                    poTmpConfig.KeyList.UnionWith(oKeyList);
                    // poTmpConfig.KeyList.InsertRange(0, oKeyList);
                }
            }

            string sContentType = poProcessDetailsReader["aca_content_type"].ToString();
            if (sContentType.ToUpper() == "XML")
                poTmpConfig.DataContentType = ContentType.XML;
            else if (sContentType.ToUpper() == "JSON")
                poTmpConfig.DataContentType = ContentType.JSON;
            else
                poTmpConfig.DataContentType = ContentType.XML;

            string   sRequestHeaderArgs = poProcessDetailsReader["aca_rq_hdr_args"].ToString();
            string[] oRequestHeaderList = sRequestHeaderArgs.Split(CONST_BUCKET_LIST_DELIM);
            foreach (string sTmpRequestHeader in oRequestHeaderList)
            {
                if (sTmpRequestHeader.Contains("="))
                {
                    string[] oRequestHeaderPair = sTmpRequestHeader.Split(CONST_PARAM_VAL_DELIM);
                    if (oRequestHeaderPair.Length == 2)
                        poTmpConfig.RequestHeaderArgs[oRequestHeaderPair[0]] = oRequestHeaderPair[1];
                }
            }
        }

        /// <summary>
        /// 
        /// This method will extract the detailed information (like the payload's tags mapping to which 
        /// columns of a staging table) needed in order to drive the retrieval of data through the API.
        /// 
        /// <param name="poProcessDetailsReader">The reader that pulls metadata from the configuration tables</param>
        /// <param name="poTmpConfig">The structure that will hold all of the extract config metadata</param>
        /// <returns>None.</returns>
        /// </summary>
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

                    oTmpBucket.AddTargetColumn(sAttrName, oOraDbType, bIsKey, nAttrLen, sAttrXPath, bIsXmlBody); 
                }
            }
        }

        public bool ValidateDbConnection()
        {
            return ((DbConnection != null) && (DbConnection.State == System.Data.ConnectionState.Open));
        }
    }
}
