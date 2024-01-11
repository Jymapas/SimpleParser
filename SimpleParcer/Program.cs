﻿using HtmlAgilityPack;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleParser
{
    internal class Program
    {
        const string url = "https://chgk-spb.livejournal.com/2596838.html";
        static async Task Main(string[] args)
        {
            var html = await new HttpClient().GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var postContent = doc.DocumentNode.SelectSingleNode("//article[contains(@class, 'b-singlepost-body')]");
            if (postContent != null)
            {
                foreach (var link in postContent.SelectNodes(".//a[@href]"))
                {
                    var href = link.GetAttributeValue("href", string.Empty);
                    if (href.StartsWith("https://www.livejournal.com/away?to="))
                    {
                        href = href.Replace("https://www.livejournal.com/away?to=", "");
                        href = HttpUtility.UrlDecode(href); // Normalize encoded characters
                        link.SetAttributeValue("href", href);
                    }
                }

                Console.WriteLine(postContent.InnerHtml);
            }
            else
            {
                Console.WriteLine("Post content not found.");
            }
        }

    }
}