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

        /// <summary>
        /// 
        /// This method will fulfill the contractual obligation of the IEnumerable interface.
        /// 
        /// <returns>The enumerator for our XML reader</returns>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) GetEnumerator();
        }

        /// <summary>
        /// 
        /// This method will provide the actual enumerator for reading a XML payload.
        /// 
        /// <returns>The enumerator for our XML reader</returns>
        /// </summary>
        public AceXmlRecordEnumerator GetEnumerator()
        {
            return new AceXmlRecordEnumerator(this, APIConfiguration);
        }

        /// <summary>
        /// 
        /// This method will assemble the next call to the REST API and then retrieve the
        /// XML payload as a string.
        /// 
        /// <param name="psBaseURL">The base call for the REST API</param>
        /// <param name="poFilterArgs">The query string's arguments and values for the REST API</param>
        /// <returns>The string representation of the XML payload from the REST API</returns>
        /// </summary>
        static public string PullData(string psBaseURL, Dictionary<string, string> poFilterArgs)
        {
            string sRequestURL = AceXmlReader.FormatURL(psBaseURL, poFilterArgs);

            return PullData(WebRequest.Create(sRequestURL));
        }

        /// <summary>
        /// 
        /// This method will make the next call to the REST API and then retrieve the
        /// XML payload as a string.
        /// 
        /// <param name="poAPIRequest">The Request object that represents our call to the REST API</param>
        /// <returns>The string representation of the XML payload from the REST API</returns>
        /// </summary>
        static public string PullData(WebRequest poAPIRequest)
        {
            string sResponseXml = null;

            poAPIRequest.Timeout = CONST_WEB_REQUEST_TIMEOUT_MS;

            for (int nRetryCount = 0; nRetryCount < 3; ++nRetryCount)
            {
                try
                {
                    using (WebResponse oWebAPIResponse = poAPIRequest.GetResponse())
                    {
                        // Get the stream containing content returned by the server.
                        Stream oDataStream = oWebAPIResponse.GetResponseStream();

                        // Open the stream using a StreamReader for easy access.
                        using (StreamReader oWebReader = new StreamReader(oDataStream))
                        {
                            sResponseXml = oWebReader.ReadToEnd();
                        }
                    }

                    break;
                }
                catch (WebException ex)
                {
                    if ((nRetryCount + 1) < 3)
                    {
                        System.Console.WriteLine("DEBUG: Timeout issue with pulling product data for URL(" + poAPIRequest.RequestUri.ToString() +
                                                 ")...attempting to pull the data again...");
                    }
                    else
                        throw ex;
                }
            }

            return sResponseXml;
        }

        /// <summary>
        /// 
        /// This method will assemble the next call to the REST API and then retrieve the
        /// XML payload as a XDocument.
        /// 
        /// <param name="psBaseURL">The base call for the REST API</param>
        /// <param name="poFilterArgs">The query string's arguments and values for the REST API</param>
        /// <returns>The XDocument representation of the XML payload from the REST API</returns>
        /// </summary>
        static public XDocument PullXmlDoc(string psBaseURL, Dictionary<string, string> poFilterArgs)
        {
            string sRequestURL = AceXmlReader.FormatURL(psBaseURL, poFilterArgs);

            return PullXmlDoc(sRequestURL);
        }

        /// <summary>
        /// 
        /// This method will make the next call to the REST API and then retrieve the
        /// XML payload as a XDocument.
        /// 
        /// <param name="psRequestURL">The Request URL for our call to the REST API</param>
        /// <returns>The XDocument representation of the XML payload from the REST API</returns>
        /// </summary>
        static public XDocument PullXmlDoc(string psRequestURL)
        {
            XDocument oXDoc = null;
            WebRequest oWebAPIRequest = null;

            for (int nRetryCount = 0; nRetryCount < 3; ++nRetryCount)
            {
                try
                {
                    oWebAPIRequest = WebRequest.Create(psRequestURL);

                    oWebAPIRequest.Timeout = AceXmlReader.CONST_WEB_REQUEST_TIMEOUT_MS;

                    // If required by the server, set the credentials.
                    oWebAPIRequest.Credentials = CredentialCache.DefaultCredentials;

                    using (WebResponse oWebAPIResponse = oWebAPIRequest.GetResponse())
                    {
                        // Get the stream containing content returned by the server.
                        Stream oDataStream = oWebAPIResponse.GetResponseStream();

                        // Open the stream using a StreamReader for easy access.
                        using (StreamReader oWebReader = new StreamReader(oDataStream))
                        {
                            oXDoc = XDocument.Load(oWebReader, LoadOptions.PreserveWhitespace);
                        }
                    }

                    break;
                }
                catch (WebException ex)
                {
                    if ((nRetryCount + 1) < 3)
                    {
                        if (oWebAPIRequest != null)
                        {
                            System.Console.WriteLine("DEBUG: Timeout (value=" + oWebAPIRequest.Timeout + ") issue with pulling catalog data for URL(" +
                                                     oWebAPIRequest.RequestUri.ToString() + ")...attempting to pull the data again..." + DateTime.Now.ToString());
                        }
                    }
                    else
                        throw ex;
                }
            }

            return oXDoc;
        }

        public void StartEnumerator()
        {
            Dispose();
        }
    }

    /// <summary>
    /// 
    /// This class will serve as the enumerator for a XML payload being read by an instance
    /// of the AceXmlReader class.
    ///     
    /// </summary>
    public class AceXmlRecordEnumerator : IEnumerator
    {
        private AceXmlReader XmlReader       = null;
        private XDocument    CurrXmlResponse = null;

        private bool IsFinalSet = false;
        private int  CurrIndex  = -1;

        private List<Hashtable> CurrRecordList = null;
        private Hashtable       CurrRecord     = null;

        private AceAPIConfiguration EnumAPIConfiguration = null;

        public AceXmlRecordEnumerator(AceXmlReader poXmlReader, AceAPIConfiguration poConfiguration)
        {
            XmlReader            = poXmlReader;
            EnumAPIConfiguration = poConfiguration;
        }

        /// <summary>
        /// 
        /// This method will fulfill the contractual obligation of the IEnumerator interface and 
        /// will traverse to the next available record.
        /// 
        /// <returns>The indicator of whether another record was read from the REST API</returns>
        public bool MoveNext()
        {
            bool bResult = false;

            ++CurrIndex;
            if ((CurrRecordList != null) && (CurrIndex < CurrRecordList.Count))
            {
                bResult    = true;
                CurrRecord = CurrRecordList[CurrIndex];
            }
            else if (!IsFinalSet && PullNextSet())
            {
                ++CurrIndex;

                bResult      = true;
                CurrRecord = CurrRecordList[CurrIndex];
            }

            return bResult;
        }

        private bool PullNextSet()
        {
            bool   bMoreData   = false;

            return bMoreData;
        }

        /// <summary>
        /// 
        /// This method will fulfill the contractual obligation of the IEnumerator interface.
        /// 
        /// <returns>None.</returns>
        public void Reset()
        {
            return;
        }

        /// <summary>
        /// 
        /// This method will fulfill the contractual obligation of the IEnumerator interface by
        /// providing the latest record of the enumeration.
        /// 
        /// <returns>The latest record read via the enumeration</returns>
        public object Current
        {
            get
            {
                return CurrRecord;
            }
        }
    }

}
