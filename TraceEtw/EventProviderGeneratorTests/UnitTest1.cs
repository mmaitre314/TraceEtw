using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EventProviderGenerator;

namespace EventProviderGeneratorTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CS_Desktop_TestGetSafeString()
        {
            var obj = new PrivateType(typeof(GenerateEventProvider));
            var safeString = obj.InvokeStatic("GetSafeString", "Company-Product-Component");

            Assert.AreEqual("Company_Product_Component", safeString);
        }

        [TestMethod]
        public void CS_Desktop_TestGetGuidFromName()
        {
            var obj = new PrivateType(typeof(GenerateEventProvider));
            var providerGuid = obj.InvokeStatic("GetGuidFromName", "Company-Product-Component");

            Console.WriteLine("Provider Guid: {0}", ((Guid)providerGuid).ToString("B"));

            Assert.AreEqual(Logger.Events.Guid, providerGuid);
        }
    }
}
