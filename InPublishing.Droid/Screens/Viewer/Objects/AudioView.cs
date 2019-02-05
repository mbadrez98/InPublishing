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
using Android.Media;
using Android.Graphics;
using System.IO;

namespace InPublishing
{
	public class AudioView : FrameLayout
	{
		private Audio _audio;
		private string _basePath;
		private Android.Widget.VideoView _audioView;
		private bool _isReady = false;
        private ViewerScreen _docView;

        public AudioView(Context context, Audio audio, string path, ViewerScreen docView) : base (context)
		{
			_audio = audio;
			_basePath = path;
            _docView = docView;
			_audioView = new Android.Widget.VideoView(context);

			ViewGroup.LayoutParams param = new ViewGroup.LayoutParams(1, 1);
			_audioView.LayoutParameters = param;

            if (!File.Exists(System.IO.Path.Combine(_basePath, _audio.Link)))
                return;
            
			_audioView.SetVideoPath(System.IO.Path.Combine(_basePath, _audio.Link));

			var time = _audioView.CurrentPosition;

			this.AddView(_audioView);

			_audioView.RequestFocus();

			//playstopclick
			if(_audio.PlayStopClick)
			{
				this.Click += (sender, e) => 
				{
					this.PlayStop();
				};
			}

			//autoplay
			if(this._audio.Autoplay)
			{
				//_audioView.Prepared -= Autoplay;
				//_audioView.Prepared += Autoplay;
				_audioView.Prepared += (sender, e) => 
				{
					_isReady = true;
				};
			}

			//loop
			if(_audio.Loop)
			{
				_audioView.Completion += (sender, e) => 
				{
					_audioView.Start();
				};
			}
		}

		private void Autoplay(object sender, EventArgs e)
		{
			_isReady = true;
			this.PostDelayed(this.Play, _audio.Delay);
		}

		public void Autoplay()
		{
			if(_audioView == null || !_audio.Autoplay)
				return;

			if(_isReady)
			{
				this.PostDelayed(this.Play, _audio.Delay);
			}
			else
			{
				_audioView.Prepared -= Autoplay;
				_audioView.Prepared += Autoplay;
			}
		}

		public void Play()
		{
            try
            {
                if(_audioView != null)
    			{
    				_audioView.Start();
    			}
            }
            catch(Exception ex)
            {
                Log.Error("AudioView(Play)", ex.Message);
            }
		}

		public void Stop()
		{
            try
            {
    			if(_audioView != null)
    			{
    				_audioView.Pause ();
    				_audioView.SeekTo(0);
    			}
            }
            catch(Exception ex)
            {
                Log.Error("AudioView(Stop)", ex.Message);
            }
		}

		public void PlayStop()
		{
			if(!_audioView.IsPlaying)
			{
				this.Play();
                AnalyticsService.SendEvent(_docView.Pubblicazione.Titolo, AnalyticsEventAction.AudioPlay, _audio.AnalyticsName);
			}
			else
			{
				this.Stop();
                AnalyticsService.SendEvent(_docView.Pubblicazione.Titolo, AnalyticsEventAction.AudioStop, _audio.AnalyticsName);
			}
		}

		/*public override bool OnKeyDown(Keycode key, KeyEvent e) 
		{
			if ((key == Keycode.Home) || (key == Keycode.Back)) 
			{
				this.Stop();
				return true;
			}

			return base.OnKeyDown(key, e);
		}*/

		/*public override void OnWindowFocusChanged(bool hasWindowFocus)
		{
			if(hasWindowFocus)
			{
			}
			else
			{
				this.Stop();
			}
		}*/

		public void DisposeMedia()
		{
			_audioView.Pause();
			//_player.Release();
			_audioView.Dispose();
			_audioView = null;

			/*_docView.RunOnUiThread(() => 
            {
				_player.Pause();
				//_player.Release();
				//_player.Dispose();
				//_player = null;
			});*/
		}
	}
}

