using RSS_II_RGB.App;
using RSS_II_RGB.Core.Localization;

namespace ROG_KB_RGB_Studio.App.Tests;

public class BrandIdentityTests
{
    [Fact]
    public void ProductTitleIsConsistentAcrossLanguages()
    {
        var previous = L.Language;
        try
        {
            foreach (var language in Enum.GetValues<AppLanguage>())
            {
                L.Language = language;
                Assert.Equal("ROG KB RGB Studio", AppConstants.AppTitle);
            }
        }
        finally { L.Language = previous; }
    }
}
