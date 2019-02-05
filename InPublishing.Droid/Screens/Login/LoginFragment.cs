
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

namespace InPublishing
{
    public class LoginFragment : Fragment
    {
        EditText _txtUser;
        EditText _txtPasswd;
        Button _btnLogin;
        TextView _lblResult;
        ScrollView _scrollView;

        public Action LoginSuccess;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            // return inflater.Inflate(Resource.Layout.YourFragment, container, false);
            var view = inflater.Inflate(Resource.Layout.LoginScreen, container, false);

            Activity.Title = Activity.ActionBar.Title = "Login";

            _txtUser = view.FindViewById<EditText>(Resource.Id.txtUsername);
            _txtPasswd = view.FindViewById<EditText>(Resource.Id.txtPassword);
            _btnLogin = view.FindViewById<Button>(Resource.Id.btnLogin);
            var lblForgot = view.FindViewById<TextView>(Resource.Id.lblForgot);
            var lblRegister = view.FindViewById<TextView>(Resource.Id.lblRegister);
            _lblResult = view.FindViewById<TextView>(Resource.Id.lblResult);

            //txt user
            _txtUser.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

            //txt password
            _txtPasswd.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

            //pulsante login
            _btnLogin.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
            _btnLogin.SetTextColor(Color.White);

            _btnLogin.Click += (sender, e) => 
            {
                /*if(this.Activity is LoginActivity)
                {
                    LoginActivity logActivity = this.Activity as LoginActivity;

                    logActivity.LoadFragment();
                }*/

                /*FragmentManager.BeginTransaction()
                               .Add(12345, new RegisterFragment())
                               .AddToBackStack(null).Commit();*/
                    //.addToBackStack(null)

                Login();
            };

            lblRegister.TextFormatted = Html.FromHtml(GetString(Resource.String.log_account));
            lblRegister.Click += (sender, e) => 
            {
                FragmentManager.BeginTransaction()
                               .Replace(12345, new RegisterFragment())
                               .AddToBackStack(null).Commit();
            };

            lblForgot.Text = GetString(Resource.String.set_forgot);
            lblForgot.Click += (sender, e) => 
            {
                FragmentManager.BeginTransaction()
                               .Replace(12345, new ForgotFragment())
                               .AddToBackStack(null).Commit();
            };

            _lblResult.Text = "";

            if(!Utility.IsTablet(Activity))
            {
                var icon = view.FindViewById<ImageView>(Resource.Id.imgLogo);

                //icon.Visibility = ViewStates.Gone;
                var lp = icon.LayoutParameters as LinearLayout.LayoutParams;

                lp.Width = Utility.dpToPx(Activity, 70);
                lp.Height = Utility.dpToPx(Activity, 70);
                lp.TopMargin = 50;
                lp.BottomMargin = 30;

                icon.LayoutParameters = lp;

                /*var lp = _txtUser.LayoutParameters as LinearLayout.LayoutParams;

                lp.TopMargin = 50;

                _txtUser.LayoutParameters = lp;*/


            }

            return view;
        }

        private void Login()
        {
            Uri host = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);

            if (!Reachability.IsHostReachable("http://" + host.Host))
            {
                var alert = new AlertDialog.Builder(Activity);
                alert.SetTitle(GetString(Resource.String.gen_error));
                alert.SetMessage(GetString(Resource.String.gen_serverNotReachable));
                alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
                alert.Show().SetDivider();
            }

            if (_txtUser.Text == "" || _txtPasswd.Text == "")
                return;

            _txtUser.ClearFocus();
            _txtPasswd.ClearFocus();
            var inputManager = Activity.GetSystemService(Context.InputMethodService) as InputMethodManager;
            inputManager.HideSoftInputFromWindow(_txtUser.WindowToken, HideSoftInputFlags.None);

            LoginResult result = DownloadManager.CheckUser(_txtUser.Text, _txtPasswd.Text);

            if(result.Success)
            {
                Intent myIntent = new Intent (Activity, typeof(DownloadFragment));
                myIntent.PutExtra ("action", "refresh");
                Activity.SetResult (Result.Ok, myIntent);
                //Activity.Finish();

                DataManager.Get<IPreferencesManager>().Preferences.DownloadUsername = _txtUser.Text;
                DataManager.Get<IPreferencesManager>().Preferences.DownloadPassword = _txtPasswd.Text;
                DataManager.Get<IPreferencesManager>().Save();

                Activity.Finish();

                if (LoginSuccess != null)
                    LoginSuccess();
            }
            else
            {
                if(result.Message != null && result.Message != "")
                {
                    _lblResult.Text = result.Message;
                }
                else
                {
                    _lblResult.Text = Activity.GetString(Resource.String.set_loginFailed);
                }
            }
        }
    }
}
