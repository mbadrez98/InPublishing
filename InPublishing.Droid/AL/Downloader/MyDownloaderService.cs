namespace InPublishing
{
    using Android.App;

    using ExpansionDownloader.Service;

    [Service]
    public class MyDownloaderService : DownloaderService
    {
        protected override string PublicKey
        {
            get
            {
				return DataManager.Get<ISettingsManager>().Settings.PublicKey;
            }
        }

        protected override byte[] Salt
        {
            get
            {
                return new byte[] { 1, 43, 12, 1, 54, 98, 100, 12, 43, 2, 8, 4, 9, 5, 106, 108, 33, 45, 1, 84 };
            }
        }

        protected override string AlarmReceiverClassName
        {
            get
            {
				return "InPublishing.AlarmReceiver";
            }
        }
    }
}
