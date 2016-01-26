using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACE.DB.Factory
{
    /// <summary>
    /// 
    /// This class serves to load the configuration data from the database tables 
    /// and assemble it within the cache.
    ///     
    /// </summary>
    public class AceMetadataFactory
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
SELECT aca.api_type
       , aca.base_url 
       , aca.bucket_list
       , aca.since_url_arg_nm
       , aca.anchor_ind_tag_nm
       , aca.anchor_val_tag_nm
       , aca.anchor_filter_args
       , aca.request_filter_args
       , aca.xpath_resp_filter
       , aca.target_chld_tag
       , aca.target_chld_key_tag
       , aca.target_key_list
       , aca.pulls_single_item_flag
  FROM ace_cfg_api aca
 WHERE aca.process_id = :pid
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
    process_id = :pid
AND
    api_type = :at
ORDER BY 
    bucket_nm ASC, attr_is_key DESC, attr_nm ASC
";

        #endregion

        #endregion

        public AceMetadataFactory()
        {
        }
    }
}
