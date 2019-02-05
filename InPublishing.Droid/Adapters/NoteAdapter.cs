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
	public class NoteAdapter : BaseAdapter<NotaUtente>
	{
		protected Activity _context = null;
		private List<NotaUtente> _Items;

		public Action<int> DeleteAction;        

		public NoteAdapter(Activity context, List<NotaUtente> note, string basePath)
		{
			_Items = note;
			_context = context;
		}

		public override NotaUtente this[int position]
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

			View view = _context.LayoutInflater.Inflate(Resource.Layout.NoteItem, parent, false);

			TextView txtTitle = view.FindViewById<TextView>(Resource.Id.txtTitle);
			TextView txtPage = view.FindViewById<TextView>(Resource.Id.txtPage);
			TextView txtContent= view.FindViewById<TextView>(Resource.Id.txtTesto);
			var btnDelete = view.FindViewById<ImageView>(Resource.Id.btnDelete);

			txtTitle.Text = item.Titolo == "" ? item.ID : item.Titolo;

            string page = item.Pagina.ToString();

            if(item.PaginaAbs != "")
            {
                page = item.PaginaAbs.ToString();
            }

			txtTitle.Text += " " + page;

			txtPage.Text = page;

			txtContent.Text = item.Testo;

			btnDelete.Click += (sender, e) => 
			{
				DeleteAction(position);
			};

			return view;
		}
	}
}

