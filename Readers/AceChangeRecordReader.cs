using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using ACE.DB.Classes;
using ACE.DB.Factory;

namespace ACE.Readers
{
    /// <summary>
    /// 
    /// Once an instance of a Process has run and recorded the payloads regarding a 
    /// specific record (i.e., product), this class will provide the functionality to
    /// iterate through the record's data payloads and parse them into manageable buckets.
    ///     
    /// </summary>
    public class AceChangeRecordReader : IDisposable // , IEnumerable
    {
        #region SQL

        private const string CONST_RETRIEVE_RECORDS_SQL =
@"
SELECT 
    cp.data
FROM 
    ace_change_product cp
WHERE
    change_id = @cid
ORDER BY
    cp.id ASC
";

        #endregion

        private AceConnectionMetadata DbConnMetadata;
        private AceProcess            AceProcess;

        private SqlConnection DbConnection     = null;
        private SqlCommand    RetrieveProducts = null;
        private SqlDataReader ProductReader    = null;

        public AceChangeRecordReader(AceConnectionMetadata poDbConnMetadata, AceProcess poAceProcess)
        {
            DbConnMetadata = poDbConnMetadata;
            AceProcess     = poAceProcess;

            // InitDBMembers();
        }

        public void Dispose()
        {
        }
    }
}
