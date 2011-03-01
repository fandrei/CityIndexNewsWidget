using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;

namespace CityIndexNewsWidget
{
	public class ApplicationSettings
	{
		public ApplicationSettings()
		{
			RefreshPeriodSecs = 60;
		}

		private int _refreshPeriodSecs;
		public int RefreshPeriodSecs
		{
			get { return _refreshPeriodSecs; }
			set { _refreshPeriodSecs = value; }
		}

		private static ApplicationSettings _instance;

		public static ApplicationSettings Instance
		{
			get { return _instance ?? (_instance = Load()); }
		}

		private const string FileName = @"ApplicationSettings.xml";

		public static void Reload()
		{
			_instance = Load();
		}

		public static ApplicationSettings Load()
		{
			var settings = new ApplicationSettings();

			using (var store = IsolatedStorageFile.GetUserStoreForApplication())
			{
				if (!store.FileExists(FileName))
					return settings;

				using (var isoStream = store.OpenFile(FileName, FileMode.Open))
				{
					var s = new XmlSerializer(typeof(ApplicationSettings));
					using (var rd = new StreamReader(isoStream))
					{
						settings = (ApplicationSettings)s.Deserialize(rd);
					}

					return settings;
				}
			}
		}

		public void Save()
		{
			using (var store = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (var isoStream = store.OpenFile(FileName, FileMode.Create))
				{
					var s = new XmlSerializer(typeof(ApplicationSettings));
					using (var writer = new StreamWriter(isoStream))
					{
						s.Serialize(writer, this);
					}
				}
			}
		}
	}
}
