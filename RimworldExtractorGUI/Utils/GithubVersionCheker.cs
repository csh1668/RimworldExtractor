using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RimworldExtractorGUI
{
    internal static class GithubVersionCheker
    {
        internal static readonly string ReleasesUrl = "https://github.com/csh1668/RimworldExtractor/releases";
        internal static readonly string LatestUrl = Path.Combine(ReleasesUrl, "latest");

        public static string GetLatest()
        {
            using var hc = new HttpClient(new HttpClientHandler(){AllowAutoRedirect = false});
            var response = hc.GetAsync(LatestUrl).Result;

            if (response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently)
            {
                var redirectedUrl = response.Headers.Location;
                return redirectedUrl?.AbsolutePath.Split('/').Last() ?? throw new WebException("RedirectedUrl was null.");
            }

            throw new WebException("HttpClient got no response.");
        }
    }
}
