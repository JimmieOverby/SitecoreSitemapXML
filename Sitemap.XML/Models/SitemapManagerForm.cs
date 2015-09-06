/* *********************************************************************** *
 * File   : SitemapManagerForm.cs                         Part of Sitecore *
 * Version: 1.0.0                                         www.sitecore.net *
 *                                                                         *
 *                                                                         *
 * Purpose: Codebehind of ManagerForm                                      *
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
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Diagnostics;
using System.Collections.Specialized;
using System.Text;
using System.Linq;

namespace Sitemap.XML.Models
{
    public class SitemapManagerForm : Sitecore.Web.UI.Sheer.BaseForm
    {
        protected Button RefreshButton;
        protected Literal Message;

        protected override void OnLoad(EventArgs args)
        {
            base.OnLoad(args);
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                RefreshButton.Click = "RefreshButtonClick";
            }
        }

        protected void RefreshButtonClick()
        {
            var sh = new SitemapHandler();
            sh.RefreshSitemap(this, new EventArgs());

            StringBuilder sb = new StringBuilder();
            var siteNames = SitemapManagerConfiguration.GetSiteNames();
            var message = string.Empty;
            if (siteNames == null || !siteNames.Any())
            {
                Message.Text = "No sitemap configurations found under /sitecore/system/Modules/Sitemap XML. Please create one or more configuration nodes and try refreshing again.";
                RefreshPanel("MainPanel");
                return;
            }
            foreach (var siteName in siteNames)
            {
                var config = new SitemapManagerConfiguration(siteName);
                if (string.IsNullOrWhiteSpace(config.FileName)) continue;
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(config.FileName);
            }

            message = !string.IsNullOrWhiteSpace(sb.ToString()) 
                ? string.Format(" - The sitemap file <b>\"{0}\"</b> has been refreshed<br /> - <b>\"{0}\"</b> has been registered to \"robots.txt\"", sb.ToString()) 
                :"File name has not been specified for one or more sitemap configurations under /sitecore/system/Modules/Sitemap XML.";

            Message.Text = message;

            RefreshPanel("MainPanel");
        }

        private static void RefreshPanel(string panelName)
        {
            Sitecore.Web.UI.HtmlControls.Panel ctl = Sitecore.Context.ClientPage.FindControl(panelName) as
                Sitecore.Web.UI.HtmlControls.Panel;
            Assert.IsNotNull(ctl, "can't find panel");

            Sitecore.Context.ClientPage.ClientResponse.Refresh(ctl);
        }
    }
}
