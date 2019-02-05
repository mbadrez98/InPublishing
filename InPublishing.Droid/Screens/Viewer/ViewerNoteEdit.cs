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
using System.Collections;
using Android.Graphics.Drawables;
using Android.Graphics;

namespace InPublishing
{
	public class ViewerNoteEdit : DialogFragment
	{
        private Pubblicazione _pubblicazione;
		private NotaUtente _nota;
		private EditText _txtNota;
		private NoteManager _noteManager;

        public ViewerNoteEdit(NotaUtente nota, Pubblicazione pub)
		{
			_pubblicazione = pub;
			_nota = nota;

			_noteManager = new NoteManager(_pubblicazione);
			_noteManager.LoadNota(_nota);
		}

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			//schermo intero
			//SetStyle((int)DialogFragmentStyle.Normal, Resource.Style.Theme_Blue);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			Dialog.Window.RequestFeature(WindowFeatures.NoTitle);

			View view = inflater.Inflate(Resource.Layout.NoteEdit, container);

			_txtNota = (EditText) view.FindViewById<EditText>(Resource.Id.txtNote);

			var btnOption = view.FindViewById<ImageButton>(Resource.Id.btnOption);

			btnOption.Click += (object sender, EventArgs e) => 
			{
				PopupMenu popup = new PopupMenu(Dialog.Context, btnOption);

				popup.Menu.Add(1, 1, 1, "Salva");
				popup.Menu.Add(1, 2, 2, "Elimina");

				popup.MenuItemClick += (object send, PopupMenu.MenuItemClickEventArgs ev) => 
				{
					if(ev.Item.ItemId == 1) //salva
					{
						_nota.Testo = _txtNota.Text;

						_noteManager.EditNota(_nota);

						this.Dismiss();
					}
					else if(ev.Item.ItemId == 2) //elimina
					{
						this.DeleteNote();
					}
				};

				popup.Show();
			};

			return view;
		}

		public override void OnStart()
		{
			base.OnStart();

			Rect frame = new Rect();
			Activity.Window.DecorView.GetWindowVisibleDisplayFrame(frame);

			WindowManagerLayoutParams lp = Dialog.Window.Attributes;

			Dialog.Window.SetGravity(GravityFlags.Bottom | GravityFlags.CenterHorizontal);

			lp.DimAmount = 0;
			lp.Width = Utility.dpToPx(this.Activity, frame.Right - 300); // 600;
			//lp.X = 25;
			//lp.Y = frame.Top;
			lp.Height = Utility.dpToPx(this.Activity, 150);

			Dialog.Window.Attributes = lp;

			Dialog.Window.AddFlags(WindowManagerFlags.DimBehind);

			_txtNota.Text = _nota.Testo;
			_txtNota.RequestFocus();

			Dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

			//Dialog.Window.SetLayout(240, 300);
			//Dialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
		}

		public override void OnDismiss(IDialogInterface dialog)
		{
			_nota.Testo = _txtNota.Text;

			_noteManager.EditNota(_nota);

			base.OnDismiss(dialog);
		}

		private void DeleteNote()
		{
			var dialog = new AlertDialog.Builder(Activity);

			dialog.SetTitle(GetString(Resource.String.pub_note));
			dialog.SetMessage (GetString (Resource.String.edic_delete));
			dialog.SetNegativeButton("Annulla", delegate
			{
				return;
			});

			dialog.SetPositiveButton("Elimina", delegate(object sender, DialogClickEventArgs e)
			{			
				this.Dismiss();
				_noteManager.DeleteNota(_nota.ID);
				_nota.Testo = "";
				_txtNota.Text = "";				
			});

			dialog.Create();
			dialog.Show().SetDivider();
		}
	}
}

