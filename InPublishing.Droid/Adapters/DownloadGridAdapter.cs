using System;
using System.Collections.Generic;
using Android.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using System.IO;
using Android.Graphics.Drawables;
using Android.Content;
using Android.OS;
using System.Threading;
using Android.Database;

namespace InPublishing
{
	public class DownloadGridAdapter : BaseAdapter<Object> 
	{
		protected Activity context = null;
		protected IList<Object> _Items = new List<Object>();
		public bool ActionMode = false;
		public SparseBooleanArray CheckedPosition = new SparseBooleanArray();

		private Action<string> _OpenAction;


		public Action<string> OpenAction
		{
			get
			{
				return _OpenAction;
			}
			set
			{
				_OpenAction = value;
			}
		}     

        private Action<string> _BuyAction;


        public Action<string> BuyAction
        {
            get
            {
                return _BuyAction;
            }
            set
            {
                _BuyAction = value;
            }
        }

        public DownloadGridAdapter(Activity context, IList<Object> items) : base()
		{
			this.context = context;
			_Items = items;
		}

		public override Object this[int position]
		{
			get { return _Items[position]; }
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override int Count
		{
			get { return _Items.Count; }
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			var item = _Items[position];

			/*var view = (convertView ??
                    context.LayoutInflater.Inflate(
					Resource.Layout.EdicolaGridItem,
                    parent,
                    false)) as LinearLayout;*/


			/*if(convertView != null)
			{
				convertView.Dispose();
				//convertView = null;
			}*/

			/*if(position == 0 && convertView != null && convertView.GetType() == typeof(DownloadGridItem))
				return convertView;*/

			View view;

			if(item.GetType() == typeof(Download))
			{
				view = this.GetViewThumb(position, convertView, parent);
			}
			else if(item.GetType() == typeof(DownDir))
			{
				if(((DownDir)item).Nome == null && ((DownDir)item).Path == null)
				{
					//return new RelativeLayout(context);
					var ly = context.LayoutInflater.Inflate(Resource.Layout.EdicolaFakeItem, parent, false);

					//ly.LayoutParameters = new ViewGroup.LayoutParams(GridView.LayoutParams.MatchParent, 90);

					ly.SetOnClickListener(null);
					ly.SetOnLongClickListener(null);
					ly.Clickable = false;

					return ly;
				}
				else
				{
					view = this.GetViewDir(position, convertView, parent);
				}
			}
			else
			{
				view = null;
			}

			return view;
		}

		private View GetViewThumb(int position, View convertView, ViewGroup parent)
		{
			var down = _Items[position] as Download;

			GridView grid = null;

			if(parent.GetType() == typeof(GridView))
			{
				grid = parent as GridView;
			}

			bool list = (grid != null && grid.NumColumns == 1);

			DownloadItem view = null;

            if(convertView != null && convertView is DownloadItem)
            {
                view = convertView as DownloadItem;

                if(view.DownloadUri == down.Uri)
                    return view;
                else
                    view = null;
            }

            if(view == null)
                view = new DownloadItem(this.context, list);

			view.Tag = position;

			view.DownloadUri = down.Uri;
			view.Stato = view.InitStato = down.Stato;

            view.Prezzo = down.IapPrezzo;

			//copertina
			if(down.Tipo == "pdf")
			{
				view.ImgCover.SetImageResource(Resource.Drawable.pdf_icon);
				view.ImgCover.SetScaleType(ImageView.ScaleType.FitCenter);
			}
			else
			{
				string key = down.ImageKey;
				var uri = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl + 
					"services/edicola_services.php?action=pubCover&zip=" + down.RelativePath + 
					"&img=copertina.jpg&key=" + key + 
					"&app=" + DataManager.Get<ISettingsManager>().Settings.AppId);

				view.ImgCover.Tag = uri.ToString();			
                //MBImageLoader.DisplayNetworkImage(uri, view.ImgCover, new PointF(280, 280));
                Koush.UrlImageViewHelper.SetUrlDrawable(view.ImgCover, uri.AbsoluteUri);
			}

			//titolo            
			view.Title = down.Titolo;

			//dettagli
			if(view.Details != null)
			{
				string autore = down.Autore;

				if(autore == "")
				{
					autore = context.GetString(Resource.String.pub_sconosciuto);
				}

				view.Details = autore;
			}

			//pulsante apri
			EventHandler openClick = delegate
			{
				if(_OpenAction != null)
				{
					OpenAction(down.GetLocalPath());
				}
			};

			view.OpenButton.Click -= openClick;
			view.OpenButton.Click += openClick;

			//download finito
			Action downFinish = () =>
			{
				down.Stato = DownloadStato.NoUpdate;
			};

			view.DownloadFinish -= downFinish;
			view.DownloadFinish += downFinish;

            //pulsante buy
            EventHandler buyClick = delegate
            {
                if (_BuyAction != null)
                {
                    _BuyAction(down.IapID);
                }
            };

            view.BuyButton.Click -= buyClick;
            view.BuyButton.Click += buyClick;

			view.ConfigureForDownload();


			/*if(grid != null && grid.NumColumns == 1)
			{
				view = new DownloadGridItem(this.context, doc, true); //context.LayoutInflater.Inflate(Resource.Layout.EdicolaListItem, parent, false);	

			}
			else
			{
				view = new DownloadGridItem(this.context, doc);

			}

			var btnOpen = view.FindViewById<ImageView>(Resource.Id.btnOpen);

			btnOpen.Click += delegate
			{
				if(_OpenAction != null)
				{
					OpenAction(doc.GetLocalPath());
				}
			};*/

			return view;
		}

