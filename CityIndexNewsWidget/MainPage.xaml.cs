﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

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

		private static readonly Uri RPC_URI = new Uri("http://ciapipreprod.cityindextest9.co.uk/tradingapi");
		private const string USERNAME = "xx189949";
		private const string PASSWORD = "password";

		private readonly DispatcherTimer _timer = new DispatcherTimer();

		private NewsDTO[] _news;

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			Application.Current.UnhandledException += OnUnhandledException;

			RefreshNews();

			InitTimer();
		}

		private void InitTimer()
		{
			_timer.Interval = TimeSpan.FromSeconds(ApplicationSettings.Instance.RefreshPeriodSecs);
			_timer.Tick += TimerTick;
			_timer.Start();
		}

		public void TimerTick(object o, EventArgs sender)
		{
			RefreshNews();
		}

		void OnUnhandledException(object sender, ApplicationUnhandledExceptionEventArgs args)
		{
			Dispatcher.BeginInvoke(
				() =>
				{
					try
					{
						errTextBox.Text = args.ExceptionObject.ToString();
						tabControl.SelectedItem = errTab;
					}
					catch (Exception exc)
					{
						Debugger.Break();
					}
				});

			args.Handled = true;
		}

		private void refreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshNews();
		}

		private void RefreshNewsAsync()
		{
			refreshButton.IsEnabled = false;

			var client = new Client(RPC_URI);
			client.BeginLogIn(USERNAME, PASSWORD,
				ar =>
				{
					try
					{
						client.EndLogIn(ar);

						client.BeginListNewsHeadlines("UK", 10,
							ar2 =>
							{
								try
								{
									var news = client.EndListNewsHeadlines(ar2);

									Dispatcher.BeginInvoke(
										() =>
										{
											DataContext = news.Headlines;
											tabControl.SelectedItem = newsTab;
											refreshButton.IsEnabled = true;
										});
								}
								catch (Exception exc)
								{
									ReportException(exc);
								}
							}, null);
					}
					catch (Exception exc)
					{
						ReportException(exc);
					}
				}, null);
		}

		private void ReportException(Exception exc)
		{
			Dispatcher.BeginInvoke(
				() =>
				{
					errTextBox.Text = exc.ToString();
					tabControl.SelectedItem = errTab;
					refreshButton.IsEnabled = true;
				});
		}

		void RefreshNews()
		{
			//RefreshNewsAsync();

			refreshButton.IsEnabled = false;
			_timer.Stop();
			ThreadPool.QueueUserWorkItem(x => RefreshNewsSyncThreadEntry());
		}

		void RefreshNewsSyncThreadEntry()
		{
			try
			{
				//var client = new Client(RPC_URI);
				//client.LogIn(USERNAME, PASSWORD);
				//var news = client.ListNewsHeadlines("UK", 10);
				//client.LogOut();

				var dummyList = new List<NewsDTO>();
				for (int i = 0; i < 20; i++)
				{
					var text = "news " + i;
					for (int t = 0; t < 5; t++)
						text += text;
					dummyList.Add(
						new NewsDTO
						{
							Headline = text,
							PublishDate = DateTime.Now,
							StoryId = i
						});
				}
				_news = dummyList.ToArray();

				Dispatcher.BeginInvoke(
					() =>
					{
						DataContext = _news;

						tabControl.SelectedItem = newsTab;
						refreshButton.IsEnabled = true;
					});

			}
			catch (Exception exc)
			{
				ReportException(exc);
			}

			Dispatcher.BeginInvoke(() => _timer.Start());
		}

		private void settingsButton_Click(object sender, RoutedEventArgs e)
		{
			var settingsWindow = new SettingsWindow();
			settingsWindow.Show();
		}

		private void newsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			newsAnimation.From = tabControl.ActualHeight;
			newsAnimation.To = -newsGrid.ActualHeight;
			var secs = (newsAnimation.From.Value - newsAnimation.To.Value) / 50;
			newsAnimation.Duration = new Duration(TimeSpan.FromSeconds(secs));

			newsStoryBoard.Begin();
		}

		private void mainPage_MouseEnter(object sender, MouseEventArgs e)
		{
			newsStoryBoard.Pause();
		}

		private void mainPage_MouseLeave(object sender, MouseEventArgs e)
		{
			newsStoryBoard.Resume();
		}
	}
}
