
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;

namespace InPublishing
{
    public class RegisterFragment : Fragment
    {
        EditText _txtUser;
        EditText _txtPasswd;
        TextView _lblPrivacy;
        Button _btnRegister;
        TextView _lblResult;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            // return inflater.Inflate(Resource.Layout.YourFragment, container, false);

            var view = inflater.Inflate(Resource.Layout.RegisterScreen, container, false);

            Activity.Title = Activity.ActionBar.Title = GetString(Resource.String.log_account_create);

            _txtUser = view.FindViewById<EditText>(Resource.Id.txtUsername);
            _txtPasswd = view.FindViewById<EditText>(Resource.Id.txtPassword);
            _btnRegister = view.FindViewById<Button>(Resource.Id.btnRegister);
            _lblPrivacy = view.FindViewById<TextView>(Resource.Id.lblPrivacy);
            _lblResult = view.FindViewById<TextView>(Resource.Id.lblResult);

            //txt user
            _txtUser.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
            _txtUser.Hint = GetString(Resource.String.gen_mailAddress);

            //txt password
            _txtPasswd.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

            //pulsante registrati
            _btnRegister.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
            _btnRegister.SetTextColor(Color.White);
            _btnRegister.Text = GetString(Resource.String.gen_register).ToUpper();

            _btnRegister.Click += (sender, e) => 
            {
                Register();
            };

            //etichetta risposta
            _lblResult.Text = "";

            //privacy
            SetPrivacy();

            if(!Utility.IsTablet(Activity))
            {
                var icon = view.FindViewById<ImageView>(Resource.Id.imgLogo);

                var lp = icon.LayoutParameters as LinearLayout.LayoutParams;

                lp.Width = Utility.dpToPx(Activity, 70);
                lp.Height = Utility.dpToPx(Activity, 70);
                lp.TopMargin = 50;
                lp.BottomMargin = 30;

                icon.LayoutParameters = lp;
            }

            return view;
        }

        private void SetPrivacy()
        {
            _lblPrivacy.TextFormatted = Html.FromHtml(GetString(Resource.String.log_privacy));
           
            _lblPrivacy.Click += delegate {
                string prLink = DataManager.Get<ISettingsManager>().Settings.DownloadUrl + "services/privacy.php?app=" + DataManager.Get<ISettingsManager>().Settings.AppId;

                Intent i = new Intent();
                i.SetClass(Application.Context, typeof(BrowserViewScreen));
                i.PutExtra("url", prLink);
                i.PutExtra("tipo", "url");
                i.PutExtra("pageFit", "True");
                i.PutExtra("basePath", "");
                Activity.StartActivity(i);
            };
        }

        private void Register()
        {
            //string username = txtUsername.Text;
            string password = _txtPasswd.Text;
            string mail = _txtUser.Text;

            if(!Regex.Match(mail, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$").Success)
            {
                _lblResult.Text = GetString(Resource.String.log_mail_invalid);
                return;
            }

            if(password.Length < 5)
            {
                _lblResult.Text = GetString(Resource.String.log_passwd_short);
                return;
            }

            Uri host = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);
            if(!Reachability.IsHostReachable("http://" + host.Host))
            {
                var alert = new AlertDialog.Builder(Activity);
                alert.SetTitle(GetString(Resource.String.gen_error));
                alert.SetMessage(GetString(Resource.String.gen_serverNotReachable));
                alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
                alert.Show().SetDivider();
            }

            var result = Notification.RegisterUser(mail, password);

            if(result["success"].ToLower() == "true")
            {
                DataManager.Get<IPreferencesManager>().Preferences.DownloadUsername = mail;
                DataManager.Get<IPreferencesManager>().Preferences.DownloadPassword = password;
                DataManager.Get<IPreferencesManager>().Save();

                Intent myIntent = new Intent (Activity, typeof(DownloadFragment));
                myIntent.PutExtra ("action", "refresh");
                Activity.SetResult (Result.Ok, myIntent);

                var alert = new AlertDialog.Builder(Activity);
                alert.SetMessage(GetString(Resource.String.log_account_created));
                alert.SetPositiveButton("Ok", delegate {
                    Activity.Finish();
                });

                alert.Show();
            }
            else
            {
                int resId = Resources.GetIdentifier(result["errorKey"], "string", Activity.PackageName);

                if (resId == 0) 
                {
                    _lblResult.Text =  result["errorKey"];
                } 
                else 
                {
                    _lblResult.Text = GetString(resId);
                }
            }
        }
    }
}
