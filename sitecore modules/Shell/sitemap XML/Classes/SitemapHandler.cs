/* *********************************************************************** *
 * File   : SitemapHandler.cs                             Part of Sitecore *
 * Version: 1.0.0                                         www.sitecore.net *
 *                                                                         *
 *                                                                         *
 * Purpose: Contains logic which fires when event submitted                *
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

using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using System.Collections;

namespace Sitecore.Modules.SitemapXML
{
    public class SitemapHandler
    {
        public void RefreshSitemap(object sender, EventArgs args)
        {
            var sites = SitemapManagerConfiguration.GetSites();
            foreach (DictionaryEntry site in sites)
            {
                var config = new SitemapManagerConfiguration(site.Key.ToString(), site.Value.ToString());
                var sitemapManager = new SitemapManager(config);
                sitemapManager.SubmitSitemapToSearchenginesByHttp();
                sitemapManager.RegisterSitemapToRobotsFile();
            }
        }
    }
}
