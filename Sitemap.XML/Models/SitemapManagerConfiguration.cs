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
using Sitecore.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Sitecore;

namespace Sitemap.XML.Models
{
    public class SitemapManagerConfiguration
    {
        #region Fields

        private readonly string _siteName = string.Empty;

        #endregion

        public SitemapManagerConfiguration(string siteName)
        {
            Assert.IsNotNullOrEmpty(siteName, "siteName");
            _siteName = siteName;
        }


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

        public string SitemapConfigurationItemPath
        {
            get
            {
                return GetValueByName("sitemapConfigurationItemPath")+_siteName;
            }
        }

        public string EnabledTemplates
        {
            get
            {
                return GetValueByNameFromDatabase("Enabled templates");
            }
        }

        public string ExcludeItems
        {
            get
            {
                return GetValueByNameFromDatabase("Exclude items");
            }
        }

        public string ServerUrl
        {
            get
            {
                return GetValueByNameFromDatabase("Server Url");
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

        public string SiteName
        {
            get
            {
                return _siteName;
            }
        }

        public string FileName
        {
            get
            {
                return GetValueByNameFromDatabase("File Name");
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

        private string GetValueByNameFromDatabase(string name)
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

        public static IEnumerable<string> GetSiteNames()
        {
            const string sitemapXmlSystemRootId = "{6003D67E-0000-4A4D-BFB1-11408B9ADCFD}";
            var configRoot = Context.ContentDatabase.GetItem(sitemapXmlSystemRootId);
            if (configRoot == null) return null;

            var configs = configRoot.Children;
            if (!configs.Any()) return null;

            var siteNames = configs.Select(c => c.Name);
            return siteNames;
        }

        public static string GetServerUrl(string siteName)
        {
            string result = string.Empty;

            foreach (XmlNode node in Factory.GetConfigNodes("sitemapVariables/sites/site"))
            {

                if (XmlUtil.GetAttribute("name", node) == siteName)
                {
                    result = XmlUtil.GetAttribute("serverUrl", node);
                    break;
                }
            }

            return result;
        }
    }
}
