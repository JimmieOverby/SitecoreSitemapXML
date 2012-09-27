using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitemap.XML.Links
{
    using Sitecore.Data.Items;
    using Sitecore.Links;

    public class LowerCaseLinkProvider : LinkProvider
    {
        public override string GetItemUrl(Item item, UrlOptions options)
        {
            var url = base.GetItemUrl(item, options);
            if (!string.IsNullOrEmpty(url))
                return url.ToLower();

            return url;
        }
    }
}