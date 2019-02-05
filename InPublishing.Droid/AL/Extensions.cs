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
using Android.Graphics;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.Content.Res;
using Android.Util;
using System.Collections.Specialized;
using Android.Net.Wifi;
using Java.Lang.Reflect;
using Android;

namespace InPublishing
{
	public static class ColorExtensions
	{
		public static Color FromHex(this Color color, int hexValue)
		{
			return Color.Rgb(
				(((int)((hexValue & 0xFF0000) >> 16))/255),
				(((int)((hexValue & 0xFF00) >> 8))/255),
				(((int)(hexValue & 0xFF))/255)
				);
		}

		public static Color FromHex(this Color color, string hexValue)
		{
			int val;

			hexValue = hexValue.Replace("#", "");

			val = Convert.ToInt32(hexValue, 16);

			return Color.Rgb(
				(((int)((val & 0xFF0000) >> 16))),
				(((int)((val & 0xFF00) >> 8))),
				(((int)(val & 0xFF)))
				);
		}

		public static Color FromHexA(this Color color, string hexValue)
		{
			int val;

			hexValue = hexValue.Replace("#", "");

			val = Convert.ToInt32(hexValue, 16);

            int a, r, g, b;

            r = (((int)((val & 0xFF000000) >> 24)));
            g = (((int)((val & 0xFF0000) >> 16)));
            b = (((int)((val & 0xFF00) >> 8)));
            a = (((int)(val & 0xFF)));

			return Color.Argb(a, r, g, b);
		}

		public static Color SetAlpha(this Color color, float factor) 
		{
			int alpha = (int)Math.Round(color.A * factor);
			int red = color.R;
			int green = color.G;
			int blue = color.B;

			return Color.Argb(alpha, red, green, blue);
		}

        public static Color Lighter(this Color color, float factor)
		{
			int red = color.R;
            int green = color.G;
            int blue = color.B;
            int alpha = color.A;

            red = (int)Math.Min(red + (red * factor), 255);
            green = (int)Math.Min(green + (green * factor), 255);
            blue = (int)Math.Min(blue + (blue * factor), 255);

            return Color.Argb(alpha, red, green, blue);
        }

		public static Color Darker(this Color color, float factor)
		{
			int red = color.R;
            int green = color.G;
            int blue = color.B;
            int alpha = color.A;

            red = (int)Math.Max(red - (red * factor), 0);
            green = (int)Math.Max(green - (green * factor), 0);
            blue = (int)Math.Max(blue - (blue * factor), 0);

            return Color.Argb(alpha, red, green, blue);            
		}

        public static float GetBrightness(this Color color)
        {
            System.Drawing.Color c = System.Drawing.Color.FromArgb(color.R, color.G, color.B);

            return c.GetBrightness();
        }
	}

	public static class StringExtensions
	{
		public static string t(this string translate, string comment = "")
		{
			return translate;// NSBundle.MainBundle.LocalizedString(translate, comment);
		}
	}

	public static class Utility
	{
		public static string AppName(this Activity activity)
		{
			PackageManager pm = activity.ApplicationContext.PackageManager; //getApplicationContext().getPackageManager();
			ApplicationInfo ai;
			try 
			{
				ai = pm.GetApplicationInfo(activity.PackageName, 0);
			} 
			catch (Android.Content.PM.PackageManager.NameNotFoundException ex) 
			{
				Log.Error("AppName", ex.Message);
				ai = null;
			}

			return (String) (ai != null ? pm.GetApplicationLabel(ai) : "(unknown)");
		}

		public static Drawable GetDrawable(this int attr, Context context)
		{
			int[] attrs = new int[] { attr};

			TypedArray ta = context.Theme.ObtainStyledAttributes(attrs);

			Drawable drawableFromTheme = ta.GetDrawable(0);

			ta.Recycle();

			return drawableFromTheme;
		}

		public static bool IsTablet(Context context) 
		{
			return (context.Resources.Configuration.ScreenLayout
				& ScreenLayout.SizeMask)
				>= ScreenLayout.SizeLarge;
		}

		public static int dpToPx(Context context, float valueInDp) 
		{
			DisplayMetrics metrics = context.Resources.DisplayMetrics;
			return (int) TypedValue.ApplyDimension(ComplexUnitType.Dip, valueInDp, metrics);
		}

        public static int PxToDp(Context context, int valueInPixel)
        {
            var dp = (int)((valueInPixel) / context.Resources.DisplayMetrics.Density);
            return dp;
        }

		public static int ActionBarHeight(this Context activity)
		{
			TypedValue tv = new TypedValue();
			if (activity.Theme.ResolveAttribute(Android.Resource.Attribute.ActionBarSize, tv, true))
			{
				return TypedValue.ComplexToDimensionPixelSize(tv.Data, activity.Resources.DisplayMetrics);
			}

			return -1;
		}

