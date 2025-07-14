﻿using System.Text.RegularExpressions;

namespace DotNet.DocsTools.Utility;

public static partial class ContentScrubber
{
    private const string ImgReplacement = "<i>Image link removed to protect against security vulnerability.</i>";
    private const string UserAssetReplacement = "<i>User asset removed to protect against security vulnerability.</i>";
    
    /// <summary>
    /// Remove false security vulnerabilities from the content.
    /// </summary>
    /// <param name="content">The source HTML content</param>
    /// <returns>The content with any tokens removed.</returns>
    /// <remarks>
    /// Links to images on GitHub contain a JWT token that
    /// creates a false positive in our credential scan.
    /// The token is a secret that expires. As a result, it won't
    /// work as a link, but also puts triggers a security warning.
    /// We need to scrub these links from the content.
    /// Replace the link with a placeholder that says
    /// "Image link removed to protect against security vulnerability."
    /// </remarks>
    public static string ScrubContent(this string content)
    {
        var postImageReplacementContent = ImageAnchorRegEx().Replace(content, ImgReplacement);
        if (content != postImageReplacementContent)
        {
            Console.WriteLine("Replaced linked image in content.");
        }
        
        var postUserAttachementReplacementContent = AttachmentAnchorRegEx().Replace(postImageReplacementContent, UserAssetReplacement);
        if (postImageReplacementContent != postUserAttachementReplacementContent)
        {
            Console.WriteLine("Replaced linked user asset in content.");
        }
        
        return postUserAttachementReplacementContent;
    }

    [GeneratedRegex("""<a.+href="https:\/\/private-user-images\.githubusercontent\.com\/.+".+><\/a>""")]
    private static partial Regex ImageAnchorRegEx();

    [GeneratedRegex("""<a.+href="https:\/\/github\.com\/user-attachments\/assets\/.+".+><\/a>""")]
    private static partial Regex AttachmentAnchorRegEx();
}
