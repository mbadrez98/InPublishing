
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;

namespace InPublishing
{
    [Activity(Label = "Login", Theme = "@style/Blue", ScreenOrientation = ScreenOrientation.Portrait)]
    public class LoginActivity : FragmentActivity
    {
        FrameLayout _contentView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.Window.SetSoftInputMode(SoftInput.AdjustResize);

            ActionBar.SetDisplayUseLogoEnabled(false);
            ActionBar.SetIcon(new ColorDrawable(Color.Transparent));
            ActionBar.SetHomeButtonEnabled(false);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true);

            ActionBar.SetBackgroundDrawable(new ColorDrawable(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.NavigationBarColor)));

            //back
            var iconId = Resources.GetIdentifier("up", "id", "android");
            var abBack = FindViewById<ImageView>(iconId);
            abBack.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);

            //colore titolo
            var titleId = Resources.GetIdentifier("action_bar_title", "id", "android");
            var abTitle = FindViewById<TextView>(titleId);
            abTitle.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor));

            this.Title = ActionBar.Title = "Login";

            _contentView = new FrameLayout(this);

            //_contentView.SetBackgroundColor(Color.Red);

            _contentView.Id = 12345;

            this.AddContentView(_contentView, new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.MatchParent));

            Action onSuccess = (Action)ActivitiesBringe.GetObject("loginSuccess");

            LoginFragment fragment = new LoginFragment();
            fragment.LoginSuccess = onSuccess;

            FragmentManager.BeginTransaction()
                           .Add(_contentView.Id, fragment).Commit();

        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
                if(FragmentManager.BackStackEntryCount > 0)
                {
                    FragmentManager.PopBackStack();
                    FragmentManager.ExecutePendingTransactions();
                }
                else
                {
                    Finish();
                }
            }

            return false;
        }

        public override void OnBackPressed()
        {
            //base.OnBackPressed();

            if(FragmentManager.BackStackEntryCount > 0)
            {
                FragmentManager.PopBackStack();
                FragmentManager.ExecutePendingTransactions();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        /*public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            if(newConfig.HardKeyboardHidden == Android.Content.Res.HardKeyboardHidden.No)
            {
                
            }
            else if(newConfig.HardKeyboardHidden == Android.Content.Res.HardKeyboardHidden.Yes)
            {

            }
        }*/
    }
}
