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

using System.Collections.Generic;
using System.IO;
using System.Web.SessionState;
using System.Xml;
using Sitecore.ContentSearch.Sharding;
using Sitecore.Data.Items;
using Sitecore.Sites;
using Sitecore.Data;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using System.Web;
using System.Text;
using System.Linq;
using System.Collections.Specialized;
using System.Collections;
using Sitecore.Data.Fields;
using Sitecore.Buckets.Managers;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore;

namespace Sitemap.XML.Models
{
    public class SitemapManager
    {
        //private static string sitemapUrl;
        private readonly SitemapManagerConfiguration _config;

        public Database Db
        {
            get
            {
                Database database = Factory.GetDatabase(SitemapManagerConfiguration.WorkingDatabase);
                return database;
            }
        }

        public SitemapManager(SitemapManagerConfiguration config)
        {
            Assert.IsNotNull(config, "config");
            _config = config;
            if (!string.IsNullOrWhiteSpace(_config.FileName))
            {
                BuildSiteMap();
            }
        }


        private void BuildSiteMap()
        {
            Site site = Sitecore.Sites.SiteManager.GetSite(_config.SiteName);
            SiteContext siteContext = Factory.GetSite(_config.SiteName);
            string rootPath = siteContext.StartPath;

            List<SitemapItem> items = GetSitemapItems(rootPath);

            string fullPath = MainUtil.MapPath(string.Concat("/", _config.FileName));
            string xmlContent = this.BuildSitemapXML(items, site);

            StreamWriter strWriter = new StreamWriter(fullPath, false);
            strWriter.Write(xmlContent);
            strWriter.Close();

        }


        public string BuildSiteMapForHandler()
        {
            var site = Sitecore.Sites.SiteManager.GetSite(Sitecore.Context.Site.Name);
            var siteContext = Factory.GetSite(Sitecore.Context.Site.Name);
            string rootPath = siteContext.StartPath;

            var items = GetSitemapItems(rootPath);

            string xmlContent = this.BuildSitemapXML(items, site);
            return xmlContent;
        }


