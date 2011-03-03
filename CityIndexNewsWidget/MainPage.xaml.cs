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
						ErrTextBox.Text = args.ExceptionObject.ToString();
						TabControl.SelectedItem = ErrTab;
					}
					catch (Exception exc)
					{
						Debugger.Break();
					}
				});

			args.Handled = true;
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			RefreshNews();
		}

		private void SettingsButton_Click(object sender, RoutedEventArgs args)
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

		private void NewsTicker_ItemClicked(object sender, TickerControl.ClickArgs args)
		{
			var newsObj = _data.News[args.Index];
			ShowNewsDetail(newsObj, "");
		}

		void RefreshNews()
		{
			_refreshTimer.Stop();
			RefreshButton.IsEnabled = false;

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
			var keywords = ApplicationSettings.Instance.AlertKeywords.Split(new[] { ' ' },
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
					ErrTextBox.Text = exc.ToString();
					TabControl.SelectedItem = ErrTab;
					RefreshButton.IsEnabled = true;
				});
		}

		private void UpdateNewsGrid()
		{
			Dispatcher.BeginInvoke(
				() =>
				{
					NewsTicker.DataContext = _data.News;

					TabControl.SelectedItem = NewsTab;
					RefreshButton.IsEnabled = true;
				});
		}

		private void ShowNewsDetail(NewsDTO newsObj, string title)
		{
			var window = new NewsDetailWindow();
			window.Title = title + newsObj.PublishDate + " " + newsObj.Headline;
			window.Closed +=
				(s, a) =>
				{
					NewsTicker.NewsStoryBoard.Resume();
				};
			NewsTicker.Cursor = Cursors.Wait;
			window.Cursor = Cursors.Wait;
			window.Show();

			_data.GetNewsDetailAsync(newsObj.StoryId,
				detail => Dispatcher.BeginInvoke(
					() =>
					{
						NewsTicker.Cursor = Cursors.Arrow;
						window.Cursor = Cursors.Arrow;
						window.Content.Text = detail.Story;
					}),
				ReportException);
		}
	}
}
