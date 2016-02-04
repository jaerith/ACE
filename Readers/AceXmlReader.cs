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

        public static string FormatAnchorURL(AceAPIConfiguration poConfig)
        {
            return FormatURL(poConfig.BaseURL, poConfig.AnchorFilterArgs);
        }

        public static string FormatRequestURL(AceAPIConfiguration poConfig)
        {
            return FormatURL(poConfig.BaseURL, poConfig.RequestFilterArgs);
        }

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
    }
}