        public bool SubmitSitemapToSearchenginesByHttp()
        {
            if (!SitemapManagerConfiguration.IsProductionEnvironment)
                return false;

            bool result = false;
            Item sitemapConfig = Db.Items[_config.SitemapConfigurationItemPath];

            if (sitemapConfig != null)
            {
                string engines = sitemapConfig.Fields["Search engines"].Value;
                foreach (string id in engines.Split('|'))
                {
                    Item engine = Db.Items[id];
                    if (engine != null)
                    {
                        string engineHttpRequestString = engine.Fields["HttpRequestString"].Value;
                        var filePath = !SitemapManagerConfiguration.GetServerUrl(_config.SiteName).EndsWith("/")
                            ? SitemapManagerConfiguration.GetServerUrl(_config.SiteName) + "/"
                            : SitemapManagerConfiguration.GetServerUrl(_config.SiteName) + _config.FileName;
                        SubmitEngine(engineHttpRequestString, filePath);
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
            string sitemapLine = string.Concat("Sitemap: ", _config.FileName);
            if (!sitemapContent.ToString().Contains(sitemapLine))
            {
                sitemapContent.AppendLine(sitemapLine);
            }
            sw.Write(sitemapContent.ToString());
            sw.Close();
        }

        private string BuildSitemapXML(List<SitemapItem> items, Site site)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode declarationNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(declarationNode);
            XmlNode urlsetNode = doc.CreateElement("urlset");
            XmlAttribute xmlnsAttr = doc.CreateAttribute("xmlns");
            xmlnsAttr.Value = SitemapManagerConfiguration.XmlnsTpl;
            urlsetNode.Attributes.Append(xmlnsAttr);

            doc.AppendChild(urlsetNode);


            foreach (var itm in items)
            {
                doc = this.BuildSitemapItem(doc, itm, site);
            }

            return doc.OuterXml;
        }

        private XmlDocument BuildSitemapItem(XmlDocument doc, SitemapItem item, Site site)
        {
            XmlNode urlsetNode = doc.LastChild;

            XmlNode urlNode = doc.CreateElement("url");
            urlsetNode.AppendChild(urlNode);

            XmlNode locNode = doc.CreateElement("loc");
            urlNode.AppendChild(locNode);
            locNode.AppendChild(doc.CreateTextNode(item.Location));

            XmlNode lastmodNode = doc.CreateElement("lastmod");
            urlNode.AppendChild(lastmodNode);
            lastmodNode.AppendChild(doc.CreateTextNode(item.LastModified));

            if (!string.IsNullOrWhiteSpace(item.ChangeFrequency))
            {
                XmlNode changeFrequencyNode = doc.CreateElement("changefreq");
                urlNode.AppendChild(changeFrequencyNode);
                changeFrequencyNode.AppendChild(doc.CreateTextNode(item.ChangeFrequency));
            }

            if (!string.IsNullOrWhiteSpace(item.Priority))
            {
                var priorityNode = doc.CreateElement("priority");
                urlNode.AppendChild(priorityNode);
                priorityNode.AppendChild(doc.CreateTextNode(item.Priority));
            }

            return doc;
        }

        private void SubmitEngine(string engine, string sitemapUrl)
        {
            //Check if it is not localhost because search engines returns an error
            if (!sitemapUrl.Contains("http://localhost"))
            {
                string request = string.Concat(engine, SitemapItem.HtmlEncode(sitemapUrl));

                System.Net.HttpWebRequest httpRequest =
                    (System.Net.HttpWebRequest) System.Net.HttpWebRequest.Create(request);
                try
                {
                    System.Net.WebResponse webResponse = httpRequest.GetResponse();

                    System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse) webResponse;
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


        private List<SitemapItem> GetSitemapItems(string rootPath)
        {
            string disTpls = _config.EnabledTemplates;
            string exclNames = _config.ExcludeItems;


            Database database = Factory.GetDatabase(SitemapManagerConfiguration.WorkingDatabase);

            Item contentRoot = database.Items[rootPath];

            Item[] descendants;
            Sitecore.Security.Accounts.User user = Sitecore.Security.Accounts.User.FromName(@"extranet\Anonymous", true);
            using (new Sitecore.Security.Accounts.UserSwitcher(user))
            {
                descendants = contentRoot.Axes.GetDescendants();
            }

            // getting shared content
            var sharedItems = new List<Item>();
            var sharedModels = new List<SitemapItem>();
            var sharedDefinitions = Db.SelectItems("fast:" + _config.SitemapConfigurationItemPath + "/Shared Content/*");
            var site = Factory.GetSite(_config.SiteName);
            List<string> enabledTemplates = this.BuildListFromString(disTpls, '|');
            List<string> excludedNames = this.BuildListFromString(exclNames, '|');
            foreach (var sharedDefinition in sharedDefinitions)
            {
                if (string.IsNullOrWhiteSpace(sharedDefinition["Content Location"]) ||
                    string.IsNullOrWhiteSpace(sharedDefinition["Parent Item"]))
                    continue;
                var contentLocation = ((DatasourceField) sharedDefinition.Fields["Content Location"]).TargetItem;
                var parentItem = ((DatasourceField) sharedDefinition.Fields["Parent Item"]).TargetItem;

                if (BucketManager.IsBucket(contentLocation))
                {
                    var index = ContentSearchManager.GetIndex(new SitecoreIndexableItem(contentLocation));
                    using (var searchContext = index.CreateSearchContext())
                    {
                        var searchResultItem =
                            searchContext.GetQueryable<SearchResultItem>()
                                .Where(item => item.Paths.Contains(contentLocation.ID))
                                .ToList();
                        sharedItems.AddRange(searchResultItem.Select(i => i.GetItem()));
                    }
                }
                else
                {
                    sharedItems.AddRange(contentLocation.Children);
                }

                var cleanedSharedItems = from itm in sharedItems
                    where itm.Template != null && enabledTemplates.Contains(itm.Template.ID.ToString()) &&
                          !excludedNames.Contains(itm.ID.ToString())
                    select itm;
                var sharedSitemapItems = cleanedSharedItems.Select(i => new SitemapItem(i, site, parentItem));
                sharedModels.AddRange(sharedSitemapItems);
            }

            List<Item> sitemapItems = descendants.ToList();
            sitemapItems.Insert(0, contentRoot);




            var selected = from itm in sitemapItems
                where itm.Template != null && enabledTemplates.Contains(itm.Template.ID.ToString()) &&
                      !excludedNames.Contains(itm.ID.ToString())
                select itm;

            var selectedModels = selected.Select(i => new SitemapItem(i, site, null)).ToList();
            selectedModels.AddRange(sharedModels);
            return selectedModels;
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

        public static bool IsShared(Item item)
        {
            var sharedDefinitions = GetSharedContentDefinitions();

            var sharedItemContentRoots =
                sharedDefinitions.Select(i => ((DatasourceField) i.Fields["Parent Item"]).TargetItem).ToList();
            if (!sharedItemContentRoots.Any()) return false;

            return sharedItemContentRoots.Any(i => i.ID == item.ID);
        }

        public static Item GetSharedParent(Item item)
        {
            var sharedNodes = GetSharedContentDefinitions();

            var sharedAreaDefinitions = sharedNodes.Select(i => ((DatasourceField)i.Fields["Parent Item"]).TargetItem).ToList();
            return !sharedAreaDefinitions.Any() ? null : sharedAreaDefinitions.First(i => i.ID.Guid == item.ID.Guid);
        }


        private static IEnumerable<Item> GetSharedContentDefinitions()
        {
            var siteNode = GetContextSiteDefinitionItem();
            if (siteNode == null || string.IsNullOrWhiteSpace(siteNode.Name)) return null;

            var sharedItemParent = siteNode.Children["Shared Content"];
            if (sharedItemParent == null) return null;

            var sharedDefinitions = sharedItemParent.Children;
            return sharedDefinitions;
        }

        private static Item GetContextSiteDefinitionItem()
        {
            var sitemapModuleItem = Context.Database.GetItem("{6003D67E-0000-4A4D-BFB1-11408B9ADCFD}");
            var contextSite = Context.GetSiteName().ToLower();
            if (!sitemapModuleItem.Children.Any()) return null;
            var siteNode = sitemapModuleItem.Children.FirstOrDefault(i => i.Key == contextSite);
            return siteNode;
        }
    }
}