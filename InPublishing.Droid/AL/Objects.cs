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
using Android.Webkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Android.Text;
using System.Net;
using System.Collections.Specialized;
using System.Globalization;
using Android.Util;
using System.Threading;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Xamarin.Facebook;
using Xamarin.Facebook.Share.Widget;
using Xamarin.Facebook.Share.Model;
using Android;

namespace InPublishing
{
	class Objects
	{
        private static RelativeLayout GetObjectByReference(Dictionary<string, RelativeLayout> objViewList, string reference, ViewerScreen docView, ScrollerView scrollView = null)
        {
            try
            {
                if(objViewList.ContainsKey(reference))
                {
                    return objViewList[reference] as RelativeLayout;
                }

                if(docView != null && scrollView != null)
                {
                    MuPageView pageView = (MuPageView)docView.ReaderView.DisplayedView;

                    if(pageView.Oggetti.ContainsKey(reference))
                    {
                        return pageView.Oggetti[reference] as RelativeLayout;
                    }
                }

                return null;
            }
            catch(Exception ex)
            {
                Log.Info("Objects - GetObjectByReference", ex.Message);

                return null;
            }
        }

		public static Dictionary<string, RelativeLayout> CreateObjects(List<Oggetto> oggetti, string artPath, ViewerScreen docView, float scale = 1, ScrollerView scrollView = null)
		{
			Dictionary<string, RelativeLayout> objViewList = new Dictionary<string, RelativeLayout>();

			for (int j=0; j<oggetti.Count; j++)
			{
				try
				{
					Oggetto obj = oggetti[j];

					if (objViewList.ContainsKey(obj.Nome))
					{
						continue;
					}					

					RelativeLayout.LayoutParams param = new RelativeLayout.LayoutParams((int)Math.Round(obj.Width * scale), (int)Math.Round(obj.Height * scale));
					param.LeftMargin = (int)Math.Round(obj.X * scale);
					param.TopMargin = (int)Math.Round(obj.Y * scale);

					/*if(param.LeftMargin < 0)
						param.LeftMargin = 0;

					if(param.TopMargin < 0)
						param.TopMargin = 0;*/

					//vista oggetto e pulsante per azioni
					Rect oFrame = new Rect(param.LeftMargin, param.TopMargin, param.LeftMargin + param.Width, param.TopMargin + param.Height);
					ObjView objView = new ObjView(Application.Context, oFrame);
					//objView.SetBackgroundColor(Color.Green);
					objView.LayoutParameters = param;

					float density = docView.Resources.DisplayMetrics.Density;

					RelativeLayout.LayoutParams elParam = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent);
					Button actionButton = new Button(Application.Context); //new UIButton(new RectangleF(0, 0, obj.Width * scale, obj.Height * scale));

                    //actionButton.Opaque = false;
                    actionButton.LayoutParameters = elParam;
                    actionButton.Tag = "actionButton";

					actionButton.Enabled = false;
					actionButton.Clickable = false;
					actionButton.SetBackgroundResource(0);


                    /*actionButton.Click += (sender, e) => 
                    {
                            actionButton.SetBackgroundResource(Resource.Drawable.buttonBorder);
                        
                            GradientDrawable gd = (GradientDrawable)actionButton.Background;
                            gd.SetStroke(1, Color.Red);
                        
                        foreach(var o in objViewList)
                        {
                            for(int i = 0; i < o.Value.ChildCount; i++)
                            {
                                if(o.Value.GetChildAt(i).GetType() == typeof(Button))
                                {
                                    var btn = o.Value.GetChildAt(i);

                                    if(btn != actionButton)
                                    {                                    
                                        btn.SetBackgroundResource(0);
                                            btn.SetBackgroundColor((Color.Transparent));
                                        
                                    }
                                }
                            }   
                        }        
                    };	*/				

					//inserimento elementi dell'oggetto
					foreach (var el in obj.Elementi)
					{
						//immagine
						if(el.Key == "image")
						{
							Image img = (Image)el.Value;

							RectF frame = new RectF(0, 0, obj.Width * scale, obj.Height * scale);

                            ImageStateView imgView = new ImageStateView(Application.Context, artPath, img, frame);

							imgView.Tag = "image";
							objView.AddView(imgView, elParam);

                            if(img.LinkPressed != string.Empty)
                            {
                                actionButton.Click += (sender, e) => 
                                    {
                                        imgView.SetState(1);

                                        foreach(var o in objViewList)
                                        {
                                            for(int i = 0; i < o.Value.ChildCount; i++)
                                            {
                                                if(o.Value.GetChildAt(i).GetType() == typeof(ImageStateView))
                                                {
                                                    var item = (ImageStateView)o.Value.GetChildAt(i);

                                                    if(item != imgView)
                                                    {                                    
                                                        item.SetState(0);
                                                    }
                                                }
                                            }   
                                        }        
                                    };
                            }
						                                       
						}
						//scroller
						else if(el.Key == "scroller")
						{
							Scroller scrl = (Scroller)el.Value;

                            RectF frame = new RectF(param.LeftMargin, param.TopMargin, param.Width, param.Height);

							ScrollerView scrlView = new ScrollerView(Application.Context, scrl, artPath, docView, frame, scale);

							objView.AddView(scrlView, elParam);
							//objView.SetBackgroundColor(Color.Blue);

							if(scrl.PopUp && !scrl.Visible)
							{
								objView.Visibility = ViewStates.Invisible;
							}
						}

						//multistato
						else if(el.Key == "multistato")
						{
							RectF frame = new RectF(0, 0, param.Width, param.Height);

							MultistatoView multi = new MultistatoView(Application.Context, (Multistato)el.Value, artPath, docView, frame);

							objView.AddView(multi);
						}                    
						//navigazione pagine
						else if(el.Key == "pageNav")
						{     
							if(actionButton != null)
							{
								actionButton.Click += (sender, e) =>
								{   
									PageNav nav = (PageNav)el.Value;

                                    AnalyticsEventAction analyticsAction;

									if(nav.Next)
                                    {
                                        docView.NextPage();
                                        analyticsAction = AnalyticsEventAction.PageNext;
                                    }
                                    else if(nav.Prev)
                                    {
                                        docView.PreviousPage();
                                        analyticsAction = AnalyticsEventAction.PagePrev;
                                    }
                                    else if(nav.Back)
                                    {
                                        docView.BackPage();
                                        analyticsAction = AnalyticsEventAction.PageBack;
                                    }
                                    else if(nav.First)
                                    {
                                        docView.FirstPage();
                                        analyticsAction = AnalyticsEventAction.PageFirst;
                                    }
                                    else if(nav.Last)
                                    {
                                        docView.LastPage();
                                        analyticsAction = AnalyticsEventAction.PageLast;
                                    }
                                    else
                                    {
                                        docView.GoToPage(nav.Page.ToString(), nav.IdDocumento);
                                        analyticsAction = AnalyticsEventAction.Page;
                                    }

                                    AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, analyticsAction, obj.AnalyticsName);
								}; 

								actionButton.Enabled = true;
								actionButton.Clickable = true;
							}
						}                    
						//navigazione stati
						else if(el.Key == "stateNav")
						{  
							//actionButton.BackgroundColor = UIColor.Green;
							StateNav nav = (StateNav)el.Value;

							if(nav.Next || nav.Prev)
							{
								if(actionButton != null)
								{
									actionButton.Click += (sender, e) =>
									{
										try
										{
                                            RelativeLayout o = Objects.GetObjectByReference(objViewList, nav.Reference, docView, scrollView);  
                                            if(o != null)
                                            {
												MultistatoView mv = null;

												for(int i = 0; i < o.ChildCount; i++)
												{
													if(o.GetChildAt(i).GetType() == typeof(MultistatoView))
													{
														mv = o.GetChildAt(i) as MultistatoView;
													}
												}		                                   

												if(mv != null)
												{
													if(nav.Next)
                                                    {
                                                        mv.NextState();
                                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoNext, obj.AnalyticsName);
                                                    }
                                                    else
                                                    {
                                                        mv.PrevState();
                                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoPrev, obj.AnalyticsName);
                                                    }
												}
											}
										}
										catch(Exception ex)
										{
											Utils.WriteLog("Errore 'nstate/pstate'", ex.Message);
										}
									};
								}
							}
							else
							{
								if(actionButton != null)
								{
									actionButton.Click += (sender, e) =>
									{                           
										try
										{
                                            RelativeLayout o = Objects.GetObjectByReference(objViewList, nav.Reference, docView, scrollView);  
                                            if(o != null)
											{
												MultistatoView mv = null;
												

												for(int i = 0; i < o.ChildCount; i++)
												{
													if(o.GetChildAt(i).GetType() == typeof(MultistatoView))
													{
														mv = o.GetChildAt(i) as MultistatoView;
													}
												}										                                 

												if(mv != null)
												{
													mv.GoToState(nav.State);
                                                    AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoState, obj.AnalyticsName);
												}
											}                               
										}
										catch(Exception ex)
										{
											Utils.WriteLog("Errore 'sstate'", ex.Message);
										}

									};
								}
							}

