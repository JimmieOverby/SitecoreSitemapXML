using Sitecore.Data.Items;
using Sitecore.Sites;
using System;
using System.Text;
using System.Web;

namespace Sitemap.XML.Models
{
    public class SitemapItem
    {
        #region Constructor

        public SitemapItem(Item item, SiteContext site, Item parentItem)
        {
            Priority = item[Constants.SeoSettings.Priority];
            ChangeFrequency = item[Constants.SeoSettings.ChangeFrequency];
            LastModified = HtmlEncode(item.Statistics.Updated.ToLocalTime().ToString("yyyy-MM-ddTHH:mm:sszzz"));
            Id = item.ID.Guid;
            Title = item[Constants.SeoSettings.Title];
            var itemUrl = HtmlEncode(GetItemUrl(item, site));
            if (parentItem == null)
            {
                Location = itemUrl;
            }
            else
            {
                Location = GetSharedItemUrl(item, site, parentItem);
            }
        }

        #endregion

        #region Properties

        public string Location { get; set; }
        public string LastModified { get; set; }
        public string ChangeFrequency { get; set; }
        public string Priority { get; set; }
        public Guid Id { get; set; }
        public string Title { get; set; }

        #endregion

        #region Private Methods

        private static string GetSharedItemUrl(Item item, SiteContext site, Item parentItem)
        {
            var itemUrl = HtmlEncode(GetItemUrl(item, site));
            var parentUrl = HtmlEncode(GetItemUrl(parentItem, site));
            parentUrl = parentUrl.EndsWith("/") ? parentUrl : parentUrl + "/";
            var pos = itemUrl.LastIndexOf("/") + 1;
            var itemNamePath = itemUrl.Substring(pos, itemUrl.Length - pos);
            return HtmlEncode(parentUrl + itemNamePath);
        }

        public static string GetSharedItemUrl(Item item, SiteContext site)
        {
            var parentItem = SitemapManager.GetSharedLocationParent(item);
            var itemUrl = HtmlEncode(GetItemUrl(item, site));
            var parentUrl = HtmlEncode(GetItemUrl(parentItem, site));
            parentUrl = parentUrl.EndsWith("/") ? parentUrl : parentUrl + "/";
            var pos = itemUrl.LastIndexOf("/") + 1;
            var itemNamePath = itemUrl.Substring(pos, itemUrl.Length - pos);
            return HtmlEncode(parentUrl + itemNamePath);
        }

        #endregion

        #region Public Methods

        public static string HtmlEncode(string text)
        {
            string result = HttpUtility.HtmlEncode(text);
            return result;
        }

        public static string GetItemUrl(Item item, SiteContext site)
        {
            Sitecore.Links.UrlOptions options = Sitecore.Links.UrlOptions.DefaultOptions;

            options.SiteResolving = Sitecore.Configuration.Settings.Rendering.SiteResolving;
            options.Site = SiteContext.GetSite(site.Name);
            options.AlwaysIncludeServerUrl = false;

            string url = Sitecore.Links.LinkManager.GetItemUrl(item, options);

            var configServerUrl = (new SitemapManagerConfiguration(site.Name)).ServerUrl;
            string serverUrl = SitemapManagerConfiguration.GetServerUrl(site.Name);
            if (!string.IsNullOrWhiteSpace(configServerUrl))
            {
                serverUrl = configServerUrl;
            }
            
            
            if (serverUrl.Contains("http://"))
            {
                serverUrl = serverUrl.Substring("http://".Length);
            }

            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(serverUrl))
            {
                if (url.Contains("://") && !url.Contains("http"))
                {
                    sb.Append("http://");
                    sb.Append(serverUrl);
                    if (url.IndexOf("/", 3) > 0)
                        sb.Append(url.Substring(url.IndexOf("/", 3)));
                }
                else
                {
                    sb.Append("http://");
                    sb.Append(serverUrl);
                    sb.Append(url);
                }
            }
            else if (!string.IsNullOrEmpty(site.Properties["hostname"]))
            {
                sb.Append("http://");
                sb.Append(site.Properties["hostname"]);
                sb.Append(url);
            }
            else
            {
                if (url.Contains("://") && !url.Contains("http"))
                {
                    sb.Append("http://");
                    sb.Append(url);
                }
                else
                {
                    sb.Append(Sitecore.Web.WebUtil.GetFullUrl(url));
                }
            }

            return sb.ToString();

        }

        #endregion
    }
}