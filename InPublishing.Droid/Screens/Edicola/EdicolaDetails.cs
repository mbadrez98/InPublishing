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

namespace InPublishing
{
	[Activity(Label = "EdicolaDetails")]			
	public class EdicolaDetails : Dialog
	{
        Pubblicazione _pubblicazione;
		public Action DeleteAction;
		public Action OpenAction;

        public EdicolaDetails(Context context, Pubblicazione pub) : base(context)
		{
			_pubblicazione = pub;
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			this.Window.RequestFeature(WindowFeatures.NoTitle);

			SetContentView(Resource.Layout.EdicolaDetails);

			this.SetTitle(_pubblicazione.Titolo);

			TextView txtTitolo = FindViewById<TextView>(Resource.Id.txtTitolo);
			TextView txtAutore = FindViewById<TextView>(Resource.Id.txtAutore);
			TextView txtPubblicato = FindViewById<TextView>(Resource.Id.txtPubblicato);
			TextView txtScadenza = FindViewById<TextView>(Resource.Id.txtScadenza);
			ImageView imgCover = FindViewById<ImageView>(Resource.Id.imgCover);
			Button btnOpen = FindViewById<Button>(Resource.Id.btnOpen);
			Button btnDelete = FindViewById<Button>(Resource.Id.btnDelete);

			txtTitolo.Text = _pubblicazione.Titolo;
			txtAutore.Text = _pubblicazione.Autore.Trim();

			string imgPath = System.IO.Path.Combine(_pubblicazione.Path, "copertina.jpg");
			if(System.IO.File.Exists(imgPath))
			{
                MBImageLoader.DisplayDiskImage(imgPath, imgCover, new PointF(360, 360));
			}

			if(_pubblicazione.DataPubblicazione != DateTime.MinValue)
			{
				txtPubblicato.Text = string.Format(Context.GetString(Resource.String.pub_publishedOn), _pubblicazione.DataPubblicazione.ToString("dd-MM-yyyy"));
			}
			else
			{
				txtPubblicato.Visibility = ViewStates.Gone;
			}

			if(_pubblicazione.DataScadenza != DateTime.MinValue)
			{
				txtScadenza.Text = string.Format(Context.GetString(Resource.String.pub_expireOn), _pubblicazione.DataScadenza.ToString("dd-MM-yyyy"));
			}
			else
			{
				txtScadenza.Visibility = ViewStates.Gone;
			}

			//pulsante apri
			btnOpen.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor));
			btnOpen.Text = Context.GetString(Resource.String.pub_open);
			btnOpen.Click += delegate
			{
				if(OpenAction != null)
				{
					OpenAction();
				}
			};

			//pulsante elimina
			btnDelete.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor));
			btnDelete.Text = Context.GetString(Resource.String.pub_delete);
			btnDelete.Click += delegate
			{
				if(DeleteAction != null)
				{
					DeleteAction();
				}
			};
		}

		/*public override void OnStart()
		{
			base.OnStart();

			Rect frame = new Rect();
			Activity.Window.DecorView.GetWindowVisibleDisplayFrame(frame);

			WindowManagerLayoutParams lp = Dialog.Window.Attributes;

			Dialog.Window.SetGravity(GravityFlags.Top | GravityFlags.Right);

			lp.DimAmount = 0;
			//lp.Width = Utility.dpToPx(Context, 300); // 600;
			//lp.Height = frame.Bottom - frame.Top*4;
			//lp.X = 50;
			lp.Y = frame.Top*2;

			Dialog.Window.Attributes = lp;

			Dialog.Window.AddFlags(WindowManagerFlags.DimBehind);

			//Dialog.Window.SetLayout(240, 300);
			//Dialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
		}*/
	}
}

