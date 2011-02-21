using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using CIAPI.DTO;
using CIAPI.Rpc;

namespace CityIndexNewsWidget
{
	public partial class MainPage : UserControl
	{
		public MainPage()
		{
			InitializeComponent();
		}

		private static readonly Uri RPC_URI = new Uri("https://ciapipreprod.cityindextest9.co.uk/tradingapi");
		private static readonly Uri STREAMING_URI = new Uri("https://pushpreprod.cityindextest9.co.uk/CITYINDEXSTREAMING");
		private const string USERNAME = "xx189949";
		private const string PASSWORD = "password";

		private void refreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshNews();
		}

		private void RefreshNews()
		{
			refreshButton.IsEnabled = false;

			var client = new Client(RPC_URI);
			client.BeginLogIn(USERNAME, PASSWORD,
				ar =>
				{
					try
					{
						client.EndLogIn(ar);
						var news = client.ListNewsHeadlines("UK", 10);
						client.LogOut();

						Dispatcher.BeginInvoke(
							() =>
							{
								DataContext = news;
								tabControl1.SelectedItem = newsTab;
								refreshButton.IsEnabled = true;
							});
					}
					catch (Exception exc)
					{
						Dispatcher.BeginInvoke(
							() =>
							{
								errTextBox.Text = exc.ToString();
								tabControl1.SelectedItem = errTab;
								refreshButton.IsEnabled = true;
							});
					}
				}, null);
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			RefreshNews();
		}
	}
}
