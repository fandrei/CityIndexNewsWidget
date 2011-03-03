using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CIAPI.DTO;

namespace CityIndexNewsWidget
{
	public partial class MainPage : UserControl
	{
		public MainPage()
		{
			InitializeComponent();
		}

		private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();
		readonly Data _data = new Data();

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			Application.Current.UnhandledException += OnUnhandledException;

			RefreshNews();

			InitTimer();
		}

		private void InitTimer()
		{
			_refreshTimer.Interval = TimeSpan.FromSeconds(ApplicationSettings.Instance.RefreshPeriodSecs);
			_refreshTimer.Tick += RefreshTimerTick;
			_refreshTimer.Start();
		}

		public void RefreshTimerTick(object o, EventArgs sender)
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
			var newsObj = _data.News[args.Index];
			ShowNewsDetail(newsObj, "");
		}

		void RefreshNews()
		{
			_refreshTimer.Stop();
			refreshButton.IsEnabled = false;

			_data.RefreshNews(
				() =>
				{
					UpdateNewsGrid();
					CheckKeywords();
					Dispatcher.BeginInvoke(() => _refreshTimer.Start());
				},
				exception =>
				{
					ReportException(exception);
					Dispatcher.BeginInvoke(() => _refreshTimer.Start());
				});
		}

		void CheckKeywords()
		{
			var keywords = ApplicationSettings.Instance.AlertKeywords.Split(new[] {' '},
				StringSplitOptions.RemoveEmptyEntries);

			// number of news and keywords isn't supposed to be big, so use the simplest algorithm
			foreach (var newsDto in _data.News)
			{
				foreach (var keyword in keywords)
				{
					if (newsDto.Headline.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) != -1)
					{
						ShowAlert(newsDto, keyword);
						break;
					}
				}
			}
		}

		void ShowAlert(NewsDTO news, string keyword)
		{
			var title = string.Format("Alert ({0}): ", keyword);
			Dispatcher.BeginInvoke(() => ShowNewsDetail(news, title));
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

		private void UpdateNewsGrid()
		{
			Dispatcher.BeginInvoke(
				() =>
				{
					newsTicker.DataContext = _data.News;

					tabControl.SelectedItem = newsTab;
					refreshButton.IsEnabled = true;
				});
		}

		private void ShowNewsDetail(NewsDTO newsObj, string title)
		{
			newsTicker.Cursor = Cursors.Wait;
			_data.GetNewsDetailAsync(newsObj.StoryId, 
				detail => ShowNewsDetail(detail, title), ReportException);
		}

		private void ShowNewsDetail(NewsDetailDTO newsDetail, string title)
		{
			Dispatcher.BeginInvoke(
				() =>
				{
					newsTicker.Cursor = Cursors.Arrow;

					var window = new NewsDetailWindow();
					window.Title = title + newsDetail.PublishDate + " " + newsDetail.Headline;
					window.Content.Text = newsDetail.Story;
					window.Closed +=
						(s, a) =>
						{
							newsTicker.NewsStoryBoard.Resume();
						};
					window.Show();
				});
		}

	}
}
