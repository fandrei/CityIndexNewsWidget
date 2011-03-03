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

		private readonly DispatcherTimer _timer = new DispatcherTimer();
		readonly Data _data = new Data();

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
			newsTicker.Cursor = Cursors.Wait;
			_data.GetNewsDetailAsync(newsObj.StoryId, ShowNewsDetail, ReportException);
		}

		void RefreshNews()
		{
			_timer.Stop();
			refreshButton.IsEnabled = false;

			_data.RefreshNews(
				() =>
				{
					UpdateNewsGrid();
					Dispatcher.BeginInvoke(() => _timer.Start());
				},
				exception =>
				{
					ReportException(exception);
					Dispatcher.BeginInvoke(() => _timer.Start());
				});
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

	}
}
