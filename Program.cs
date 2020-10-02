using System;
using System.Threading.Tasks;

using Statiq.App;
using Statiq.Common;
using Statiq.Web;

namespace GlennWatson.Website
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-AU");
            return await Bootstrapper
                .Factory
                .CreateWeb(args)
                .DeployToNetlify(
                    Config.FromSetting<string>("NETLIFY_SITE_ID"),
                    Config.FromSetting<string>("NETLIFY_DEPLOY_KEY"))
                .RunAsync();
        }
    }
}