							if(actionButton != null)
							{
								actionButton.Enabled = true;
								actionButton.Clickable = true;
							}
						}
						//zoom specifico
						else if(el.Key == "zoomSpecifico")
						{
							ZoomSpecifico zoom = (ZoomSpecifico)el.Value;
							if(actionButton != null)
							{
								actionButton.Click += (sender, e) =>
								{
									Intent i = new Intent();
									//i.SetClass(Application.Context, typeof(ZoomViewScreen));
									i.SetClass(Application.Context, typeof(ZoomViewScreen));

									i.PutExtra("path", artPath);
									i.PutExtra("zoom", JsonConvert.SerializeObject(zoom));

									//ActivitiesBringe.SetObject(zoom);

									docView.StartActivity(i);

									docView.OverridePendingTransition(Resource.Animation.fade_in, Resource.Animation.fade_out);

									actionButton.Enabled = false;

									ThreadPool.QueueUserWorkItem(state =>
									{
										Thread.Sleep(2000); //wait 2 sec
										docView.RunOnUiThread(() => actionButton.Enabled = true); //fade out the view
									});

                                    AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.ImageZoom, obj.AnalyticsName);
								};

								actionButton.Enabled = true;
								actionButton.Clickable = true;
							}
						}
						//video
						else if(el.Key == "video")
						{
							//INP_VideoView videoView = new INP_VideoView(Application.Context, (Video)el.Value, artPath);
							VideoView videoView = new VideoView(Application.Context, (Video)el.Value, artPath, docView);
							objView.AddView(videoView, elParam);
						}
						//audio
						else if(el.Key == "audio")
						{                       
                            AudioView audioView = new AudioView(Application.Context, (Audio)el.Value, artPath, docView);

							objView.AddView(audioView, elParam);
						}
						//audio e video control
						else if(el.Key == "control")
						{                       
							Control ctrl = (Control)el.Value;

							if(actionButton != null)
							{
								actionButton.Click += (sender, e) =>
								{                            
									try
									{
                                        RelativeLayout o = Objects.GetObjectByReference(objViewList, ctrl.Reference, docView, scrollView);  
                                        if(o != null)
                                        {                              
                                            if(ctrl.Tipo == "video")
											{
												VideoView vv = null;

												for(int i = 0; i < o.ChildCount; i++)
												{
													if(o.GetChildAt(i).GetType() == typeof(VideoView))
													{
														vv = o.GetChildAt(i) as VideoView;
													}
												}										                                 

												if(vv != null)
												{
													/*if(ctrl.Azione == "play")
													{
														vv.Play();
                                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.VideoPlay, obj.AnalyticsName);
													}
													else
													{
														vv.Stop();
                                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.VideoStop, obj.AnalyticsName);
													}*/

                                                    switch(ctrl.Azione)
                                                    {
                                                        case "play":
                                                            vv.Play();
                                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.VideoPlay, obj.AnalyticsName);
                                                            break;
                                                        case "stop":
                                                            vv.Stop();
                                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.VideoStop, obj.AnalyticsName);
                                                            break;
                                                        /*case "pause":
                                                            vv.Pause();
                                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.VideoPause, obj.AnalyticsName);
                                                            break;*/
                                                    }
												} 
											}
											else
											{
												AudioView av = null;

												for(int i = 0; i < o.ChildCount; i++)
												{
													if(o.GetChildAt(i).GetType() == typeof(AudioView))
													{
														av = o.GetChildAt(i) as AudioView;
													}
												}										                                 

												if(av != null)
												{
													/*if(ctrl.Azione == "play")
													{
														av.Play();
                                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.AudioPlay, obj.AnalyticsName);
													}
													else
													{
														av.Stop();
                                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.AudioStop, obj.AnalyticsName);
													}*/

                                                    switch (ctrl.Azione)
                                                    {
                                                        case "play":
                                                            av.Play();
                                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.AudioPlay, obj.AnalyticsName);
                                                            break;
                                                        case "stop":
                                                            av.Stop();
                                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.AudioStop, obj.AnalyticsName);
                                                            break;
                                                            /*case "pause":
                                                                vv.Pause();
                                                                AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.AudioPause, obj.AnalyticsName);
                                                                break;*/
                                                    }
												} 
											}
										}                    
									}
									catch(Exception ex)
									{
										Utils.WriteLog("Errore 'videoControl'", ex.Message);
									} 
								};

								actionButton.Enabled = true;
								actionButton.Clickable = true;
							}
						}
						//browser
						else if(el.Key == "browser")
						{ 
							Browser browser = (Browser)el.Value;

							if(browser.Fullscreen)
							{

								/*	WebView brView = new WebView(Application.Context);

									
								actionButton.TouchUpInside += (sender, e) => 
								{
									docView.MB_PresentViewController(brView, true);
								}; 

								if(browser.Autostart)
								{
									docView.MB_PresentViewController(brView, true);
								}

								actionButton.UserInteractionEnabled = true;
								actionButton.Opaque = true;*/
							}
							else
							{
								/*if(browser.Tipo == "gif")
								{
									GifView gifView = new GifView(Application.Context, System.IO.Path.GetFullPath(System.IO.Path.Combine(artPath, System.Web.HttpUtility.UrlDecode(browser.UrlStream))));
									objView.AddView(gifView, elParam);
								}
								else*/
								{
									docView.RunOnUiThread(() => {
                                        BrowserView bView = new BrowserView(docView, browser, artPath, docView);
										objView.AddView(bView, elParam);
									});

								}
							}
						}
						//collegamento
						else if(el.Key == "hlink")
						{ 
							try
							{
								Collegamento coll = (Collegamento)el.Value;
								if(actionButton != null)
								{
									actionButton.Click += (sender, e) =>
									{
										Uri uriTmp = null;

										System.IO.FileInfo file = null; 

										if(coll.Tipo == "htm" || coll.Tipo == "pdf" || coll.Tipo == "mp4")
										{
											uriTmp = new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(artPath, System.Web.HttpUtility.UrlDecode(coll.Link))));

											file = new System.IO.FileInfo(uriTmp.LocalPath);
										}
										else
										{
                                            bool result =  Uri.TryCreate(coll.Link, UriKind.Absolute, out uriTmp);

                                            if(!result)
                                            {
                                                return;
                                            }
										}

										//var url = new  NSUrl(uriTmp.AbsoluteUri);

										Android.Net.Uri uri = Android.Net.Uri.Parse(uriTmp.AbsoluteUri);
										/*if(file != null && file.Extension.ToLower() == ".pdf")
										{
											Intent intent = new Intent(Intent.ActionView);
											intent.SetDataAndType(uri, "application/pdf");
											intent.SetFlags(ActivityFlags.ClearTop);
											docView.StartActivity(intent);

                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.ResourceOpen, obj.AnalyticsName);
										}
										else*/ if(uri.Path != null && uri.Path.EndsWith(".mp4"))
										{
											Video video = new Video();
											video.Autoplay = true;
											video.Delay = 0;
											video.Fullscreen = false;
											video.Link = coll.Link;
											video.PlayStopClick = false;

											Intent i = new Intent();
											i.SetClass(Application.Context, typeof(VideoViewScreen));

											i.PutExtra("path", artPath);
											i.PutExtra("video", JsonConvert.SerializeObject(video));
											//ActivitiesBringe.SetObject(zoom);

											docView.StartActivity(i);

                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.ResourceOpen, obj.AnalyticsName);
										}
										else if((uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "file") && !coll.Esterno)
										{
											Intent i = new Intent();
											i.SetClass(Application.Context, typeof(BrowserViewScreen));
											i.PutExtra("url", coll.Link);
											i.PutExtra("tipo", coll.Tipo);
											i.PutExtra("pageFit", coll.PageFit);
											i.PutExtra("basePath", artPath);
											docView.StartActivity(i);

                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.ResourceOpen, obj.AnalyticsName);
										}
										else if(uri.Scheme == "mailto")
										{
											//apro l'app mail
											try
											{
												string[] parts = uri.EncodedSchemeSpecificPart.Split('?');

												if(parts.Length > 0 /*&& parts[0].Contains('@')*/)
												{
													var email = new Intent(Intent.ActionSend);
													email.SetType("text/html");
													email.PutExtra(Android.Content.Intent.ExtraEmail, new string[] { parts[0] });       

													if(parts.Length > 1)
													{
														if(uri.Query.Length > 0)
														{
															string query = uri.EncodedQuery.Trim('?');
															string[] pieces = query.Split('&');

															foreach(string piece in pieces)
															{
																string[] val = piece.Split('=');

																if(val.Length == 2)
																{
																	if(val[0] == "subject")
																	{
																		email.PutExtra(Android.Content.Intent.ExtraSubject, System.Web.HttpUtility.UrlDecode(val[1]));
																	}
																	else if(val[0] == "body")
																	{
																		email.PutExtra(Android.Content.Intent.ExtraText, Html.FromHtml(System.Web.HttpUtility.UrlDecode(val[1]).Replace("\n", "<br />")));
																	}
																}
															}
														}
													}
													else
													{
														email.PutExtra(Android.Content.Intent.ExtraSubject, "");
														email.PutExtra(Android.Content.Intent.ExtraText, "");
													}

													docView.StartActivity(email);

													AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.SystemAction, obj.AnalyticsName);
												}
											}
											catch(ActivityNotFoundException exc)
											{
												//new UIAlertView("Invio Mail", "Invio mail non configurato", null, "OK", null).Show();
												Log.Error("obj mailto", exc.Message);
												Toast.MakeText(Application.Context, "There are no email clients installed.", ToastLength.Short).Show();
											}
										}
										else if(uri.Scheme == "tel")
										{
                                            var callIntent = new Intent(Intent.ActionView);

                                            callIntent.SetData(uri);
                                            docView.StartActivity(callIntent);

                                            AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.SystemAction, obj.AnalyticsName);
                                        }
										else if(uri.Scheme == "sms")
										{
                                            try
                                            {
                                                var smsUri = Android.Net.Uri.Parse("smsto:" + uri.SchemeSpecificPart);
                                                var smsIntent = new Intent(Intent.ActionView, smsUri);
                                                //smsIntent.PutExtra ("sms_body", "Hello from Xamarin.Android");  
                                                docView.StartActivity(smsIntent);

                                                AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.SystemAction, obj.AnalyticsName);
                                            }
                                            catch (ActivityNotFoundException exc)
                                            {
                                                Log.Error("obj sms", exc.Message);
                                                Toast.MakeText(Application.Context, "SMS non consentiti", ToastLength.Short).Show();
                                            }
                                        }
										else
										{
                                            try
                                            {
                                                var intent = new Intent(Intent.ActionView, uri);
                                                docView.StartActivity(intent);

                                                AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.SystemAction, obj.AnalyticsName);
                                            }
                                            catch(Exception exc)
                                            {
                                                Log.Error("obj link altro", exc.Message);
                                            }
										}
									};
								}
							}
							catch(Exception ex)
							{
								Utils.WriteLog("Errore 'hlink'", ex.Message);
							} 

							if(actionButton != null)
							{
								actionButton.Enabled = true;
								actionButton.Clickable = true;
							}
						}
						//comandi scroll
						else if(el.Key == "scrollCmd")
						{
							ScrollControl sCtrl = (ScrollControl)el.Value;

							actionButton.Touch += (sender, e) =>
							{
								try
								{
									if(e.Event.Action == MotionEventActions.Up) //UP
									{
                                        RelativeLayout o = Objects.GetObjectByReference(objViewList, sCtrl.Reference, docView, scrollView);  
                                        if(o != null)
                                        {
											ScrollerView iv = null;                                    

											for(int i = 0; i < o.ChildCount; i++)
											{
												if(o.GetChildAt(i).GetType() == typeof(ScrollerView))
												{
													iv = o.GetChildAt(i) as ScrollerView;
													break;
												}
											}

											if(iv != null)
											{                          
												iv.StopScroll();
											}
										}

										actionButton.CallOnClick();
									}
									else if(e.Event.Action == MotionEventActions.Down) //DOWN
									{
                                        RelativeLayout o = Objects.GetObjectByReference(objViewList, sCtrl.Reference, docView, scrollView);
                                        ScrollerView targetScroll = null;


                                        if(o != null)
                                        {
											ScrollerView iv = null;                                    

											for(int i = 0; i < o.ChildCount; i++)
											{
												if(o.GetChildAt(i).GetType() == typeof(ScrollerView))
												{
													iv = o.GetChildAt(i) as ScrollerView;
													break;
												}
											}

											if(iv != null)
											{
												//se devo aprire il poup ed è esclusivo chiudo gli altri
												if(sCtrl.Action == "switchPopUp" && !iv.PopUpVisible && iv.Esclusivo)
												{
													foreach(KeyValuePair<string, RelativeLayout> kv in objViewList)
													{
														if(kv.Key == sCtrl.Reference)
															continue;

														ScrollerView sv = null;                                 

														for(int i = 0; i < kv.Value.ChildCount; i++)
														{
															if(kv.Value.GetChildAt(i).GetType() == typeof(ScrollerView))
															{
																sv = kv.Value.GetChildAt(i) as ScrollerView;
																break;
															}
														}

														if(sv != null)
														{
															if(sv.IsPopUp && sv.PopUpVisible)
															{
																sv.ClosePopUp();
															}
														}
													}
												}

												//iv.GoAction(sCtrl);
                                                targetScroll = iv;
											}
										}
										else if((sCtrl.Action == "open" || sCtrl.Action == "switchPopUp") && scrollView != null)
										{
											//scrollView.GoAction(sCtrl);
                                            targetScroll = scrollView;
										}

										if (targetScroll != null)
										{
											switch (sCtrl.Action)
											{
												case "scroll":
													targetScroll.StopScroll();
													targetScroll.StartScroll(sCtrl.Direction, sCtrl.Step);
													break;
												case "margin":
													targetScroll.ScrollToMargin(sCtrl.Direction);
													break;
												case "open":
													if (targetScroll.DropOpened)
														AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.TendinaClose, obj.AnalyticsName);
													else
														AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.TendinaOpen, obj.AnalyticsName);

													targetScroll.ToggleObject(sCtrl.Direction);
													break;
												case "switchPopUp":
													if (targetScroll.PopUpVisible)
														AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.PopupClose, obj.AnalyticsName);
													else
														AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.PopupOpen, obj.AnalyticsName);
                                                    
													targetScroll.TogglePopUp();													
													break;
											}
										}
									}

								}
								catch(Exception ex)
								{
									Utils.WriteLog("Errore 'scrollCmd'", ex.Message);
								} 
							};
								
							actionButton.Enabled = true;
							actionButton.Clickable = true;
						}
						else if(el.Key == "notaUtente")
						{
							NotaUtente nota = (NotaUtente)el.Value;

                            if(nota.PaginaAbs == "")
                                nota.PaginaAbs = docView.Pubblicazione.RelativeToAbsolutePage(nota.IdDocumento, nota.Pagina).ToString();
                            
							if(nota.Inline)
							{
								NotaInlineView nView = new NotaInlineView(Application.Context, nota, docView);
								objView.AddView(nView, elParam);
							}
							else
							{
								actionButton.Click += (sender, e) =>
								{
									docView.ShowNotaPop(nota);

                                    AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.NotaOpen, obj.AnalyticsName);
								};

								actionButton.Enabled = true;
								actionButton.Clickable = true;
							}
						}
						else if(el.Key == "goToPub")
						{
							PubNav pubNav = (PubNav)el.Value;

                            if(pubNav.Pubblicazione != null)
                            {
                                string pubPath = System.IO.Path.Combine(DataManager.Get<ISettingsManager>().Settings.DocPath, pubNav.Cartella, pubNav.Pubblicazione);

                                actionButton.Click += (sender, e) => {
                                    /*if (System.IO.File.Exists(pubPath) || System.IO.Directory.Exists(pubPath))
                                    {
                                        Pubblicazione pub = new Pubblicazione(pubPath);

                                        docView.LoadPub(pub, pubNav.IdDocumento != "" ? pubNav.IdDocumento : pub.ID, pubNav.Pagina.ToString());

                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.PubOpen, obj.AnalyticsName);
                                    }
                                    else if (pubNav.Url != "")
                                    {
                                        var alert = new AlertDialog.Builder(docView);
                                        alert.SetTitle(docView.GetString(Resource.String.pub_download));
                                        alert.SetMessage(docView.GetString(Resource.String.pub_downloadFile));
                                        alert.SetNegativeButton(docView.GetString(Resource.String.gen_cancel), delegate
                                        {
                                            return;
                                        });
                                        alert.SetPositiveButton("Ok", delegate
                                        {
                                            ThreadPool.QueueUserWorkItem(delegate
                                            {
                                                var downloads = DownloadManager.GetDocuments(System.IO.Path.GetDirectoryName(pubNav.Url));

                                                if (downloads.Count > 0)
                                                {
                                                    var down = downloads.Where(x => x.RelativePath == pubNav.Url).FirstOrDefault();

                                                    if (down != null && down.ID != null)
                                                    {
                                                        if (down.Stato == DownloadStato.Update || down.Stato == DownloadStato.Download)
                                                        {
                                                            MBDownloadManager.RequestDownload(down.Uri, new VoidNotify());
                                                        }
                                                    }
                                                }
                                            });
                                        });

                                        alert.Show().SetDivider();
                                    }*/

                                    docView.NavTo(pubNav);
                                };

                                actionButton.Enabled = true;
							    actionButton.Clickable = true;							    
                            }
						}
						else if(el.Key == "appFunc")
						{
							AppFunc appFunc = (AppFunc)el.Value;
							actionButton.Click += (sender, e) => 
							{
								switch(appFunc.Function)
								{
									case "tabNav":
										if(appFunc.Params.Length > 0)
										{										

                                            docView.MenuNav(appFunc.Params);
										}

										break;
									case "openPop":
										if(appFunc.Params.Length > 0)
										{
											docView.OpenPopUp(appFunc.Params);
										}
										break;
									case "downloadDir":
										if(appFunc.Params.Length > 0)
										{
											var alert = new AlertDialog.Builder(docView);
											alert.SetTitle(docView.GetString(Resource.String.pub_download));
											alert.SetMessage(docView.GetString(Resource.String.pub_downloadDir));
											alert.SetNegativeButton(docView.GetString(Resource.String.gen_cancel), delegate
											{
												return;
											});
											alert.SetPositiveButton("Ok", delegate
											{
												var dir = appFunc.Params[0];

												ThreadPool.QueueUserWorkItem(delegate
												{
													var downloads = DownloadManager.GetDocuments(dir);

													foreach(var down in downloads)
													{
														if(down.Stato == DownloadStato.Update || down.Stato == DownloadStato.Download)
														{
                                                            MBDownloadManager.RequestDownload(down.Uri, new VoidNotify());
														}
													}
												});
											});

											alert.Show().SetDivider();
										}
										break;
									case "downloadFile":
										if(appFunc.Params.Length > 0)
										{
											var alert = new AlertDialog.Builder(docView);
											alert.SetTitle(docView.GetString(Resource.String.pub_download));
											alert.SetMessage(docView.GetString(Resource.String.pub_downloadFile));
											alert.SetNegativeButton(docView.GetString(Resource.String.gen_cancel), delegate
											{
												return;
											});
											alert.SetPositiveButton("Ok", delegate
											{
												var file = appFunc.Params[0];

												ThreadPool.QueueUserWorkItem(delegate
												{
													var downloads = DownloadManager.GetDocuments(System.IO.Path.GetDirectoryName(file));

													if(downloads.Count > 0)
													{
														var down = downloads.Where(x => x.RelativePath == file).FirstOrDefault();

														if(down != null && down.ID != null)
														{
                                                            if (down.Stato == DownloadStato.Update || down.Stato == DownloadStato.Download)
                                                            {
                                                                MBDownloadManager.RequestDownload(down.Uri, new VoidNotify());
                                                            }
														}
													}											
												});
											});

											alert.Show().SetDivider();
											
										}
										break;
									default:
										break;
								}

                                AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.AppNav, obj.AnalyticsName);
							};

							actionButton.Enabled = true;
							actionButton.Clickable = true;
						}
						else if(el.Key == "mappa")
						{
							Mappa mappa = (Mappa)el.Value;

							actionButton.Click += (sender, e) =>
							{
								Intent i = new Intent();
								i.SetClass(Application.Context, typeof(MappaViewScreen2));
								i.SetFlags(ActivityFlags.ClearTop);

								i.PutExtra("mappa", JsonConvert.SerializeObject(mappa));

								docView.StartActivity(i);

                                AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.MapOpen, obj.AnalyticsName);
							};

							actionButton.Enabled = true;
							actionButton.Clickable = true;

							if(!mappa.Popup)
							{
								//MappaView mapView = new MappaView(Application.Context, mappa, docView);
								//objView.AddView(mapView, elParam);

								/*MappaFragment map = new MappaFragment(mappa, docView);

								FrameLayout frame = new FrameLayout(Application.Context);
								frame.Id = 10101010;

								objView.AddView(frame, elParam);*/

								//docView.AddFragment(10101010, map);

								//objView.Id = 10101010;

								//FragmentTransaction fragTx = docView.FragmentManager.BeginTransaction();

								//fragTx.Add(10101010, map);
								//fragTx.Commit();
								//objView.AddView(map, elParam);
							}
							/*else
							{
								MappaViewController mapView = new MappaViewController(mappa, docView);
								actionButton.TouchUpInside += (sender, e) => 
								{
									docView.MB_PresentViewController(mapView, true);
								}; 

								actionButton.UserInteractionEnabled = true;
								actionButton.Opaque = true;
							}*/
						}
						else if(el.Key == "stat")
						{
							Statistica stat = (Statistica)el.Value;

							actionButton.Click += (sender, e) =>
							{
								var par = new Dictionary<string, string>();
								par.Add("variabile", stat.Variabile);
								par.Add("language", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
								StatisticheManager.AddStat(par);
							};

							actionButton.Enabled = true;
							actionButton.Clickable = true;
						}
						else if(el.Key == "animazione")
						{ 
							Animazione animaz = (Animazione)el.Value;

							docView.RunOnUiThread(() => {
								AnimazioneView bView = new AnimazioneView(Application.Context, animaz, artPath);
								//bView.SetBackgroundColor(Color.Red);
								objView.AddView(bView, elParam);
							});	

							objView.Interaction = false;
						}
                        else if(el.Key == "socialSharing")
                        {
                            SocialSharing share = (SocialSharing)el.Value;

                            actionButton.Click += (sender, e) => 
                            {
                                string fbID = docView.GetString(Resource.String.facebook_app_id);

                                if (fbID == "")
                                    return;

                                FacebookSdk.SdkInitialize(docView.ApplicationContext);
                                var shareDialog = new ShareDialog(docView);

                                if(ShareDialog.CanShow(Java.Lang.Class.FromType(typeof(ShareLinkContent))))
                                {
                                    try
                                    {
                                        if (share.Link != "")
                                        {
                                            var link = "http://" + share.Link.Replace("http://", "");

                                            ShareLinkContent content = new ShareLinkContent.Builder()
                                                                 .SetContentUrl(Android.Net.Uri.Parse(link))
                                                                 .JavaCast<ShareLinkContent.Builder>()
                                                                 .Build();

                                            shareDialog.Show(content);
                                        }
                                        else if (share.Image != "")
                                        {
                                            var imgPath = System.IO.Path.Combine(artPath, share.Image);

                                            if (System.IO.File.Exists(imgPath))
                                            {
                                                var image = BitmapFactory.DecodeFile(imgPath);

                                                var sharePhoto = new SharePhoto.Builder()
                                                    .SetBitmap(image).Build().JavaCast<SharePhoto>();

                                                var photos = new List<SharePhoto>();
                                                photos.Add(sharePhoto);

                                                SharePhotoContent content = new SharePhotoContent.Builder()
                                                                               .SetPhotos(photos)
                                                                 .JavaCast<SharePhotoContent.Builder>()
                                                                 .Build();

                                                shareDialog.Show(content);
                                            }
                                        }
                                    }
                                    catch(Exception ex)
                                    {
                                        Log.Error("socialSharing", ex.Message);
                                    }
                                }

                                AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.SocialShare, obj.AnalyticsName);
                            };

                            actionButton.Enabled = true;
                            actionButton.Clickable = true;
                        }
                        else if (el.Key == "videoEmbed")
                        {
                            VideoEmbed video = (VideoEmbed)el.Value;

                            string url = "";

                            if (video.YoutubeID != "")
                            {
                                url = "https://www.youtube.com/embed/" + video.YoutubeID + "?rel=0";
                            }
                            else if (video.VimeoID != "")
                            {
                                url = "https://player.vimeo.com/video/" + video.VimeoID;
                            }

                            if (video.PopUp)
                            {
                                actionButton.Click += (sender, e) =>
                                {
                                    Intent i = new Intent();
                                    i.SetClass(Application.Context, typeof(BrowserViewScreen));
                                    i.PutExtra("url", url);
                                    i.PutExtra("tipo", "");
                                    i.PutExtra("pageFit", true);
                                    i.PutExtra("basePath", artPath);
                                    docView.StartActivity(i);

                                    AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.ResourceOpen, obj.AnalyticsName);
                                };

                                actionButton.Enabled = true;
                                actionButton.Clickable = true;
                            }
                            else
                            {
                                docView.RunOnUiThread(() =>
                                {
                                    VideoEmbedView webView = new VideoEmbedView(docView, video, artPath);

                                    objView.AddView(webView, elParam);
                                });
                            }
                        }
                        else if (el.Key == "slider")
                        {
                            RectF frame = new RectF(0, 0, param.Width, param.Height);

                            SliderView slider = new SliderView(Application.Context, (Slider)el.Value, artPath, docView, frame, scale);

                            objView.AddView(slider);
                        }
                        else if (el.Key == "slideNav")
                        {
                            //actionButton.BackgroundColor = UIColor.Green;
                            SlideNav nav = (SlideNav)el.Value;

                            if (nav.Next || nav.Prev)
                            {
                                if (actionButton != null)
                                {
                                    actionButton.Click += (sender, e) =>
                                    {
                                        try
                                        {
                                            RelativeLayout o = Objects.GetObjectByReference(objViewList, nav.Reference, docView, new ScrollerView(Application.Context));
                                            if (o != null)
                                            {
                                                SliderView mv = null;

                                                for (int i = 0; i < o.ChildCount; i++)
                                                {
                                                    if (o.GetChildAt(i).GetType() == typeof(SliderView))
                                                    {
                                                        mv = o.GetChildAt(i) as SliderView;
                                                    }
                                                }

                                                if (mv != null)
                                                {
                                                    if (nav.Next)
                                                    {
                                                        mv.NextState();
                                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.SliderNext, obj.AnalyticsName);
                                                    }
                                                    else
                                                    {
                                                        mv.PrevState();
                                                        AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.SliderPrev, obj.AnalyticsName);
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Utils.WriteLog("Errore 'nstate/pstate'", ex.Message);
                                        }
                                    };
                                }
                            }
                            else
                            {
                                if (actionButton != null)
                                {
                                    actionButton.Click += (sender, e) =>
                                    {
                                        try
                                        {
                                            RelativeLayout o = Objects.GetObjectByReference(objViewList, nav.Reference, docView, new ScrollerView(Application.Context));
                                            if (o != null)
                                            {
                                                SliderView mv = null;


                                                for (int i = 0; i < o.ChildCount; i++)
                                                {
                                                    if (o.GetChildAt(i).GetType() == typeof(SliderView))
                                                    {
                                                        mv = o.GetChildAt(i) as SliderView;
                                                    }
                                                }

                                                if (mv != null)
                                                {
                                                    mv.GoToState(nav.State);
                                                    AnalyticsService.SendEvent(docView.Pubblicazione.Titolo, AnalyticsEventAction.SliderSlide, obj.AnalyticsName);
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Utils.WriteLog("Errore 'sstate'", ex.Message);
                                        }

                                    };
                                }
                            }

                            if (actionButton != null)
                            {
                                actionButton.Enabled = true;
                                actionButton.Clickable = true;
                            }
                        }
                    }

					objView.AddView(actionButton);
					//articleView.AddSubview(objView);
					objViewList.Add(obj.Nome, objView);

					//obj = null;
				}
				catch(Exception ex)
				{
					Utils.WriteLog("CreateObjects: ", ex.Message);
				}
			}

			oggetti = null;

			return objViewList;
		}
	}
}

