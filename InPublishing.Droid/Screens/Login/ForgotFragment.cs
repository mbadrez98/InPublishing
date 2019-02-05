
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
    public class ForgotFragment : Fragment
    {
        EditText _txtMail;
        Button _btnSend;
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

            var view = inflater.Inflate(Resource.Layout.ForgotScreen, container, false);

            Activity.Title = Activity.ActionBar.Title = GetString(Resource.String.set_reset);

            _txtMail = view.FindViewById<EditText>(Resource.Id.txtMail);
            _btnSend = view.FindViewById<Button>(Resource.Id.btnSend);
            _lblResult = view.FindViewById<TextView>(Resource.Id.lblResult);

            //txt mail
            _txtMail.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
            _txtMail.Hint = GetString(Resource.String.gen_mailAddress);


            //pulsante invia
            _btnSend.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
            _btnSend.SetTextColor(Color.White);
            _btnSend.Text = GetString(Resource.String.gen_send).ToUpper();

            _btnSend.Click += (sender, e) => 
            {
                SendReset();
            };

            //etichetta risposta
            _lblResult.Text = "";

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


        private void SendReset()
        {
            string url = DataManager.Get<ISettingsManager>().Settings.DownloadUrl;

            Uri host = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);
            if(!Reachability.IsHostReachable("http://" + host.Host))
            {
                var alert = new AlertDialog.Builder(Activity);
                alert.SetTitle(GetString(Resource.String.gen_error));
                alert.SetMessage(GetString(Resource.String.gen_serverNotReachable));
                alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
                alert.Show().SetDivider();
            }

            url += "services/edicola_services.php?action=resetPassword&mail=" + _txtMail.Text + "&app=" + DataManager.Get<ISettingsManager>().Settings.AppId;

            try
            {
                XDocument xDoc = XDocument.Load(url);

                var result = xDoc.Element("root").Element("result");

                if(XMLUtils.GetBoolValue(result.Attribute("success")))
                {
                    _lblResult.Text = GetString(Resource.String.set_mailPasswd);
                }
                else
                {
                    switch (XMLUtils.GetIntValue(result.Attribute("code"))) 
                    {
                        case 1:
                            _lblResult.Text = GetString(Resource.String.log_mail_invalid);
                            break;
                        case 2:
                            _lblResult.Text = GetString(Resource.String.set_mailProblem);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Info("resetPassword", ex.Message);
                _lblResult.Text = GetString(Resource.String.gen_tryLater);
            }
        }
    }
}