		private View GetViewDir(int position, View convertView, ViewGroup parent)
		{
			var dir = _Items[position] as DownDir;

			GridView grid = null;

			if(parent.GetType() == typeof(GridView))
			{
				grid = parent as GridView;
			}

			View view = null;

			if(grid != null && grid.NumColumns == 1)
			{
				view = context.LayoutInflater.Inflate(Resource.Layout.DownloadListItem, parent, false);	
			}
			else
			{
				view = context.LayoutInflater.Inflate(Resource.Layout.DownloadDirItem, parent, false);			
			}

			var txtTitolo = view.FindViewById<TextView>(Resource.Id.downloadTxtTitolo);
			var txtDettagli = view.FindViewById<TextView>(Resource.Id.downloadTxtDettagli);
			var imgCover = view.FindViewById<ImageView>(Resource.Id.downloadImgCover);
			var btnDownload = view.FindViewById<ImageView>(Resource.Id.btnDownload);
            var btnBuy = view.FindViewById<TextView>(Resource.Id.btnBuy);
			/*var prgDownload = view.FindViewById<ProgressBar>(Resource.Id.downloadProgress);
			var overlay = view.FindViewById<RelativeLayout>(Resource.Id.downloadOverlay);*/

			//copertina
			imgCover.SetImageResource(Resource.Drawable.ic_folder);
			imgCover.SetScaleType(ImageView.ScaleType.CenterInside);
			imgCover.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

			//campo titolo
			txtTitolo.Tag = position;		            
			txtTitolo.SetText(dir.Nome, TextView.BufferType.Normal);

			//dettagli
			if(txtDettagli != null)
			{
				//txtDettagli.SetText(context.GetString(Resource.String.pub_cartella), TextView.BufferType.Normal);
				txtDettagli.Visibility = ViewStates.Gone;
			}

			//pulsante info
			if(btnDownload != null)
			{
				btnDownload.Visibility = ViewStates.Invisible;
			}

            //pulsante buy
            if (btnBuy != null)
            {
                btnBuy.Visibility = ViewStates.Gone;
            }

			return view;
		}
	}
}