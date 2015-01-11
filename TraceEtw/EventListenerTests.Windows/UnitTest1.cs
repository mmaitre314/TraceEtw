using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Diagnostics;
using Windows.Storage;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace EventListenerTests.Windows
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task CS_Windows_TestBasic()
        {
            StorageFile file;

            using (var listener = new InProcEventListener(
                ApplicationData.Current.LocalFolder, 
                "CS_Windows_TestBasic.etl", 
                new Guid[] { Log.Events.Guid }
                ))
            {
                file = await listener.GetLogFileAsync();

                Logger.LogMessage("Log file: {0}", file.Path);

                Assert.IsTrue(Log.Events.IsEnabled());

                Log.Events.ProcessImageStart();
                Log.Events.Message("A message");
                Log.Events.ProcessImageStop();
            }

            var props = await file.GetBasicPropertiesAsync();
            Assert.IsTrue(props.Size > 0);

            var folder = await KnownFolders.SavedPictures.CreateFolderAsync("Tests", CreationCollisionOption.OpenIfExists);
            await file.MoveAsync(folder, "CS_Windows_TestBasic.etl", NameCollisionOption.ReplaceExisting);

            Logger.LogMessage("Log file moved to: {0}", file.Path);
        }
    }
}
