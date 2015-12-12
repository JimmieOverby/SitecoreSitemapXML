/* *********************************************************************** *
 * File   : SitemapManagerConfiguration.cs                Part of Sitecore *
 * Version: 1.0.0                                         www.sitecore.net *
 *                                                                         *
 *                                                                         *
 * Purpose: Class for getting config information from db and conf file     *
 *                                                                         *
 * Copyright (C) 1999-2009 by Sitecore A/S. All rights reserved.           *
 *                                                                         *
 * This work is the property of:                                           *
 *                                                                         *
 *        Sitecore A/S                                                     *
 *        Meldahlsgade 5, 4.                                               *
 *        1613 Copenhagen V.                                               *
 *        Denmark                                                          *
 *                                                                         *
 * This is a Sitecore published work under Sitecore's                      *
 * shared source license.                                                  *
 *                                                                         *
 * *********************************************************************** */

using System.Xml;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Xml;
using System.Collections.Specialized;

namespace Sitecore.Modules.SitemapXML
{
    public class SitemapManagerConfiguration
    {
        #region properties



        public static string XmlnsTpl
        {
            get
            {
                return GetValueByName("xmlnsTpl");
            }
        }



        public static string WorkingDatabase
        {
            get
            {
                return GetValueByName("database");
            }
        }

        public static string SitemapConfigurationItemPath
        {
            get
            {
                return GetValueByName("sitemapConfigurationItemPath");
            }
        }

        public static string EnabledTemplates
        {
            get
            {
                return GetValueByNameFromDatabase("Enabled templates");
            }
        }

        public static string ExcludeItems
        {
            get
            {
                return GetValueByNameFromDatabase("Exclude items");
            }
        }

        public static bool IsProductionEnvironment
        {
            get
            {
                string production = GetValueByName("productionEnvironment");
                return !string.IsNullOrEmpty(production) && (production.ToLower() == "true" || production == "1");
            }
        }

        public static bool GenerateRobotsTxt
        {
            get
            {
                string generateRobotsTxt = GetValueByName("generateRobotsTxt");
                return !string.IsNullOrEmpty(generateRobotsTxt) && (generateRobotsTxt.ToLower() == "true" || generateRobotsTxt == "1");
            }
        }

        public static string SitemapIndexFilename
        {
            get
            {
                return GetValueByName("sitemapIndexFilename");
            }
        }
        #endregion properties

        private static string GetValueByName(string name)
        {
            string result = string.Empty;

            foreach (XmlNode node in Factory.GetConfigNodes("sitemapVariables/sitemapVariable"))
            {

                if (XmlUtil.GetAttribute("name", node) == name)
                {
                    result = XmlUtil.GetAttribute("value", node);
                    break;
                }
            }

            return result;
        }

        private static string GetValueByNameFromDatabase(string name)
        {
            string result = string.Empty;

            Database db = Factory.GetDatabase(WorkingDatabase);
            if (db != null)
            {
                Item configItem = db.Items[SitemapConfigurationItemPath];
                if (configItem != null)
                {
                    result = configItem[name];
                }
            }

            return result;
        }

        public static StringDictionary GetSites()
        {
            StringDictionary sites = new StringDictionary();
            foreach (XmlNode node in Factory.GetConfigNodes("sitemapVariables/sites/site"))
            {
                if (!string.IsNullOrEmpty(XmlUtil.GetAttribute("name", node)) && !string.IsNullOrEmpty(XmlUtil.GetAttribute("filename", node)))
                {
                    sites.Add(XmlUtil.GetAttribute("name", node), XmlUtil.GetAttribute("filename", node));
                }

            }
            return sites;
        }

        public static string GetServerUrlBySite(string name)
        {
            string result = string.Empty;

            foreach (XmlNode node in Factory.GetConfigNodes("sitemapVariables/sites/site"))
            {

                if (XmlUtil.GetAttribute("name", node) == name)
                {
                    result = XmlUtil.GetAttribute("serverUrl", node);
                    break;
                }
            }

            return result;
        }
    }
}
