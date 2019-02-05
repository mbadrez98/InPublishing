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
using Android.Graphics;
using System.IO;

namespace InPublishing
{
	public class GifView : View
	{
		private Movie _movie;
		private string _path;
		private long _moviestart;

		public GifView(Context context, string path) : base (context)
		{
			_path = path;
			FileStream stream = File.Open(_path, FileMode.Open);
			_movie = Movie.DecodeStream(stream);
		}

		protected override void OnDraw(Canvas canvas) 
		{
			canvas.DrawColor(Color.Green);
			base.OnDraw(canvas);
			long now = SystemClock.UptimeMillis();

			if (_moviestart == 0) 
			{
				_moviestart = now;
			}

			//int relTime = (int)((now - _moviestart) % _movie.Duration());
			//_movie.SetTime(relTime);
			_movie.Draw(canvas, 10, 10);

			this.Invalidate();
		}

	}
}