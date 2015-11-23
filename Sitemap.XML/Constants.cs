using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitemap.XML
{
    public struct Constants
    {
        public struct SharedContent
        {
            public static string ParentItemFieldName = "Parent Item";
            public static string ContentLocationFieldName = "Content Location";
            public static string FolderName = "Shared Content";
        }

        public struct WebsiteDefinition
        {
            public static string SearchEnginesFieldName = "Search Engines";
            public static string EnabledTemplatesFieldName = "Enabled Templates";
            public static string ExcludedItemsFieldName = "Excluded Items";
            public static string FileNameFieldName = "FileName";
            public static string ServerUrlFieldName = "Server Url";
        }

        public struct SeoSettings
        {
            public static string Title = "Sitemap Title";
            public static string Priority = "Priority";
            public static string ChangeFrequency = "Change Frequency";
        }

        public static string SitemapParserUser = @"extranet\Anonymous";
        public static string SitemapModuleSettingsRootItemId = "{6003D67E-0000-4A4D-BFB1-11408B9ADCFD}";
        public static string RobotsFileName = "robots.txt";
        public static string SitemapSubmissionUriFieldName = "Sitemap Submission Uri";
    }
}