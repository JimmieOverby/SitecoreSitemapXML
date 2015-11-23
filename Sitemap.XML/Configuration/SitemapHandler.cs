using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.HttpRequest;
using Sitemap.XML.Models;
using System;
using System.Web.Caching;

namespace Sitemap.XML.Configuration
{
    public class SitemapHandler : HttpRequestProcessor
    {
        #region Properties

        public string ExcludedPaths { get; set; }
        public string CacheTime { get; set; }

        #endregion

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
                              DateTime.Now.AddSeconds(int.Parse(CacheTime)),
                              Cache.NoSlidingExpiration,
                              CacheItemPriority.Normal,
                              null);
                args.Context.Response.Flush();
                args.Context.Response.End();
            }
        }
    }
}