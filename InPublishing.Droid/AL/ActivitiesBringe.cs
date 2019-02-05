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

namespace InPublishing
{
	public class ActivitiesBringe 
	{
		private static Dictionary<string, Object> _objects = new Dictionary<string, Object>();

		/**
		* set object to static variable and retrieve it from another activity
		*
		* @param obj
		*/
		public static void SetObject(string key, Object obj) 
		{
			if(_objects.ContainsKey(key))
			{
				_objects[key] = obj;
			}
			else
			{
				_objects.Add(key, obj);
			}
		}

		/**
		* get object passed from previous activity
		*
		* @return
		*/
		public static Object GetObject(string key) 
		{
			if(_objects.ContainsKey(key))
			{
				Object obj = _objects[key];

				_objects.Remove(key);

				return obj;
			}

			return null;
		}
	}
}

