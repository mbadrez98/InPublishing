using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Net;
using System.Net;
using Android.Util;

namespace InPublishing
{
	public enum NetworkStatus 
	{
		NotReachable,
		ReachableViaCarrierDataNetwork,
		ReachableViaWiFiNetwork
	}

	public static class Reachability
	{
		public static bool IsHostReachable (string host)
		{
			if (host == null || host.Length == 0)
				return false;

			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(host);
			request.Timeout = 15000;
			request.Method = "GET"; // As per Lasse's comment
			request.AllowWriteStreamBuffering = true;
			//request.AllowReadStreamBuffering = true;

			//return true;

			try
			{
				using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
				{
					return response.StatusCode == HttpStatusCode.OK;
				}
			}
			catch (WebException ex)
			{
				Log.Error("IsHostReachable", ex.Message);
				return false;
			}
		}

		public static NetworkStatus InternetConnectionStatus ()
		{
			var connectivityManager = (ConnectivityManager)Application.Context.GetSystemService(Application.ConnectivityService);

			if(connectivityManager.GetNetworkInfo(ConnectivityType.Mobile) != null)
			{
				var mobileState = connectivityManager.GetNetworkInfo(ConnectivityType.Mobile).GetState();
				if(mobileState == NetworkInfo.State.Connected)
				{
					return NetworkStatus.ReachableViaCarrierDataNetwork;
				}
			}

			var wifiState = connectivityManager.GetNetworkInfo(ConnectivityType.Wifi).GetState();
			if (wifiState == NetworkInfo.State.Connected)
			{
				return NetworkStatus.ReachableViaWiFiNetwork;
			}

			return NetworkStatus.NotReachable;
			/*NetworkReachabilityFlags flags;
			bool defaultNetworkAvailable = IsNetworkAvailable (out flags);
			if (defaultNetworkAvailable){
				if ((flags & NetworkReachabilityFlags.IsDirect) != 0)
					return NetworkStatus.NotReachable;
			} else if ((flags & NetworkReachabilityFlags.IsWWAN) != 0)
				return NetworkStatus.ReachableViaCarrierDataNetwork;
			else if (flags == 0)
				return NetworkStatus.NotReachable;
			return NetworkStatus.ReachableViaWiFiNetwork;*/
		}
	}
}

