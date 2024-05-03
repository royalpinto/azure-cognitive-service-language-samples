using LocalizationCultureCore.StringLocalizer;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading;

namespace CoreBotCLU.Utils
{
	public class LocaleUtil
	{
        // I know this is a really bad, but it's just a POC and I don't care
        public static IStringLocalizer getLocalizer(string locale, ILogger logger)
        {
            if (locale != null)
            {
                // CultureInfo.CurrentCulture = new CultureInfo(locale, false);
                //CultureInfo.CurrentUICulture = new CultureInfo(locale, false);
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
            }
            return new JsonStringLocalizer("Resources", "Application Name", logger);
        }
    }
}
