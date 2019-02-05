using System;
using System.Collections.Generic;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;

namespace InPublishing
{
    public class DrawerAdapter<T> : ArrayAdapter<string>
    {
        Context _context;
        List<string> _items;
        int _layoutRes;

        public DrawerAdapter(Context context, int resource, List<string> objects) : base(context, resource, objects)
        {
            this._context = context;
            this._items = objects;
            this._layoutRes = resource;
        }

        public override View GetView(int position, View convertView, Android.Views.ViewGroup parent)
        {
            var view = base.GetView(position, convertView, parent);

            var txtView = view.FindViewById<TextView>(Resource.Id.txtMenu);

            //txtView.SetBackgroundColor(Color.Red);

            /**/

            StateListDrawable bgColorList = new StateListDrawable();

            bgColorList.AddState(new int[] { Android.Resource.Attribute.StateActivated }, new ColorDrawable(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.MenuSelColor)));
            bgColorList.AddState(new int[] { }, new ColorDrawable(Color.Transparent));

            txtView.Background = bgColorList;

            /*int[][] states = new int[][] {
                new int[] { Android.Resource.Attribute.StateActivated}, // enabled
                new int[] { }
            };

            int[] colors = new int[] {
                Color.Green,
                Color.Fuchsia
            };

            ColorStateList txtColorList = new ColorStateList(states, colors);*/

            txtView.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.MenuTextColor));

            //icon
            string label = _items[position];
            int res = 0;
            switch (label.ToLower())
            {
                case "edicola":
                    res = Resource.Drawable.ic_home;
                    break;
                case "download":
                    res = Resource.Drawable.ic_menu_download;
                    break;
                case "impostazioni":
                    res = Resource.Drawable.ic_impostazioni;
                    break;
                case "crediti":
                    res = Resource.Drawable.ic_menu_info;
                    break;
                default:
                    break;
            }

            txtView.SetCompoundDrawablesWithIntrinsicBounds(res, 0, 0, 0);



            foreach(var dr in txtView.GetCompoundDrawables())
            {
                if(dr != null)
                    dr.Colorize(DataManager.Get<ISettingsManager>().Settings.MenuTextColor);
            }

            return view;

            /*View view = convertView;
            if (view == null)
            {
                LayoutInflater inflater = (LayoutInflater)_context.GetSystemService(Context.LayoutInflaterService);
                view = inflater.Inflate(_layoutRes, parent, false);
            }*/
        }
    }
}
