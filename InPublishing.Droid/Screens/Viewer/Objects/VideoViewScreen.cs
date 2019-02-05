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
using Newtonsoft.Json;
using Android.Graphics;

namespace InPublishing
{
	[Activity (Label = "ZoomViewScreen", Theme = "@style/Blue.NoActionBar")]	
	public class VideoViewScreen : BaseModalScreen
	{
		private Video _video;
		private string _basePath;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			this.Window.AddFlags(WindowManagerFlags.Fullscreen);

			_video = JsonConvert.DeserializeObject<Video>(Intent.GetStringExtra("video"));
			_basePath = Intent.GetStringExtra("path");

			Android.Widget.VideoView videoView = new Android.Widget.VideoView(this);

			ViewGroup.LayoutParams param = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

			videoView.LayoutParameters = param;
			//var uri = Android.Net.Uri.Parse(System.IO.Path.Combine(path, _video.Link));

			if(_video.Link.StartsWith("http:"))
			{

				videoView.SetVideoURI(Android.Net.Uri.Parse(_video.Link));
				//_videoView.SetBackgroundColor(Color.Fuchsia);
			}
			else
			{
				videoView.SetVideoPath(System.IO.Path.Combine(_basePath, _video.Link));
			}

			//videoView.SetBackgroundColor(Color.Black);
			_contentView.SetBackgroundColor(Color.Black);

			MediaController mc = new MediaController(this);
			videoView.SetMediaController(mc);
			mc.RequestFocus();

			_contentView.AddView(videoView);

			videoView.Start();
		}
	}
}

