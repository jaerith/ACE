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
    public class AceChangeRecordReader : IDisposable , IEnumerable
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

        public AceProcess ProcessConfiguration
        {
            get { return AceProcess; }
        }

        public AceChangeRecordReader(AceConnectionMetadata poDbConnMetadata, AceProcess poAceProcess)
        {
            DbConnMetadata = poDbConnMetadata;
            AceProcess     = poAceProcess;

            DbConnection     = null;
            RetrieveProducts = null;
            ProductReader    = null;

            InitDBMembers();

            StartEnumerator();
        }

        public void Dispose()
        {
            if (ProductReader != null)
            {
                ProductReader.Close();
                ProductReader.Dispose();
                ProductReader = null;
            }

            if (DbConnection != null)
            {
                DbConnection.Close();
                DbConnection.Dispose();
                DbConnection = null;
            }
        }

        private void InitDBMembers()
        {
            DbConnection = new SqlConnection(DbConnMetadata.DBConnectionString);
            DbConnection.Open();

            RetrieveProducts = new SqlCommand(CONST_RETRIEVE_RECORDS_SQL, DbConnection);
            RetrieveProducts.CommandTimeout = 120;
            RetrieveProducts.Parameters.Add(new SqlParameter(@"cid", SqlDbType.BigInt));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public AceProductEnumerator GetEnumerator()
        {
            return new AceProductEnumerator(this, ProductReader);
        }

        private void StartEnumerator()
        {
            Dispose();
            InitDBMembers();

            RetrieveProducts.Parameters[@"cid"].Value = AceProcess.ChangeSeq;
            ProductReader = RetrieveProducts.ExecuteReader();
        }

        /// <summary>
        /// 
        /// This method will populate a provided Hashtable with data retrieved through the provided SqlDataReader.
        /// After obtaining the data payload of the target record, it will use the metadata's XPath values to 
        /// parse the payload and pull out values of interest.
        /// 
        /// <param name="poNewProductReader">The Reader that is enumerating through the dataset of payloads (which were pulled via the REST API)</param>
        /// <param name="poTmpConfig">The configuration for the currently running Process</param>
        /// <param name="poNewProductRecord">The container that will hold the values parsed from the raw data payload for our record (i.e. product)</param>
        /// <returns>None</returns>
        static public void PopulateProductData(SqlDataReader poNewProductReader, AceAPIConfiguration poTmpConfig, Hashtable poNewProductRecord)
        {
            string    sDataRecord = poNewProductReader[0].ToString();
            string    sXPath      = null;
            XDocument oDataDoc    = null;

            if (!sDataRecord.Contains("<errors>") && !sDataRecord.Contains("<error>"))
            {
                using (StringReader oDataReader = new StringReader(sDataRecord))
                {
                    oDataDoc = XDocument.Load(oDataReader, LoadOptions.PreserveWhitespace);
                }

                foreach (string sTmpBucketName in poTmpConfig.ApplyBuckets.Keys)
                {
                    AceAPIBucket oTempBucket = poTmpConfig.ApplyBuckets[sTmpBucketName];

                    foreach (string sTmpAttrName in oTempBucket.SoughtColXPaths.Keys)
                    {
                        sXPath = oTempBucket.SoughtColXPaths[sTmpAttrName];

                        try
                        {
                            if (oTempBucket.SoughtColXmlBodies.Keys.Contains(sTmpAttrName) && oTempBucket.SoughtColXmlBodies[sTmpAttrName])
                            {
                                if (oDataDoc.XPathSelectElement(sXPath) != null)
                                    poNewProductRecord[sTmpAttrName] = oDataDoc.XPathSelectElement(sXPath).ToString();
                            }
                            else if (oDataDoc.XPathSelectElement(sXPath) != null)
                                poNewProductRecord[sTmpAttrName] = oDataDoc.XPathSelectElement(sXPath).Value;
                        }
                        catch (Exception ex)
                        {
                            // Any logging should occur here
                            throw ex;
                        }
                    }
                }
            }
            else
            {
                // Any logging should occur here
                poNewProductRecord["error"] = sDataRecord;
            }
        }
    }

    /// <summary>
    /// 
    /// This subclass will assist with the enumeration of the data records.
    ///     
    /// </summary>
    public class AceProductEnumerator : IEnumerator
    {
        private AceChangeRecordReader RecordReader = null;
        private SqlDataReader         DataReader   = null;
        private Hashtable             CurrRecord   = null;

        public AceProductEnumerator(AceChangeRecordReader poRecordReader, SqlDataReader poDataReader)
        {
            RecordReader = poRecordReader;
            DataReader   = poDataReader;
        }

        public bool MoveNext()
        {
            bool bResult;

            bResult = DataReader.Read();

            if (bResult)
            {
                CurrRecord = new Hashtable();

                AceChangeRecordReader.PopulateProductData(DataReader, RecordReader.ProcessConfiguration.DataAPIConfiguration, CurrRecord);
            }
            else
                CurrRecord = null;

            return bResult;
        }

        public void Reset()
        {
            return;
        }

        public object Current
        {
            get
            {
                return CurrRecord;
            }
        }
    }
}
