namespace InPublishing
{
    using Android.Content;

    using ExpansionDownloader.Service;

    [BroadcastReceiver(Exported = false)]
    public class AlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            DownloaderService.StartDownloadServiceIfRequired(context, intent, typeof(MyDownloaderService));
        }
    }
}