/* *********************************************************************** *
 * File   : SitemapManager.cs                             Part of Sitecore *
 * Version: 1.0.0                                         www.sitecore.net *
 *                                                                         *
 *                                                                         *
 * Purpose: Manager class what contains all main logic                     *
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

using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Sites;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

namespace Sitecore.Modules.SitemapXML
{
    public class SitemapManager
    {
		private Database _db = null;
        private static StringDictionary m_Sites;
        public Database Db
        {
            get
            {
                if (_db == null)
                {
                    _db = Factory.GetDatabase(SitemapManagerConfiguration.WorkingDatabase);
                }
                
                return _db;
            }
        }

        private DeviceItem _defaultDevice = null;
        private DeviceItem DefaultDevice {
            get {
                if (_defaultDevice == null)
                {
                    _defaultDevice = Db.Resources.Devices.GetAll().Where(i => i.IsDefault).FirstOrDefault();
                }

                return _defaultDevice;
            }
        }

        public SitemapManager()
        {
            m_Sites = SitemapManagerConfiguration.GetSites();
            foreach (DictionaryEntry site in m_Sites)
            {
                BuildSiteMap(site.Key.ToString(), site.Value.ToString());
            }

            BuildSiteMapIndex();
        }

        private void BuildSiteMap(string sitename, string sitemapUrlNew)
        {
            Site site = Sitecore.Sites.SiteManager.GetSite(sitename);
            SiteContext siteContext = Factory.GetSite(sitename);
            string rootPath = siteContext.StartPath;

            List<Item> items = GetSitemapItems(rootPath);


            string fullPath = MainUtil.MapPath(string.Concat("/", sitemapUrlNew));
            string xmlContent = this.BuildSitemapXML(items, site, siteContext);

            StreamWriter strWriter = new StreamWriter(fullPath, false);
            strWriter.Write(xmlContent);
            strWriter.Close();

        }

        private void BuildSiteMapIndex()
        {
            string fullPath = MainUtil.MapPath(string.Concat("/", SitemapManagerConfiguration.SitemapIndexFilename));
            string xmlContent = this.BuildSitemapIndexXML();

            StreamWriter strWriter = new StreamWriter(fullPath, false);
            strWriter.Write(xmlContent);
            strWriter.Close();
        }

        private string BuildSitemapIndexXML()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode declarationNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(declarationNode);
            XmlNode urlsetNode = doc.CreateElement("sitemapindex");
            XmlAttribute xmlnsAttr = doc.CreateAttribute("xmlns");
            xmlnsAttr.Value = SitemapManagerConfiguration.XmlnsTpl;
            urlsetNode.Attributes.Append(xmlnsAttr);

            doc.AppendChild(urlsetNode);

            foreach (DictionaryEntry siteEntry in m_Sites)
            {
                Site site = Sitecore.Sites.SiteManager.GetSite(siteEntry.Key.ToString());
                string filename = siteEntry.Value.ToString();

                string serverUrl = SitemapManagerConfiguration.GetServerUrlBySite(site.Name);

                doc = this.BuildSitemapIndexItem(doc, string.Format("{0}/{1}", serverUrl, filename));
            }

            return doc.OuterXml;
        }

        private XmlDocument BuildSitemapIndexItem(XmlDocument doc, string filename)
        {
            string lastMod = HtmlEncode(System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"));

            XmlNode sitemapSetNode = doc.LastChild;

            XmlNode sitemapNode = doc.CreateElement("sitemap");
            sitemapSetNode.AppendChild(sitemapNode);

            XmlNode locNode = doc.CreateElement("loc");
            sitemapNode.AppendChild(locNode);
            locNode.AppendChild(doc.CreateTextNode(filename));

            XmlNode lastmodNode = doc.CreateElement("lastmod");
            sitemapNode.AppendChild(lastmodNode);
            lastmodNode.AppendChild(doc.CreateTextNode(lastMod));

            return doc;
        }

        public bool SubmitSitemapToSearchenginesByHttp()
        {
            if (!SitemapManagerConfiguration.IsProductionEnvironment)
                return false;

            bool result = false;
            Item sitemapConfig = Db.Items[SitemapManagerConfiguration.SitemapConfigurationItemPath];

            if (sitemapConfig != null)
            {
                string engines = sitemapConfig.Fields["Search engines"].Value;
                foreach (string id in engines.Split('|'))
                {
                    Item engine = Db.Items[id];
                    if (engine != null)
                    {
                        string engineHttpRequestString = engine.Fields["HttpRequestString"].Value;
                        foreach (string sitemapUrl in m_Sites.Values)
                            this.SubmitEngine(engineHttpRequestString, sitemapUrl);
                    }
                }
                result = true;
            }

            return result;
        }

        public void RegisterSitemapToRobotsFile()
        {

            string robotsPath = MainUtil.MapPath(string.Concat("/", "robots.txt"));
            StringBuilder sitemapContent = new StringBuilder(string.Empty);
            if (File.Exists(robotsPath))
            {
                StreamReader sr = new StreamReader(robotsPath);
                sitemapContent.Append(sr.ReadToEnd());
                sr.Close();
            }

            StreamWriter sw = new StreamWriter(robotsPath, false);
            
            // If production, add sitemap Urls to robots.txt.  Otherwise, disallow all bots.
            if (SitemapManagerConfiguration.IsProductionEnvironment)
            {
                foreach (string sitemapUrl in m_Sites.Values)
                {
                    string sitemapLine = string.Concat("sitemap: ", sitemapUrl);
                    if (!sitemapContent.ToString().Contains(sitemapLine))
                    {
                        sitemapContent.AppendLine(sitemapLine);
                    }
                }
            }
            else
            {
                sitemapContent = new StringBuilder();
                sitemapContent.AppendLine("User-agent: *");
                sitemapContent.AppendLine("Disallow: /");
            }
            sw.Write(sitemapContent.ToString());
            sw.Close();
        }

        private string BuildSitemapXML(List<Item> items, Site site, SiteContext siteContext)
        {
            string strSiteLanguage = "";
            site.Properties.TryGetValue("language", out strSiteLanguage);

            XmlDocument doc = new XmlDocument();
            string url = "";

            XmlNode declarationNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(declarationNode);
            XmlNode urlsetNode = doc.CreateElement("urlset");
            XmlAttribute xmlnsAttr = doc.CreateAttribute("xmlns");
            xmlnsAttr.Value = SitemapManagerConfiguration.XmlnsTpl;
            urlsetNode.Attributes.Append(xmlnsAttr);

            doc.AppendChild(urlsetNode);

            foreach (Item itm in items)
            {
                // If an item version exists in the site's default language
                Globalization.Language language = itm.Languages.FirstOrDefault(i => i.Name.ToLower().Equals(strSiteLanguage.ToLower()));
                if (language != null && Sitecore.Data.Managers.ItemManager.GetVersions(itm, language).Any())
                {
                    url = HtmlEncode(this.GetItemUrl(itm, site));
                    doc = this.BuildSitemapItem(doc, itm, site, url);
                }
            }

            return doc.OuterXml;
        }

        private XmlDocument BuildSitemapItem(XmlDocument doc, Item item, Site site, string url)
        {
            string lastMod = HtmlEncode(item.Statistics.Updated.ToString("yyyy-MM-ddTHH:mm:sszzz"));

            XmlNode urlsetNode = doc.LastChild;

            XmlNode urlNode = doc.CreateElement("url");
            urlsetNode.AppendChild(urlNode);

            XmlNode locNode = doc.CreateElement("loc");
            urlNode.AppendChild(locNode);
            locNode.AppendChild(doc.CreateTextNode(url));

            XmlNode lastmodNode = doc.CreateElement("lastmod");
            urlNode.AppendChild(lastmodNode);
            lastmodNode.AppendChild(doc.CreateTextNode(lastMod));

            return doc;
        }

        private string GetItemUrl(Item item, Site site)
        {
            Sitecore.Links.UrlOptions options = Sitecore.Links.UrlOptions.DefaultOptions;

            options.SiteResolving = Sitecore.Configuration.Settings.Rendering.SiteResolving;
            options.Site = SiteContext.GetSite(site.Name);
			options.AlwaysIncludeServerUrl = false;

            string serverUrl = SitemapManagerConfiguration.GetServerUrlBySite(site.Name);
            string itemUrl = Sitecore.Links.LinkManager.GetItemUrl(item, options);

            if (itemUrl.StartsWith("http"))
            {
                return itemUrl;
            }
            else
            {
                return string.Format("{0}{1}", serverUrl, itemUrl);
            }
            
        }

        private static string HtmlEncode(string text)
        {
            string result = HttpUtility.HtmlEncode(text);

            return result;
        }

        private void SubmitEngine(string engine, string sitemapUrl)
        {
            //Check if it is not localhost because search engines returns an error
            if (!sitemapUrl.Contains("http://localhost"))
            {
                string request = string.Concat(engine, HtmlEncode(sitemapUrl));

                System.Net.HttpWebRequest httpRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(request);
                try
                {
                    System.Net.WebResponse webResponse = httpRequest.GetResponse();

                    System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)webResponse;
                    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        Log.Error(string.Format("Cannot submit sitemap to \"{0}\"", engine), this);
                    }
                }
                catch
                {
                    Log.Warn(string.Format("The serachengine \"{0}\" returns an 404 error", request), this);
                }
            }
        }


        private List<Item> GetSitemapItems(string rootPath)
        {
            string disTpls = SitemapManagerConfiguration.EnabledTemplates;
            string exclNames = SitemapManagerConfiguration.ExcludeItems;


            Item contentRoot = Db.Items[rootPath];
            if (contentRoot == null)
            {
                return new List<Item>();
            }

            Item[] descendants;
            Sitecore.Security.Accounts.User user = Sitecore.Security.Accounts.User.FromName(@"extranet\Anonymous", true);
            using (new Sitecore.Security.Accounts.UserSwitcher(user))
            {
                descendants = contentRoot.Axes.GetDescendants();
            }
            List<Item> sitemapItems = descendants.ToList();
            sitemapItems.Insert(0, contentRoot);

            List<string> enabledTemplates = this.BuildListFromString(disTpls, '|');
            List<string> excludedNames = this.BuildListFromString(exclNames, '|');
            List<Item> selected;

            if (enabledTemplates.Any())
            {
                selected = sitemapItems.Where(itm => itm.Template != null && enabledTemplates.Contains(itm.Template.ID.ToString()) && !excludedNames.Contains(itm.ID.ToString())).ToList();
            }
            else
            {
                selected = sitemapItems.Where(itm => HasLayout(itm, DefaultDevice) && !excludedNames.Contains(itm.ID.ToString())).ToList();
            }
            
            return selected;
        }

        private static bool HasLayout(Item item, DeviceItem device)
        {
            bool hasLayout = false;

            if (item != null && item.Visualization != null)
            {
                ID layoutId = item.Visualization.GetLayoutID(device);
                hasLayout = Guid.Empty != layoutId.Guid;
            }

            return hasLayout;
        }

        private List<string> BuildListFromString(string str, char separator)
        {
            string[] enabledTemplates = str.Split(separator);
            var selected = from dtp in enabledTemplates
                           where !string.IsNullOrEmpty(dtp)
                           select dtp;

            List<string> result = selected.ToList();

            return result;
        }

    }
}
