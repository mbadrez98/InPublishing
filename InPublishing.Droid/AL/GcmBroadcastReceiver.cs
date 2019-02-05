using System;
using Android.Content;
using Android.App;

namespace InPublishing
{
	[BroadcastReceiver(Permission= "com.google.android.c2dm.permission.SEND")]
	[IntentFilter(new string[] { "com.google.android.c2dm.intent.RECEIVE" }, Categories = new string[] {"@PACKAGE_NAME@" })]
	[IntentFilter(new string[] { "com.google.android.c2dm.intent.REGISTRATION" }, Categories = new string[] {"@PACKAGE_NAME@" })]
	[IntentFilter(new string[] { "com.google.android.gcm.intent.RETRY" }, Categories = new string[] { "@PACKAGE_NAME@"})]
	[IntentFilter(new[] { Android.Content.Intent.ActionBootCompleted })]
	public class GcmBroadcastReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			GcmIntentService.RunIntentInService(context, intent);
			SetResult(Result.Ok, null, null);
		}
	}
}

