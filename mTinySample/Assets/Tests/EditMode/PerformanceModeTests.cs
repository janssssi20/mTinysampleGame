using NUnit.Framework;

public class PerformanceModeTests
{
    [Test]
    public void PhoneBrowser_IsAlwaysLowEnd()
    {
        Assert.IsTrue(PerformanceMode.Decide(PerformanceMode.Kind.MobileWeb, 8000, 8, 0));
    }

    [Test]
    public void Phone_LowMemoryOrFewCores_IsLowEnd()
    {
        Assert.IsTrue(PerformanceMode.Decide(PerformanceMode.Kind.Mobile, 2000, 8, 0), "2 GB phone");
        Assert.IsTrue(PerformanceMode.Decide(PerformanceMode.Kind.Mobile, 4000, 4, 0), "quad-core tablet");
        Assert.IsFalse(PerformanceMode.Decide(PerformanceMode.Kind.Mobile, 6000, 8, 0), "a good phone");
    }

    [Test]
    public void Computer_WeakParts_AreLowEnd()
    {
        Assert.IsTrue(PerformanceMode.Decide(PerformanceMode.Kind.Desktop, 2048, 4, 2048), "2 GB laptop");
        Assert.IsTrue(PerformanceMode.Decide(PerformanceMode.Kind.Desktop, 8192, 2, 2048), "dual-core");
        Assert.IsTrue(PerformanceMode.Decide(PerformanceMode.Kind.Desktop, 8192, 4, 256), "tiny graphics memory");
        Assert.IsFalse(PerformanceMode.Decide(PerformanceMode.Kind.Desktop, 8192, 8, 2048), "a normal PC");
    }

    [Test]
    public void DesktopBrowser_OnlyVeryFewCores_IsLowEnd()
    {
        Assert.IsFalse(PerformanceMode.Decide(PerformanceMode.Kind.Web, 0, 8, 0));
        Assert.IsTrue(PerformanceMode.Decide(PerformanceMode.Kind.Web, 0, 2, 0));
    }

    [Test]
    public void UnknownSizes_DoNotCountAsLowEnd()
    {
        Assert.IsFalse(PerformanceMode.Decide(PerformanceMode.Kind.Desktop, 0, 0, 0));
        Assert.IsFalse(PerformanceMode.Decide(PerformanceMode.Kind.Mobile, 0, 0, 0));
    }
}
