using System;
using Android.Content;
using Plugin.GoogleAnalytics;

namespace InPublishing
{
    public static class AnalyticsService
    {
        public static bool Activated { get { return _activated; } }
        private static bool _activated = false;

        public static void Initialize(Context context)
        {
            if(DataManager.Get<ISettingsManager>().Settings.AnalyticsID != "")
            {
                GoogleAnalytics.Current.Config.TrackingId = DataManager.Get<ISettingsManager>().Settings.AnalyticsID;
                GoogleAnalytics.Current.Config.AppId = context.PackageName;
                GoogleAnalytics.Current.Config.AppName = context.ApplicationInfo.LoadLabel(context.PackageManager);
                GoogleAnalytics.Current.Config.AppInstallerId = Guid.NewGuid().ToString();
                //GoogleAnalytics.Current.Config.Debug = DataManager.Get<ISettingsManager>().Settings.Debug;
                GoogleAnalytics.Current.InitTracker();

                _activated = true;
            }
        }

        public static void SendEvent(string category, AnalyticsEventAction action, string name)
        {
            if(!_activated || name == "")
                return;

            string strAction = action.ToDescriptionString();

            GoogleAnalytics.Current.Tracker.SendEvent(category, strAction, name);
        }

        public static void SendEvent(string category, AnalyticsEventAction action)
        {
            if(!_activated)
                return;

            string strAction = action.ToDescriptionString();

            GoogleAnalytics.Current.Tracker.SendEvent(category, strAction);
        }
    }
}
