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
using Newtonsoft.Json;
using Android.Util;
using System.IO;

namespace InPublishing
{
    class VideoView : RelativeLayout
    {
        private Video _video;
        private string _basePath;
        private Android.Widget.VideoView _videoView;
        private ImageButton _btnFull;
        private bool _isReady = false;
        ViewerScreen _docView;

        public Action OnStart;
        public Action OnFinish;

        public VideoView(Context context, Video video, string path, ViewerScreen docView) : base(context)
        {
            _video = video;
            _basePath = path;
            _docView = docView;
            //this.SetBackgroundColor(Color.Aqua);

            View.Inflate(this.Context, Resource.Layout.VideoView, this);



            try
            {
                _videoView = FindViewById<Android.Widget.VideoView>(Resource.Id.videoView);
                _btnFull = FindViewById<ImageButton>(Resource.Id.btnFull);

                if (video.Link != "")
                {
                    if (!File.Exists(System.IO.Path.Combine(_basePath, _video.Link)))
                        return;

                    _videoView.SetVideoPath(System.IO.Path.Combine(_basePath, _video.Link));
                }
                else if (_video.UrlStream != "")
                {
                    _videoView.SetVideoURI(Android.Net.Uri.Parse(_video.UrlStream));
                }

                /*MediaController mc = new MediaController(context);
                mc.SetMediaPlayer(_videoView);
                mc.SetAnchorView(_videoView);

                _videoView.SetMediaController(mc);*/
                _videoView.RequestFocus();
            }
            catch (Exception ex)
            {
                Utils.WriteLog("Errore video", ex.Message);
                return;
            }

            _videoView.Error += (sender, e) =>
            {
                return;
            };

            //playstopclick
            if (this._video.PlayStopClick)
            {
                this.Click += (sender, e) =>
               {
                   this.PlayStop();
               };               
            }

            if (_video.Fullscreen)
            {
                _btnFull.Click += (sender, e) =>
                {
                    Intent i = new Intent();
                    i.SetClass(this.Context, typeof(VideoViewScreen));

                    i.PutExtra("path", _basePath);
                    i.PutExtra("video", JsonConvert.SerializeObject(_video));
                    //ActivitiesBringe.SetObject(zoom);
                    this.Stop();
                    docView.StartActivity(i);
                };
            }
            else
            {
                _btnFull.Visibility = ViewStates.Invisible;
            }

            //autoplay
            if (this._video.Autoplay)
            {
                //_videoView.Prepared -= Autoplay;
                //_videoView.Prepared += Autoplay;
                _videoView.Prepared += (sender, e) =>
                {
                    _isReady = true;
                };

                if (_video.Delay > 0)
                {
                    this.Hide();
                    _isReady = true;
                }
            }
            else
            {
                this.Hide();
            }

            //loop
            if (_video.Loop)
            {
                _videoView.Completion += (sender, e) =>
                {
                    _videoView.Start();
                };
            }

            //finish
            _videoView.Completion += (sender, e) =>
                {
                    if (OnFinish != null)
                        OnFinish();
                };
        }

        private void Autoplay(object sender, EventArgs e)
        {
            _isReady = true;
            this.PostDelayed(this.Play, _video.Delay);
        }

        public void Autoplay()
        {
            if (_videoView == null || !this._video.Autoplay)
                return;

            if (_isReady)
            {
                this.PostDelayed(this.Play, _video.Delay);
            }
            else
            {
                _videoView.Prepared -= Autoplay;
                _videoView.Prepared += Autoplay;
            }
        }

        public void Play()
        {
            try
            {
                if (_videoView != null)
                {
                    this.Show();

                    _videoView.SeekTo(0);
                    _videoView.Start();
                    //_videoView.RequestFocus();

                    if (OnStart != null)
                    {
                        OnStart();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("VideoView(Play)", ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                if (_videoView != null)
                {
                    _videoView.Pause();
                    _videoView.SeekTo(0);
                    this.Hide();
                    //_videoView.SetBackgroundDrawable(null);
                    //_videoView.ClearFocus();
                }
            }
            catch (Exception ex)
            {
                Log.Error("VideoView(Stop)", ex.Message);
            }
        }

        public void Pause()
        {
            try
            {
                if (_videoView != null)
                {
                    _videoView.Pause();
                }
            }
            catch (Exception ex)
            {
                Log.Error("VideoView(Pause)", ex.Message);
            }
        }

        public void PlayStop()
        {
            if (_videoView.IsPlaying)
            {
                this.Stop();
                AnalyticsService.SendEvent(_docView.Pubblicazione.Titolo, AnalyticsEventAction.VideoStop, _video.AnalyticsName);
            }
            else
            {
                this.Play();
                AnalyticsService.SendEvent(_docView.Pubblicazione.Titolo, AnalyticsEventAction.VideoPlay, _video.AnalyticsName);
            }
        }

        private void Hide()
        {
            _videoView.SetBackgroundDrawable(null);
            _videoView.SetBackgroundColor(Color.Transparent);
            _videoView.Visibility = ViewStates.Invisible;

            if (_btnFull != null)
            {
                _btnFull.Visibility = ViewStates.Invisible;
            }
        }

        private void Show()
        {
            _videoView.Visibility = ViewStates.Visible;

            if (_video.Fullscreen)
            {
                _btnFull.Visibility = ViewStates.Visible;
            }
        }

        public void DisposeMedia()
        {
            _videoView.Pause();
            //_player.Release();
            _videoView.Dispose();
            _videoView = null;

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