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
using Android.Support.V4.App;
using Android.Graphics;

namespace InPublishing
{
	public class NoteFragment : BaseTitleFragment
	{
        private Pubblicazione _pubblicazione;
		private List<NotaUtente> _note = new List<NotaUtente>();
		private NoteManager _noteManager;
		private ListView _listView;

		private Action<NotaUtente> _noteItemClick;
		public Action<NotaUtente> NoteItemClick
		{
			get
			{
				return _noteItemClick;
			}
			set
			{
				_noteItemClick = value;
			}
		}   

        public NoteFragment(string title, Pubblicazione pub) : base(title)
		{
			_pubblicazione = pub;
		}

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Create your fragment here
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate(Resource.Layout.NoteList, container, false);

			_noteManager = new NoteManager(_pubblicazione);

			_listView = view.FindViewById<ListView>(Resource.Id.viewerPageList);

			_listView.ItemClick += (sender, e) => 
			{
				var nota = _note[e.Position];
				_noteItemClick(nota);
			};

			var btnSend = view.FindViewById<Button>(Resource.Id.btnSend);
			btnSend.SetText(GetString(Resource.String.pub_sendNotes), TextView.BufferType.Normal);

            btnSend.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
            btnSend.SetTextColor(Color.White);

			btnSend.Click += delegate
			{
				SendNotes();
			};

			PopulateTable();

			return view;
		}

		private void PopulateTable()
		{
			_note = _noteManager.GetNote();

			NoteAdapter adapter = new NoteAdapter(Activity, _note, _pubblicazione.Path);

			adapter.DeleteAction += DeleteAction;

			_listView.Adapter = adapter;
		}

		public void DeleteAction(int position)
		{
			var dialog = new AlertDialog.Builder(Activity);

			dialog.SetTitle(GetString(Resource.String.pub_note));
			dialog.SetMessage(GetString(Resource.String.edic_delete));

			dialog.SetNegativeButton(GetString(Resource.String.gen_cancel), delegate
			{
				return;
			});

			dialog.SetPositiveButton(GetString(Resource.String.gen_delete), delegate(object sender, DialogClickEventArgs e)
			{	
				var nota = _note[position];

				_noteManager.DeleteNota(nota.ID);

				PopulateTable();

			});

			dialog.Create();
			dialog.Show().SetDivider();
		}

		private void SendNotes()
		{
			string body = "";

			foreach (NotaUtente n in _noteManager.GetNote())
			{
				NotaUtente nota = n;

				body += nota.Titolo + "\n";
				body += nota.Testo + "\n";
				body += "-------------------------------------------------------------------------\n";
			}

			if (body == string.Empty)
			{
				return;
			}

			//apro l'app mail
			var email = new Intent(Intent.ActionSend);
			email.SetType("text/html");

			email.PutExtra(Android.Content.Intent.ExtraSubject, string.Format(GetString(Resource.String.pub_sendNotesFrom), Activity.ApplicationInfo.LoadLabel(Activity.PackageManager)));//string.Format("Sending notes from {0}".t(), ));
			email.PutExtra(Android.Content.Intent.ExtraText, body); 

			Activity.StartActivity(email);
		}
	}
}

