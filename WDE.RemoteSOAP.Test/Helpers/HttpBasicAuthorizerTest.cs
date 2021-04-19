using NUnit.Framework;
using WDE.RemoteSOAP.Helpers;

namespace WDE.RemoteSOAP.Test.Helpers
{
    public class HttpBasicAuthorizerTest
    {
        [Test]
        public void Test()
        {
            var auth = HttpBasicAuthorizer.Encode("user", "pass");
            Assert.AreEqual("dXNlcjpwYXNz", auth.Parameter);
            Assert.AreEqual("Basic", auth.Scheme);
        }
    }
}