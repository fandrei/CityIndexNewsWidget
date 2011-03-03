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

namespace CityIndexNewsWidget
{
	public partial class TickerControl : UserControl
	{
		public TickerControl()
		{
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
		}

		private void NewsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var data = (Array)DataContext;

			if (data == null)
				return;

			NewsStoryBoard.Stop();
			NewsGrid.SelectedItem = null;

			if (data.Length == 0)
				return;

			NewsAnimation.From = ActualHeight;
			NewsAnimation.To = -NewsGrid.ActualHeight;
			var secs = (NewsAnimation.From.Value - NewsAnimation.To.Value) / 50;
			NewsAnimation.Duration = new Duration(TimeSpan.FromSeconds(secs));

			NewsStoryBoard.Begin();
		}

		private void NewsGrid_MouseEnter(object sender, MouseEventArgs e)
		{
			NewsStoryBoard.Pause();
		}

		private void NewsGrid_MouseLeave(object sender, MouseEventArgs e)
		{
			NewsStoryBoard.Resume();
		}

		public class ClickArgs : EventArgs
		{
			public int Index;
		}

		public event EventHandler<ClickArgs> ItemClicked;

		private void NewsGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (ItemClicked != null)
				ItemClicked(this, new ClickArgs { Index = NewsGrid.SelectedIndex });
		}
	}
}
