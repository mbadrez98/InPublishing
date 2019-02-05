using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Preferences;
using System.Xml.Linq;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace InPublishing
{
	public class SettingsFragment : PreferenceFragment
	{
		PreferenceCategory _PinCategory;
		PreferenceCategory _DownCategory;
		SwitchPreference _PinEnable;
		SwitchPreference _DownEnable;
		Preference _DownUser;
		Preference _DownLogout;
		Preference _DownReset;

        PreferenceCategory _NotifyCategory;
        Preference _NotifyTest;

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			Button btnBack = Activity.ActionBar.CustomView.FindViewById<Button>(Resource.Id.btnBack);
			btnBack.Text = Activity.Title = Activity.ActionBar.Title = GetString(Resource.String.set_title);
			btnBack.SetCompoundDrawables(null, null, null, null);

			this.AddPreferencesFromResource(Resource.Xml.Settings);

			this.SetDownloadSettings();
		}

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            view.SetBackgroundColor(Color.White);

            return view;
        }

		private void SetDownloadSettings()
		{
			_PinCategory = (PreferenceCategory)this.FindPreference("Pin");
			_PinEnable = (SwitchPreference)this.FindPreference("PinActive");
			_DownCategory = (PreferenceCategory)this.FindPreference("Download");
			_DownEnable = (SwitchPreference)this.FindPreference("DownActive");
			_DownUser = (Preference)this.FindPreference("DownUsername");
			_DownLogout = (Preference)this.FindPreference("DownLogout");
			_DownReset = (Preference)this.FindPreference("DownReset");

            _NotifyCategory = (PreferenceCategory)this.FindPreference("Notifications");
            _NotifyTest = (Preference)this.FindPreference("NotTest");


			//pin
			_PinCategory.Title = GetString(Resource.String.set_pin);

			//_PinEnable.WidgetLayoutResource = Resource.Layout.CustomSwitch;

			if(DataManager.Get<ISettingsManager>().Settings.PinEnabled)
			{
				_PinEnable.Title = GetString(Resource.String.set_pinEnabled);
				_PinEnable.Checked = DataManager.Get<IPreferencesManager>().Preferences.PinEnabled;
				_PinEnable.PreferenceChange += (sender, e) =>
				{
					if(_PinEnable.Checked != (bool)e.NewValue)
					{
						ShowPinDialog((bool)e.NewValue);
					}
				};
			}
			else
			{
				_PinCategory.RemovePreference(_PinEnable);
				_PinCategory.Enabled = false;
				_PinCategory.ShouldDisableView = true;
				_PinCategory.Title = "";
			}

			//download
			//_DownEnable.WidgetLayoutResource = Resource.Layout.CustomSwitch;
			_DownEnable.Title = GetString(Resource.String.set_downEnabled);
			_DownEnable.Checked = DataManager.Get<IPreferencesManager>().Preferences.DownloadEnabled;
			_DownEnable.PreferenceChange += (sender, e) => 
			{
				DataManager.Get<IPreferencesManager>().Preferences.DownloadEnabled = (bool)e.NewValue;
				DataManager.Get<IPreferencesManager>().Save();

				_DownUser.Enabled = (bool)e.NewValue;
				_DownLogout.Enabled = (bool)e.NewValue;
				_DownReset.Enabled = (bool)e.NewValue;
			};

			//in base al tipo di login setto le impostazioni disponibili
			if(DataManager.Get<ISettingsManager>().Settings.DownloadUtenti || DataManager.Get<ISettingsManager>().Settings.DownloadPassword)
			{
				if(DownloadManager.IsLogged())
				{
					if(DataManager.Get<ISettingsManager>().Settings.DownloadUtenti)
					{
						_DownUser.Summary = DataManager.Get<IPreferencesManager>().Preferences.DownloadUsername;
					}
					else
					{
						_DownUser.Summary = GetString(Resource.String.set_logged);
					}

					_DownCategory.RemovePreference(_DownReset);
				}
				else
				{
					_DownUser.Summary = GetString(Resource.String.set_notLogged);

					_DownCategory.RemovePreference(_DownLogout);
				}

				//attivo il popup per il login
				_DownUser.PreferenceClick += (sender, e) => 
				{
					ShowLoginDialog();
				};
			}
			else
			{
				_DownCategory.RemovePreference(_DownUser);
			}

			//pulsante logout
			if(DataManager.Get<ISettingsManager>().Settings.DownloadUtenti || DataManager.Get<ISettingsManager>().Settings.DownloadPassword)
			{
				_DownLogout.Title = GetString(Resource.String.set_logout);
				_DownLogout.Summary = GetString(Resource.String.set_publicAccess);
				_DownLogout.PreferenceClick += (sender, e) =>
				{
					var dialog = new AlertDialog.Builder(Activity);
					dialog.SetTitle(_DownCategory.Title);
					dialog.SetMessage(GetString(Resource.String.set_logoutConfirm));

					dialog.SetNegativeButton(GetString(Resource.String.gen_cancel), delegate
					{
						return;
					});

					dialog.SetPositiveButton(GetString(Resource.String.set_logout), delegate
					{				
						DataManager.Get<IPreferencesManager>().Preferences.DownloadUsername = "";
						DataManager.Get<IPreferencesManager>().Preferences.DownloadPassword = "";
						DataManager.Get<IPreferencesManager>().Save();

						_DownUser.Summary = GetString(Resource.String.set_notLogged);

						_DownCategory.RemovePreference(_DownLogout);

						if(DataManager.Get<ISettingsManager>().Settings.DownloadUtenti && DataManager.Get<ISettingsManager>().Settings.PasswordReset)
						{
							_DownCategory.AddPreference(_DownReset);
						}
					});

					dialog.Create();
					dialog.Show().SetDivider();
				};
			}
			else
			{
				_DownCategory.RemovePreference(_DownLogout);
			}

			//reset password
			if(DataManager.Get<ISettingsManager>().Settings.PasswordReset)
			{
				if(DataManager.Get<ISettingsManager>().Settings.DownloadUtenti)
				{
					_DownReset.Title = GetString(Resource.String.set_forgot);
					_DownReset.Summary = GetString(Resource.String.set_reset);
					_DownReset.PreferenceClick += (sender, e) =>
					{
						ShowResetDialog();
					};
				}
			}
			else
			{
				_DownCategory.RemovePreference(_DownReset);
			}

            //notifiche
            if(!Utility.IsMediabookApp)
            {
                _NotifyCategory.RemovePreference(_NotifyTest);
                _NotifyCategory.Enabled = false;
                _NotifyCategory.ShouldDisableView = true;
                _NotifyCategory.Title = "";
            }
            else
            {
                _NotifyTest.PreferenceClick += (sender, e) => 
                {
                    Notification noti = new Notification();
                    noti.SendTest(Activity.BaseContext.DeviceInfo(), "Notifica di esempio");
                };
            }
		}

		private void ShowLoginDialog()
		{
			var builder = new AlertDialog.Builder(Activity);
			builder.SetTitle(_DownCategory.Title);

			builder.SetView(Activity.LayoutInflater.Inflate(Resource.Layout.LoginDialog, null));

			builder.SetPositiveButton("Login", (EventHandler<DialogClickEventArgs>)null);
			builder.SetNegativeButton(GetString(Resource.String.gen_cancel), (EventHandler<DialogClickEventArgs>)null);

			builder.SetCancelable(true);

			var dialog = builder.Create();
			dialog.Show();
			dialog.SetDivider();

			EditText txtUser = dialog.FindViewById<EditText>(Resource.Id.txtUser); //new EditText(Activity);
			EditText txtPasswd = dialog.FindViewById<EditText>(Resource.Id.txtPasswd); //new EditText(Activity);
			TextView lblSuccess = dialog.FindViewById<TextView>(Resource.Id.lblSuccess);
			ProgressBar prgLogin = dialog.FindViewById<ProgressBar>(Resource.Id.prgLogin);

            txtUser.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
            txtPasswd.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

			//se è edicola con password nasconto il campo username
			if (!DataManager.Get<ISettingsManager>().Settings.DownloadUtenti)
			{
				txtUser.Visibility = ViewStates.Gone;
			}

			//pulsante login
			var btnLogin = dialog.GetButton((int)DialogButtonType.Positive);

			if(btnLogin == null)
				return;

			btnLogin.Click += (sender, e) => 
			{
				Activity.RunOnUiThread(() => 
				{
					lblSuccess.Text = "";
					prgLogin.Visibility = ViewStates.Visible;
					lblSuccess.Visibility = ViewStates.Gone;
				});

				Uri host = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);
				if (!Reachability.IsHostReachable("http://" + host.Host))
				{
					var alert = new AlertDialog.Builder(Activity);
					alert.SetTitle(GetString(Resource.String.gen_error));
					alert.SetMessage(GetString(Resource.String.gen_serverNotReachable));
					alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
					alert.Show().SetDivider();

					return;
				}

				var result = DownloadManager.CheckUser(txtUser.Text, txtPasswd.Text);

				if(result.Success)
				{
					DataManager.Get<IPreferencesManager>().Preferences.DownloadUsername = txtUser.Text;
					DataManager.Get<IPreferencesManager>().Preferences.DownloadPassword = txtPasswd.Text;
					DataManager.Get<IPreferencesManager>().Save();

					if(DataManager.Get<ISettingsManager>().Settings.DownloadUtenti)
					{
						_DownUser.Summary = txtUser.Text;
					}
					else if(DataManager.Get<ISettingsManager>().Settings.DownloadPassword)
					{
						_DownUser.Summary = GetString(Resource.String.set_logged);
					}

					//registro il device
					Notification notif = new Notification();

                    string pushId = Activity.DevicePushId();

                    var data = Activity.BaseContext.DeviceInfo();
                    data.Add("deviceToken", Activity.DevicePushId());

					notif.RegisterDevice(data);

					if((Preference)this.FindPreference("DownLogout") == null)
					{
						_DownCategory.AddPreference(_DownLogout);
					}

					if((Preference)this.FindPreference("DownReset") != null)
					{
						_DownCategory.RemovePreference(_DownReset);
					}

					dialog.Dismiss();
				}
				else
				{
					if(result.Message != null && result.Message != "")
					{
						_DownUser.Summary = result.Message;
					}
					else
					{
						_DownUser.Summary = GetString(Resource.String.set_notLogged);
					}

					Activity.RunOnUiThread(() => 
					{
						if(result.Message != null && result.Message != "")
						{
							lblSuccess.Text = result.Message;
						}
						else
						{
							lblSuccess.Text = GetString(Resource.String.set_loginFailed);
						}

						lblSuccess.Visibility = ViewStates.Visible;
						prgLogin.Visibility = ViewStates.Gone;
					});

					DataManager.Get<IPreferencesManager>().Preferences.DownloadUsername = "";
					DataManager.Get<IPreferencesManager>().Preferences.DownloadPassword = "";
					DataManager.Get<IPreferencesManager>().Save();

					if((Preference)this.FindPreference("DownLogout") != null)
					{
						_DownCategory.RemovePreference(_DownLogout);
					}

					if(DataManager.Get<ISettingsManager>().Settings.PasswordReset && (Preference)this.FindPreference("DownReset") == null)
					{
						_DownCategory.AddPreference(_DownReset);
					}
				}
			};
		}

		private void ShowResetDialog()
		{
			var builder = new AlertDialog.Builder(Activity);
			builder.SetTitle(GetString(Resource.String.set_reset));

			builder.SetView(Activity.LayoutInflater.Inflate(Resource.Layout.ResetPasswordDialog, null));

			builder.SetPositiveButton(GetString(Resource.String.gen_send), (EventHandler<DialogClickEventArgs>)null);
			builder.SetNegativeButton(GetString(Resource.String.gen_cancel), (EventHandler<DialogClickEventArgs>)null);

			builder.SetCancelable(true);

			var dialog = builder.Create();
			dialog.Show();
			dialog.SetDivider();

			EditText txtMail = dialog.FindViewById<EditText>(Resource.Id.txtMail);
			TextView lblSuccess = dialog.FindViewById<TextView>(Resource.Id.lblSuccess);
			ProgressBar prgLogin = dialog.FindViewById<ProgressBar>(Resource.Id.prgLogin);

            txtMail.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

			//se è edicola con password nascondo il campo username

			//pulsante login
			var btnSend = dialog.GetButton((int)DialogButtonType.Positive);

			if(btnSend == null)
				return;

			btnSend.Click += (sender, e) => 
			{
				Activity.RunOnUiThread(() => 
				{
					lblSuccess.Text = "";
					prgLogin.Visibility = ViewStates.Visible;
					lblSuccess.Visibility = ViewStates.Gone;
				});

				string url = DataManager.Get<ISettingsManager>().Settings.DownloadUrl;

				if (!Reachability.IsHostReachable("http://" + new Uri(url).Host))
				{
					var alert = new AlertDialog.Builder(Activity);
					alert.SetTitle(GetString(Resource.String.gen_error));
					alert.SetMessage(GetString(Resource.String.gen_serverNotReachable));
					alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
					alert.Show().SetDivider();

					return;
				}

				url += "services/edicola_services.php?action=resetPassword&mail=" + txtMail.Text + "&app=" + DataManager.Get<ISettingsManager>().Settings.AppId;

				try
				{
					XDocument xDoc = XDocument.Load(url);

					var result = xDoc.Element("root").Element("result");

					if(XMLUtils.GetBoolValue(result.Attribute("success")))
					{
						var alert = new AlertDialog.Builder(Activity);
						alert.SetTitle(GetString(Resource.String.set_reset));
						alert.SetMessage(GetString(Resource.String.set_mailPasswd));
						alert.SetPositiveButton("Ok", delegate {
							dialog.Dismiss();
						});
						alert.Show().SetDivider();
					}
					else
					{
						switch (XMLUtils.GetIntValue(result.Attribute("code"))) 
						{
							case 1:
								lblSuccess.Text = GetString(Resource.String.set_mailNotValid);
								break;
							case 2:
								lblSuccess.Text = GetString(Resource.String.set_mailProblem);
								break;
							default:
								break;
						}

						lblSuccess.Visibility = ViewStates.Visible;
						prgLogin.Visibility = ViewStates.Gone;
					}
				}
				catch(Exception ex)
				{
					Log.Info("resetPassword", ex.Message);
					lblSuccess.Text = GetString(Resource.String.gen_serverNotReachable);
				}
			};
		}

		private void ShowPinDialog(bool enable)
		{
			var builder = new AlertDialog.Builder(Activity);
			builder.SetTitle(GetString(Resource.String.set_pin));

			builder.SetView(Activity.LayoutInflater.Inflate(Resource.Layout.PinDialog, null));

			builder.SetPositiveButton("OK", (EventHandler<DialogClickEventArgs>)null);
			builder.SetNegativeButton(GetString(Resource.String.gen_cancel), (EventHandler<DialogClickEventArgs>)null);

			builder.SetCancelable(true);

			var dialog = builder.Create();
			dialog.Show();
			dialog.SetDivider();

			EditText txtPin = dialog.FindViewById<EditText>(Resource.Id.txtPin); //new EditText(Activity);
			EditText txtPin2 = dialog.FindViewById<EditText>(Resource.Id.txtPin2); //new EditText(Activity);
			TextView lblSuccess = dialog.FindViewById<TextView>(Resource.Id.lblSuccess);

            txtPin.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
            txtPin2.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

			if(!enable)
			{
				txtPin2.Visibility = ViewStates.Gone;
			}

			//pulsante annulla
			var btnCancel = dialog.GetButton((int)DialogButtonType.Negative);
			btnCancel.Click += delegate
			{
				_PinEnable.Checked = !enable;
				dialog.Dismiss();
			};

			//pulsante ok
			var btnOK = dialog.GetButton((int)DialogButtonType.Positive);

			if(btnOK == null)
				return;

			btnOK.Click += (sender, e) => 
			{
				string error = "";
				if(enable)
				{                  
					if(txtPin.Text != txtPin2.Text)
					{
						error = GetString(Resource.String.set_pinMatch);
					}

					if(txtPin.Text == "")
					{
						error = GetString(Resource.String.set_pinEmpty);
					}

					if(txtPin.Text.Length < 4)
					{
						error = GetString(Resource.String.set_pinLenght);
					}
				}
				else
				{
					if(Encryptor.MD5Hash(txtPin.Text) != DataManager.Get<IPreferencesManager>().Preferences.PinCode)
					{
						error = GetString(Resource.String.set_pinError);
					}
				}

				if(error != "")
				{
					Activity.RunOnUiThread(() => 
					{
						lblSuccess.Text = error;
						lblSuccess.Visibility = ViewStates.Visible;

						_PinEnable.Checked = !enable;
					});
					return;
				}

				DataManager.Get<IPreferencesManager>().Preferences.PinEnabled = enable;
				if(enable)
				{
					DataManager.Get<IPreferencesManager>().Preferences.PinCode = Encryptor.MD5Hash(txtPin.Text);
					_PinEnable.Checked = true;
				}
				else
				{
					DataManager.Get<IPreferencesManager>().Preferences.PinCode = "";
					_PinEnable.Checked = false;
				}

				DataManager.Get<IPreferencesManager>().Save();

				dialog.Dismiss();
			};
		}
	}
}