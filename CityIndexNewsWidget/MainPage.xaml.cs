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
			_timer.Stop();
			refreshButton.IsEnabled = false;

			RefreshNewsAsync();
			//ThreadPool.QueueUserWorkItem(x => RefreshNewsSyncThreadEntry());
			//RefreshNewsDummy();
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

						client.BeginListNewsHeadlines(ApplicationSettings.Instance.CategoryCode,
							ApplicationSettings.Instance.MaxCount,
							ar2 =>
							{
								try
								{
									var resp = client.EndListNewsHeadlines(ar2);
									_news = resp.Headlines;

									client.BeginLogOut(ar3 => { }, null);

									UpdateNewsGrid();
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

		private void ShowNewsDetailAsync(int storyId)
		{
			var client = new Client(RPC_URI);
			client.BeginLogIn(USERNAME, PASSWORD,
				ar =>
				{
					try
					{
						client.EndLogIn(ar);

						client.BeginGetNewsDetail(storyId.ToString(),
							ar2 =>
							{
								try
								{
									var resp = client.EndGetNewsDetail(ar2);

									client.BeginLogOut(ar3 => { }, null);

									ShowNewsDetail(resp.NewsDetail);
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

		private void ShowNewsDetail(NewsDetailDTO newsDetail)
		{
			Dispatcher.BeginInvoke(
				() =>
				{
					var window = new NewsDetailWindow();
					window.Title = newsDetail.PublishDate + " " + newsDetail.Headline;
					window.Content.Text = newsDetail.Story;
					window.Closed +=
						(s, a) =>
							{
								newsTicker.Cursor = Cursors.Arrow;
								newsTicker.NewsStoryBoard.Resume();
							};
					window.Show();
				});
		}

		void RefreshNewsSyncThreadEntry()
		{
			try
			{
				var client = new Client(RPC_URI);
				client.LogIn(USERNAME, PASSWORD);
				var resp = client.ListNewsHeadlines(ApplicationSettings.Instance.CategoryCode,
					ApplicationSettings.Instance.MaxCount);
				_news = resp.Headlines;
				client.LogOut();

				UpdateNewsGrid();

			}
			catch (Exception exc)
			{
				ReportException(exc);
			}

			Dispatcher.BeginInvoke(() => _timer.Start());
		}

		void RefreshNewsDummy()
		{
			var dummyList = new List<NewsDTO>();
			for (int i = 0; i < ApplicationSettings.Instance.MaxCount; i++)
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

			UpdateNewsGrid();
		}

		private void UpdateNewsGrid()
		{
			Dispatcher.BeginInvoke(
				() =>
				{
					newsTicker.DataContext = _news;

					tabControl.SelectedItem = newsTab;
					refreshButton.IsEnabled = true;
				});
		}

		private void settingsButton_Click(object sender, RoutedEventArgs args)
		{
			var settingsWindow = new SettingsWindow();
			settingsWindow.Show();
			settingsWindow.Closed +=
				(s, a) =>
				{
					if (settingsWindow.DialogResult != null && settingsWindow.DialogResult.Value)
						RefreshNews();
				};
		}

		private void newsTicker_ItemClicked(object sender, TickerControl.ClickArgs args)
		{
			var newsObj = _news[args.Index];
			newsTicker.Cursor = Cursors.Wait;
			ShowNewsDetailAsync(newsObj.StoryId);
		}
	}
}
