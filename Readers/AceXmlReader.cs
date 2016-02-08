using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

using ACE.DB.Classes;

namespace ACE.Readers
{
    /// <summary>
    /// 
    /// This class will use the configuration (in the metadata) in order to properly enumerate
    /// through the data set that is available for retrieval through the designated API.
    ///     
    /// </summary>
    public class AceXmlReader : IDisposable, IEnumerable
    {
        #region CONSTANTS

        public delegate bool FoundNewAnchor(string psNewAnchor);

        public const int    CONST_WEB_REQUEST_TIMEOUT_MS    = 60000;
        public const string CONST_DEFAULT_RESPONSE_XML_BODY = "body";

        #endregion

        #region PROPERTIES

        private AceAPIConfiguration APIConfiguration = null;

        public FoundNewAnchor FoundNewAnchorCallback { get; set; }

        #endregion

        public AceXmlReader(AceAPIConfiguration poConfiguration)
        {
            APIConfiguration       = poConfiguration;
            FoundNewAnchorCallback = null;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// 
        /// This method will compile a final list of arguments for a REST API through two steps:
        /// 
        /// 1.) It will accept the provided metadata (poTemplateFilterArgs) and use most of them in order 
        /// to find a list of arguments that are literal values (which will be added to the final list).
        /// Any non-literal values (i.e., anything that starts with "//") will be stored in a container.
        /// 
        /// 2.) The stored non-literals will be treated as XPath directions, used to draw out additional values
        /// from the provided XML body.  These values will be added to the final list.
        /// 
        /// The final list will then be used to assemble the query string for our call of the intended REST API.
        /// 
        /// <param name="psXmlBody">The XML body (likely a manifest payload) that can provide more arguments for the REST API</param>
        /// <param name="poTemplateFilterArgs">The metadata that describes the process for our assembly of API arguments</param>
        /// <returns>The final list of arguments to use when calling the intended REST API</returns>
        /// </summary>
        public static Dictionary<string, string> ExtractFilterArgs(string psXmlBody, Dictionary<string, string> poTemplateFilterArgs)
        {
            List<string> oTempValues = new List<string>();

            Dictionary<string, string> oFilterArgs      = new Dictionary<string, string>();
            Dictionary<string, string> oFinalFilterArgs = new Dictionary<string, string>();

            foreach (string sTmpKey in poTemplateFilterArgs.Keys)
            {
                string sTmpValue = poTemplateFilterArgs[sTmpKey];

                if (sTmpValue.StartsWith("//"))
                    oFilterArgs[sTmpKey] = sTmpValue;
                else
                    oFinalFilterArgs[sTmpKey] = sTmpValue;
            }

            foreach (string sTmpKey in oFilterArgs.Keys)
            {
                string sXPath = poTemplateFilterArgs[sTmpKey];
                HashSet<string> oItemSet = new HashSet<string>();
                StringBuilder sbItemList = new StringBuilder();
                XDocument oXDoc = XDocument.Load(new StringReader(psXmlBody));

                foreach (XElement oTmpElement in oXDoc.XPathSelectElements(sXPath))
                {
                    if (!String.IsNullOrEmpty(oTmpElement.Value))
                        oItemSet.Add(oTmpElement.Value);
                }

                if (oItemSet.Count > 0)
                {
                    foreach (string sTmpValue in oItemSet)
                    {
                        if (sbItemList.Length > 0)
                            sbItemList.Append(",");

                        sbItemList.Append(sTmpValue);
                    }

                    oFinalFilterArgs[sTmpKey] = sbItemList.ToString();
                }
            }

            return oFinalFilterArgs;
        }

        /// <summary>
        /// 
        /// This method will format a GET request URL for our target REST API.  In this case, the request 
        /// will make a subsequent (i.e., not initial) call in the enumeration of sought data, and it 
        /// will use an anchor point (record from a previous call) along with various other arguments.
        /// 
        /// <param name="poConfig">The configuration's metadata for the REST API</param>
        /// <returns>The formatted call to our intended REST API</returns>
        /// </summary>
        public static string FormatAnchorURL(AceAPIConfiguration poConfig)
        {
            return FormatURL(poConfig.BaseURL, poConfig.AnchorFilterArgs);
        }

        /// <summary>
        /// 
        /// This method will format a GET request URL for our target REST API.  In this case, the request 
        /// will make an initial call in the enumeration of sought data, using the metadata contained
        /// within our configuration.
        /// 
        /// <param name="poConfig">The configuration's metadata for the REST API</param>
        /// <returns>The formatted call to our intended REST API</returns>
        /// </summary>
        public static string FormatRequestURL(AceAPIConfiguration poConfig)
        {
            return FormatURL(poConfig.BaseURL, poConfig.RequestFilterArgs);
        }

        /// <summary>
        /// 
        /// This method will format a GET request URL for our target REST API.
        /// 
        /// <param name="psBaseURL">The base call for the REST API</param>
        /// <param name="poFilterArgs">The query string's arguments and values for the REST API</param>
        /// <returns>The formatted call to our intended REST API</returns>
        /// </summary>
        public static string FormatURL(String psBaseURL, Dictionary<string, string> poFilterArgs)
        {
            string sTempVal = null;
            StringBuilder sbQueryString = new StringBuilder();

            foreach (string sTempParam in poFilterArgs.Keys)
            {
                sTempVal = poFilterArgs[sTempParam];

                if (!String.IsNullOrEmpty(sTempVal))
                {
                    if (sbQueryString.Length > 0)
                        sbQueryString.Append("&");
                    else
                        sbQueryString.Append("?");

                    sbQueryString.Append(sTempParam + "=" + sTempVal);
                }
            }

            return String.Format(psBaseURL + sbQueryString);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) GetEnumerator();
        }

        public AceXmlRecordEnumerator GetEnumerator()
        {
            return new AceXmlRecordEnumerator(this, APIConfiguration);
        }
    }

    public class AceXmlRecordEnumerator : IEnumerator
    {
        private AceXmlReader XmlReader       = null;
        private XDocument    CurrXmlResponse = null;

        private bool mbFinalSet = false;
        private int  mnIndex    = -1;

        private List<Hashtable> CurrRecordList = null;
        private Hashtable       CurrRecord     = null;

        private AceAPIConfiguration EnumAPIConfiguration = null;

        public AceXmlRecordEnumerator(AceXmlReader poXmlReader, AceAPIConfiguration poConfiguration)
        {
            XmlReader            = poXmlReader;
            EnumAPIConfiguration = poConfiguration;
        }

        public bool MoveNext()
        {
            bool bResult = false;

            return bResult;
        }

        private bool PullNextSet()
        {
            bool bMoreData = false;

            return bMoreData;
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
