using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Links;
using Sitecore.Pipelines.HttpRequest;
using Sitemap.XML.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Xml;

namespace Sitemap.XML.Configuration
{
    public class SitemapHandler : HttpRequestProcessor
    {
        public string excludedPaths { get; set; }
        public string cacheTime { get; set; }

        public override void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            if (Context.Site == null || string.IsNullOrEmpty(Context.Site.RootPath.Trim())) return;
            if (Context.Page.FilePath.Length > 0) return;

            if (!args.Url.FilePath.Contains("sitemap.xml")) return;

            // Important to return qualified XML (text/xml) for sitemaps
            args.Context.Response.ClearHeaders();
            args.Context.Response.ClearContent();
            args.Context.Response.ContentType = "text/xml";

            // Checking the cache first
            var sitemapXmlCache = args.Context.Cache["sitemapxml"];
            if (sitemapXmlCache != null)
            {
                args.Context.Response.Write(sitemapXmlCache.ToString());
                args.Context.Response.End();
                return;
            }

            var content = string.Empty;

            try
            {
                var site = Context.Site;
                var config = new SitemapManagerConfiguration(site.Name);
                var sitemapManager = new SitemapManager(config);

                content = sitemapManager.BuildSiteMapForHandler();
                args.Context.Response.Write(content);
            }
            finally
            {
                args.Context.Cache.Add("sitemapxml", content, null,
                              DateTime.Now.AddSeconds(int.Parse(cacheTime)),
                              Cache.NoSlidingExpiration,
                              CacheItemPriority.Normal,
                              null);
                args.Context.Response.Flush();
                args.Context.Response.End();
            }
        }


        private bool IsPage(Item item)
        {
            var result = false;
            var layoutField = new LayoutField(item.Fields[FieldIDs.LayoutField]);
            if (!layoutField.InnerField.HasValue || string.IsNullOrEmpty(layoutField.Value)) return false;
            var layout = LayoutDefinition.Parse(layoutField.Value);
            foreach (var deviceObj in layout.Devices)
            {
                var device = deviceObj as DeviceDefinition;
                if (device == null) return false;
                if (device.Renderings.Count > 0)
                {
                    result = true;
                }
            }
            return result;
        }
    }
}