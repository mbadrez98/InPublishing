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
	public class SearchDownAdapter : BaseAdapter<Download>
	{
		protected Activity _context = null;
		private List<Download> _Items;

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
     
        public SearchDownAdapter(Activity context, List<Download> items)
		{
			_context = context;
			_Items = items;
		}

		public override Download this[int position]
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

            DownloadItem view = null;

            if(convertView != null && convertView is DownloadItem)
            {
                view = convertView as DownloadItem;

                if(view.DownloadUri == item.Uri)
                    return view;
            }

            if(view == null)
                view = new DownloadItem(_context, true);

			//var view = new DownloadItem(_context, true);

			view.Tag = position;

			view.DownloadUri = item.Uri;
			view.Stato = view.InitStato = item.Stato;

			//copertina
			if(item.Tipo == "pdf")
			{
				view.ImgCover.SetImageResource(Resource.Drawable.pdf_icon);
				view.ImgCover.SetScaleType(ImageView.ScaleType.FitCenter);
			}
			else
			{
				string key = item.ImageKey;
				var uri = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl + 
					"services/edicola_services.php?action=pubCover&zip=" + item.RelativePath + 
					"&img=copertina.jpg&key=" + key + 
					"&app=" + DataManager.Get<ISettingsManager>().Settings.AppId);

				view.ImgCover.Tag = uri.ToString();			

				//Koush.UrlImageViewHelper.SetUrlDrawable (view.ImgCover, uri.AbsoluteUri);

                MBImageLoader.DisplayNetworkImage(uri, view.ImgCover, new PointF(280, 280));

            }

			//titolo            
			view.Title = item.Titolo;

			//dettagli
			if(view.Details != null)
			{
				string autore = item.Autore;

				if(autore == "")
				{
					autore = _context.GetString(Resource.String.pub_sconosciuto);
				}

				view.Details = autore;
			}

            //pulsante apri
            EventHandler openClick = delegate {
                if(_OpenAction != null)
                {
                    OpenAction(item.GetLocalPath());
                }
            };

            view.OpenButton.Click -= openClick;
            view.OpenButton.Click += openClick;

			//download finito
			Action downFinish = () =>
			{
				item.Stato = DownloadStato.NoUpdate;
			};

			view.DownloadFinish -= downFinish;
			view.DownloadFinish += downFinish;

			view.ConfigureForDownload();		

			return view;
		}
	}
}

