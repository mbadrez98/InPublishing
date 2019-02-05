using System;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Support.V4.App;
using Android.Media;

namespace InPublishing
{
	[Service]
	public class GcmIntentService : IntentService
	{
		static PowerManager.WakeLock sWakeLock;
		static object LOCK = new object();
		public static int NOTIFICATION_ID = 1;
		private NotificationManager mNotificationManager;
		//NotificationCompat.Builder builder;

		public static void RunIntentInService(Context context, Intent intent)
		{
			lock (LOCK)
			{
				if (sWakeLock == null)
				{
					// This is called from BroadcastReceiver, there is no init.
					var pm = PowerManager.FromContext(context);
					sWakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "My WakeLock Tag");
				}
			}

			sWakeLock.Acquire();
			intent.SetClass(context, typeof(GcmIntentService));
			context.StartService(intent);

			//base.OnStartCommand§(
		}

		/*public void OnCreate()
		{
			base.OnCreate();
		}

		public StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
		{
			return base.OnStartCommand(intent, flags, startId);
		}*/

		protected override void OnHandleIntent(Intent intent)
		{
			try
			{
				Context context = this.ApplicationContext;
				string action = intent.Action;

				if (action.Equals("com.google.android.c2dm.intent.REGISTRATION"))
				{
					//HandleRegistration(context, intent);
				}
				else if (action.Equals("com.google.android.c2dm.intent.RECEIVE"))
				{
					//HandleMessage(context, intent);
					SendNotification(intent.Extras);
				}
			}
			finally
			{
				lock (LOCK)
				{
					//Sanity check for null as this is a public method
					if (sWakeLock != null)
						sWakeLock.Release();
				}
			}
		}

		private void SendNotification(Bundle extras) 
		{
			string msg = extras.GetString("message");
			string sound = extras.GetString("sound");

			mNotificationManager = (NotificationManager)this.GetSystemService(Context.NotificationService);

			PendingIntent contentIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(SplashScreen)), 0);

            NotificationCompat.Builder mBuilder =
				new NotificationCompat.Builder(this)					
					.SetAutoCancel(true)
					.SetContentTitle(ApplicationInfo.LoadLabel(PackageManager))
					.SetContentIntent(contentIntent)
					.SetStyle(new NotificationCompat.BigTextStyle()
						.BigText(msg))
					.SetContentText(msg);

            if ((int)Android.OS.Build.VERSION.SdkInt >= 21)
            {
                if (Utility.IsMediabookApp)
                {
                    mBuilder.SetSmallIcon(Resource.Drawable.ic_notification);
                }
                else
                {
                    mBuilder.SetSmallIcon(Resource.Drawable.ic_notification_gen);
                }
            }
            else
            {
                mBuilder.SetSmallIcon(Resource.Drawable.ic_launcher);
            }

			if(sound == "1")
			{
				mBuilder.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification));
			}			

			mBuilder.SetContentIntent(contentIntent);
			mNotificationManager.Notify(NOTIFICATION_ID, mBuilder.Build());
		}

	}
}

