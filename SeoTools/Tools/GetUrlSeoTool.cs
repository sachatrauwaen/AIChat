using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

using AngleSharp;
using AngleSharp.Dom;

using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Services;
using Newtonsoft.Json;
using Dnn.Mcp.WebApi;

namespace Satrabel.SeoTools.Tools
{
    public class GetUrlSeoTool : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterTool(new ToolDefinition
            {
                Name = "get-url-seo",
                Title = "Get SEO Information of an URL",
                Description = "Retrieves and parses the HTML of a URL and extracts SEO-relevant information including meta tags, headings, Open Graph, Twitter Card, structured data, links, and more",
                Category = "Web",
                ReadOnly = true,
                Parameters = new List<ToolParameter>
                {
                    new ToolParameter
                    {
                        Name = "url",
                        Description = "The URL to retrieve and analyse for SEO",
                        Required = true,
                        Type = "string"
                    }
                },
                Handler = (arguments) =>
                {
                    if (!arguments.ContainsKey("url"))
                        throw new ArgumentException("URL is required");
                    var url = arguments["url"]?.ToString();
                    if (string.IsNullOrWhiteSpace(url))
                        throw new ArgumentException("URL cannot be empty");
                    var result = ExtractSeoInfo(url);

                    return new CallToolResult
                    {
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock
                            {
                                Text = result
                            }
                        }
                    };
                }
            });
        }

        public string ExtractSeoInfo(string url)
        {
            try
            {
                string html;
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; SEO-Bot/1.0)");
                    html = client.GetStringAsync(url).Result;
                }

                var config = Configuration.Default;
                var context = BrowsingContext.New(config);
                var document = context.OpenAsync(req => req.Content(html)).Result;

                var seo = new SeoData();

                // Basic metadata
                seo.Url = url;
                seo.Title = document.Title?.Trim();
                seo.MetaDescription = GetMetaContent(document, "description");
                seo.MetaKeywords = GetMetaContent(document, "keywords");
                seo.MetaRobots = GetMetaContent(document, "robots");
                seo.Language = document.DocumentElement?.GetAttribute("lang");
                seo.Charset = document.CharacterSet;

                // Canonical & alternate
                seo.Canonical = document.QuerySelector("link[rel='canonical']")?.GetAttribute("href");
                seo.HreflangTags = document.QuerySelectorAll("link[rel='alternate'][hreflang]")
                    .Select(el => new HreflangEntry
                    {
                        Lang = el.GetAttribute("hreflang"),
                        Href = el.GetAttribute("href")
                    }).ToList();

                // Viewport
                seo.Viewport = GetMetaContent(document, "viewport");

                // Open Graph
                seo.OpenGraph = new Dictionary<string, string>();
                foreach (var el in document.QuerySelectorAll("meta[property^='og:']"))
                {
                    var prop = el.GetAttribute("property")?.Replace("og:", "");
                    var content = el.GetAttribute("content");
                    if (!string.IsNullOrEmpty(prop))
                        seo.OpenGraph[prop] = content;
                }

                // Twitter Card
                seo.TwitterCard = new Dictionary<string, string>();
                foreach (var el in document.QuerySelectorAll("meta[name^='twitter:']"))
                {
                    var name = el.GetAttribute("name")?.Replace("twitter:", "");
                    var content = el.GetAttribute("content");
                    if (!string.IsNullOrEmpty(name))
                        seo.TwitterCard[name] = content;
                }

                // Headings
                seo.H1 = document.QuerySelectorAll("h1").Select(h => h.TextContent.Trim()).Where(t => t.Length > 0).ToList();
                seo.H2 = document.QuerySelectorAll("h2").Select(h => h.TextContent.Trim()).Where(t => t.Length > 0).ToList();
                seo.H3 = document.QuerySelectorAll("h3").Select(h => h.TextContent.Trim()).Where(t => t.Length > 0).ToList();

                // Images
                var images = document.QuerySelectorAll("img").ToList();
                seo.ImageCount = images.Count;
                seo.ImagesWithoutAlt = images.Count(img => string.IsNullOrWhiteSpace(img.GetAttribute("alt")));
                seo.ImagesWithEmptyAlt = images.Count(img => img.GetAttribute("alt") != null && img.GetAttribute("alt").Trim() == "");

                // Links
                var links = document.QuerySelectorAll("a[href]").ToList();
                var baseUri = new Uri(url);
                var internalLinks = new List<string>();
                var externalLinks = new List<string>();
                foreach (var link in links)
                {
                    var href = link.GetAttribute("href");
                    if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#") || href.StartsWith("javascript:"))
                        continue;
                    try
                    {
                        var absolute = new Uri(baseUri, href);
                        if (absolute.Host == baseUri.Host)
                            internalLinks.Add(absolute.ToString());
                        else
                            externalLinks.Add(absolute.ToString());
                    }
                    catch { }
                }
                seo.InternalLinkCount = internalLinks.Count;
                seo.ExternalLinkCount = externalLinks.Count;
                seo.ExternalLinks = externalLinks.Distinct().Take(20).ToList();

                // Structured Data (JSON-LD)
                seo.StructuredData = document.QuerySelectorAll("script[type='application/ld+json']")
                    .Select(el => el.TextContent.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                // Page speed hints
                seo.InlineStyleCount = document.QuerySelectorAll("[style]").Length;
                seo.ExternalScriptCount = document.QuerySelectorAll("script[src]").Length;
                seo.ExternalStylesheetCount = document.QuerySelectorAll("link[rel='stylesheet']").Length;

                return JsonConvert.SerializeObject(seo, Formatting.None);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        private static string GetMetaContent(IDocument document, string name)
        {
            return document.QuerySelector($"meta[name='{name}']")?.GetAttribute("content")
                ?? document.QuerySelector($"meta[name='{name.ToLowerInvariant()}']")?.GetAttribute("content");
        }

        private class SeoData
        {
            public string? Url { get; set; }
            public string? Title { get; set; }
            public string? MetaDescription { get; set; }
            public string? MetaKeywords { get; set; }
            public string? MetaRobots { get; set; }
            public string? Language { get; set; }
            public string? Charset { get; set; }
            public string? Canonical { get; set; }
            public string? Viewport { get; set; }
            public List<HreflangEntry> HreflangTags { get; set; } = new List<HreflangEntry>();
            public Dictionary<string, string> OpenGraph { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, string> TwitterCard { get; set; } = new Dictionary<string, string>();
            public List<string> H1 { get; set; } = new List<string>();
            public List<string> H2 { get; set; } = new List<string>();
            public List<string> H3 { get; set; } = new List<string>();
            public int ImageCount { get; set; }
            public int ImagesWithoutAlt { get; set; }
            public int ImagesWithEmptyAlt { get; set; }
            public int InternalLinkCount { get; set; }
            public int ExternalLinkCount { get; set; }
            public List<string> ExternalLinks { get; set; } = new List<string>();
            public List<string> StructuredData { get; set; } = new List<string>();
            public int InlineStyleCount { get; set; }
            public int ExternalScriptCount { get; set; }
            public int ExternalStylesheetCount { get; set; }
        }

        private class HreflangEntry
        {
            public string? Lang { get; set; }
            public string? Href { get; set; }
        }
    }
}
