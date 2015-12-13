SitecoreSitemapXML
==================

The Sitemap XML module generates the Sitemap compliant with the schema defined by sitemaps.org and submits it to search engines.

HOW TO USE THE MODULE
---------------------

1. Install the package.  
2. Perform a site publish.  
3. Open the SitemapXML.config under the App_Config\Include folder. In the SitemapXML.config file you can specify the following:  
   a. The filenames of each sitemap XML file and the server URLs.  
   b. The Database that will be used for the sitemap generation.  
   c. Where the sitemap configuration item will be stored.  
   d. Whether or not to generate robots.txt file.  
   e. The filename of the sitemap index file.  
```xml
   <sitemapVariables>
      <sitemapVariable name="xmlnsTpl" value="http://www.sitemaps.org/schemas/sitemap/0.9" />
      <sitemapVariable name="database" value="web" />
      <sitemapVariable name="sitemapConfigurationItemPath" value="/sitecore/system/Modules/Sitemap XML/Sitemap configuration" />
      <sitemapVariable name="productionEnvironment" value="false" />
      <sitemapVariable name="generateRobotsTxt" value="true" />
      <sitemapVariable name="sitemapIndexFilename" value="sitemap.xml" />
      <sites>
      <!-- 
      serverUrl: (optional) will be used to generate url in sitemap file. 
      If serverUrl left blank, the hostname value set in web.config file for each site will be used.
      Example: serverUrl="www.
      name: this is the sitename which is defined in <site> element in sitecore web.config file.
      filename: the xml sitemap file name. This file name will be inserted into robots.txt file.
      -->
        <site name="website_1" filename="sitemap1.xml" serverUrl="https://www.site1domain.com" />
        <site name="website_2" filename="sitemap2.xml" serverUrl="https://www.site2domain.com"/>
      </sites>
   </sitemapVariables>
```
4. Open the Sitecore backend.  
5. Open the Content Editor and navigate to /sitecore/content/System/modules/Sitemap manager/Sitemap configuration item. This item provides the ability to set which search engines will be used. The module contains three predefined search engines (Google, Live search, Yahoo). You can add your own search engine by adding the new “Sitemap search engine” item and specifying “HttpRequestString”.  
6. In the Configuration section of the Sitemap configuration item you can set which templates that you want the sitemap to contain and which ones that shouldn't be indexed.  
7. Sitemap will be generated and submitted after publishing. Also you can submit sitemap manual. Run the Sitemap Manager application (sitecore menu/all programs/sitemap manager) and click “Refresh sitemap” button.
