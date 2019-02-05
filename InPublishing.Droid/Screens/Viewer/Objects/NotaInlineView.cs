using System;
using Android.Widget;
using Android.Content;
using Android.Views;
using Android.Graphics;
using Android.Views.InputMethods;
using Android.InputMethodServices;

namespace InPublishing
{
	public class NotaInlineView : RelativeLayout
	{
		NotaUtente _nota;
		EditText _txtNota;
		ViewerScreen _docView;

		public NotaInlineView(Context context, NotaUtente nota, ViewerScreen docView) : base(context)
		{
			_docView = docView;
			_nota = nota;

			View.Inflate(this.Context, Resource.Layout.NotaInlineView, this);

            NoteManager noteMan = new NoteManager(docView.Pubblicazione);

			noteMan.LoadNota(nota);

			_txtNota = FindViewById<EditText>(Resource.Id.txtNota);
			//txtNota.SetBackgroundColor(Color.Red);
			_txtNota.SetText(nota.Testo, TextView.BufferType.Normal);

			SetTextStyle();

			_docView.ReaderView.OnSingleTap += OnParentTap;

			_txtNota.TextChanged += delegate
			{
				nota.Testo = _txtNota.Text;
				noteMan.EditNota(nota);
			};
		}

		private void SetTextStyle()
		{
			//colore font
			if(_nota.FontColor != "")
			{
				_txtNota.SetTextColor(Color.Transparent.FromHex(_nota.FontColor)); 
			}

			//dimensione font
			if(_nota.FontSize > 0)
			{
				_txtNota.TextSize = _nota.FontSize * 0.6f; //conversione per android
			}

			//stile font
			Typeface fontType;
			TypefaceStyle fontStyle;

			switch(_nota.FontStyle.ToUpper())
			{
				case "B":
					fontStyle = TypefaceStyle.Bold;
					break;
				case "I":
					fontStyle = TypefaceStyle.Italic;
					break;
				case "BI":
					fontStyle = TypefaceStyle.BoldItalic;
					break;
				default:
					fontStyle = TypefaceStyle.Normal;
					break;
			}

			switch(_nota.FontType.ToUpper())
			{
				case "B":
					fontType = Typeface.SansSerif;
					break;
				case "G":
					fontType = Typeface.Serif;
					break;
				default:
					fontType = Typeface.Default;
					break;
			}

			_txtNota.SetTypeface(fontType, fontStyle);
		}

		private void OnParentTap()
		{
			_txtNota.ClearFocus();
			var inputManager = _docView.GetSystemService(Context.InputMethodService) as InputMethodManager;
			inputManager.HideSoftInputFromWindow(_txtNota.WindowToken, HideSoftInputFlags.None);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			_docView.ReaderView.OnSingleTap -= OnParentTap;
		}
	}
}