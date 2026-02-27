using System.Collections.Generic;
using Dnn.Mcp.WebApi.Models;
using Dnn.Mcp.WebApi.Models.Mcp;
using Dnn.Mcp.WebApi.Services;

namespace Dnn.Mcp.WebApi.Prompts
{
    public class PromptProvider : IMcpProvider
    {
        public void Register(IMcpRegistry registry)
        {
            registry.RegisterPrompt(new PromptDefinition
            {
                Name = "seo_checkup",
                Title = "SEO Checkup",
                Description = "Generate a comprehensive SEO analysis prompt for a DNN page",
                Parameters = new List<PromptParameter>
                {
                    new PromptParameter
                    {
                        Name = "tabId",
                        Description = "The ID of the page to analyze for SEO",
                        Required = true
                    },
                    new PromptParameter
                    {
                        Name = "checkImages",
                        Description = "Include image SEO analysis (alt tags, file names) (y/n)",                        
                        Required = false,
                        DefaultValue = "y"
                    },
                    new PromptParameter
                    {
                        Name = "checkLinks",
                        Description = "Include link analysis (internal/external links) (y/n)",
                        Required = false,
                        DefaultValue = "y"
                    }
                },
                Handler = (arguments) =>
                {
                    var tabId = arguments["tabId"].ToString();
                    var checkImages = arguments.ContainsKey("checkImages") ? arguments["checkImages"].ToString().ToLower() : "true";
                    var checkLinks = arguments.ContainsKey("checkLinks") ? arguments["checkLinks"].ToString().ToLower() : "true";

                    var promptText = $@"Please perform a comprehensive SEO checkup for the DNN page with TabID: {tabId}.

Analyze the following SEO elements:

1. **Title Tag Analysis**
   - Check if the title tag exists and is properly formatted
   - Verify title length (optimal: 50-60 characters)
   - Check for keyword presence and placement
   - Ensure uniqueness and descriptiveness

2. **Meta Description**
   - Verify meta description exists
   - Check length (optimal: 150-160 characters)
   - Assess relevance and call-to-action
   - Check for keyword inclusion

3. **Heading Structure (H1-H6)**
   - Verify H1 tag exists and is unique
   - Check heading hierarchy is logical
   - Ensure headings include relevant keywords
   - Validate there's only one H1 per page

4. **Content Analysis**
   - Check content length and quality
   - Assess keyword density and placement
   - Look for duplicate content issues
   - Verify readability and structure

5. **URL Structure**
   - Analyze URL readability and structure
   - Check for keyword presence in URL
   - Verify URL length is reasonable
   - Check for proper formatting (lowercase, hyphens)

6. **Meta Keywords** (if present)
   - Note: Meta keywords are largely deprecated but note if present

7. **Page Performance Indicators**
   - Note any obvious performance issues in HTML structure
   - Check for excessive inline styles or scripts";

                    if (checkImages == "y")
                    {
                        promptText += @"

8. **Image SEO**
   - Check all images have alt attributes
   - Verify alt text is descriptive and relevant
   - Check image file names are descriptive
   - Look for missing or empty alt tags";
                    }

                    if (checkLinks == "y")
                    {
                        promptText += @"

9. **Link Analysis**
   - Count internal and external links
   - Check for broken links (if detectable)
   - Verify proper use of rel attributes (nofollow, noopener, etc.)
   - Check anchor text quality and relevance";
                    }

                    promptText += @"

10. **Additional SEO Elements**
    - Check for Open Graph tags (social media)
    - Look for Twitter Card tags
    - Verify canonical tag if present
    - Check for schema.org structured data

Please provide:
- A summary score or grade for overall SEO health
- Specific issues found with priority levels (Critical, High, Medium, Low)
- Actionable recommendations for improvement
- Best practice suggestions

Use the get-page tool to retrieve the page details and get-url-html to fetch the HTML content for analysis.";

                    var result = new PromptGetResult
                    {
                        Description = $"SEO checkup analysis for page TabID: {tabId}",
                        Messages = new[]
                        {
                            new PromptMessage
                            {
                                Role = "user",
                                Content = new PromptContent
                                {
                                    Type = "text",
                                    Text = promptText
                                }
                            }
                        }
                    };
                    return result;
                }
            });
        }
    }
}