		public static NameValueCollection DeviceInfo(this Context Activity)
		{
            string mode = "0";

			if(DataManager.Get<ISettingsManager>().Settings.DownloadUtenti)
			{
				mode = "2";
			}
			else if(DataManager.Get<ISettingsManager>().Settings.DownloadPassword)
			{
				mode = "1";
			}

			/*WifiManager manager = (WifiManager) Activity.GetSystemService(Context.WifiService);
			WifiInfo info = manager.ConnectionInfo;
			String address = info.MacAddress;*/

			NameValueCollection data = new NameValueCollection() 
			{ 
				{ "alreadyRun", DataManager.Get<IPreferencesManager>().Preferences.AlreadyRun.ToString()},
				{ "deviceMac", DataManager.Get<ISettingsManager> ().Settings.DeviceUID },
				{ "deviceName", "" },
				{ "deviceOS", "Android"},
				{ "deviceOSVersion", Android.OS.Build.VERSION.Release},
				{ "deviceModel", Android.OS.Build.Manufacturer + " " + Android.OS.Build.Model},
				{ "appName", Activity.ApplicationInfo.LoadLabel(Activity.PackageManager)},
				{ "appVersion", DataManager.Get<ISettingsManager>().Settings.InpubVersion /*Activity.PackageManager.GetPackageInfo(Activity.PackageName, 0).VersionName*/},
				{ "username", DataManager.Get<IPreferencesManager>().Preferences.DownloadUsername},
				{ "password", DataManager.Get<IPreferencesManager>().Preferences.DownloadPassword},                        
				{ "language", Java.Util.Locale.Default.Language},
				{ "mode", mode},
				{ "notify", DataManager.Get<ISettingsManager>().Settings.NotificationsEnabled.ToString()},
				{ "app", DataManager.Get<ISettingsManager>().Settings.AppId},
                { "deviceType", ((int)(DataManager.Get<ISettingsManager>().Settings.DeviceType)).ToString() }
			}; 

			return data;
		}

        public static string DevicePushId(this Context context)
        {
            ISharedPreferences prefs = context.GetSharedPreferences(context.PackageName, FileCreationMode.Private);
            string registrationId = prefs.GetString("registration_id", "");

            if (registrationId == "")
            {
                Log.Info("DevicePushId", "Registration not found.");
                return "";
            }
            // Check if app was updated; if so, it must clear the registration ID
            // since the existing regID is not guaranteed to work with the new
            // app version.
            int registeredVersion = prefs.GetInt("appVersion", int.MinValue);
            int currentVersion = context.AppVersion();
            if (registeredVersion != currentVersion)
            {
                Log.Info("DevicePushId", "App version changed.");
                return "";
            }
            return registrationId;
        }

        private static int AppVersion(this Context context)
        {
            try
            {
                PackageInfo packageInfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);
                return packageInfo.VersionCode;
            }
            catch (Android.Content.PM.PackageManager.NameNotFoundException e)
            {
                // should never happen
                throw new Java.Lang.RuntimeException("Could not get package name: " + e);
            }
        }

		public static void RecycleBitmap(this ImageView imageView)
		{
            try
            {
                if (imageView == null) {
    				return;
    			}

    			Drawable toRecycle = imageView.Drawable;
    			if (toRecycle != null) 
                {
                    if(((BitmapDrawable)toRecycle).Bitmap != null)
                    {
                        ((BitmapDrawable)toRecycle).Bitmap.Recycle();
                    }
    			}
            }
            catch(Exception ex)
            {
                Log.Error("RecycleBitmap", ex.Message);
            }
		}

		public static void SetDivider(this Dialog dialog)
		{
            string mainColor = "#" + DataManager.Get<ISettingsManager>().Settings.ButtonColor;

			var resources = dialog.Context.Resources;
			var color = Color.Red;//dialog.Context.Resources.GetColor(Resource.Color.dialog_textcolor);
			var background = Color.ParseColor(mainColor);//dialog.Context.Resources.GetColor(Resource.Color.dialog_background);

			var titleDividerId = resources.GetIdentifier("titleDivider", "id", "android");
			var titleDivider = dialog.Window.DecorView.FindViewById(titleDividerId);
			titleDivider.SetBackgroundColor(background); // change divider color
		}

		public static void Colorize(this Drawable draw, string Color)
		{
			var color = Android.Graphics.Color.Transparent.FromHex (Color);

			draw.SetColorFilter(color, Android.Graphics.PorterDuff.Mode.SrcAtop);
		}

		public static void Colorize(this ImageView img, string Color)
		{
			var color = Android.Graphics.Color.Transparent.FromHex (Color);

			img.SetColorFilter(color, Android.Graphics.PorterDuff.Mode.SrcAtop);
		}

        public static bool IsMediabookApp
        {
            get
            {
                return DataManager.Get<ISettingsManager>().Settings.ShareDir;
            }
        }

        public static bool CanAccessExternal(this Activity context)
        {
            if (!IsMediabookApp)
                return true;
            
            //if ((int)Build.VERSION.SdkInt <= 23)
            {
                if (context.PackageManager.CheckPermission(Manifest.Permission.ReadExternalStorage, context.PackageName) != Android.Content.PM.Permission.Granted
                    && context.PackageManager.CheckPermission(Manifest.Permission.WriteExternalStorage, context.PackageName) != Android.Content.PM.Permission.Granted)
                {
                    //var permissions = new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage };
                    //context.RequestPermissions(permissions, 1);

                    return false;
                }
            }

            return true;
        }
	}
}

