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

		private void newsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var data = (Array)DataContext;

			if (data == null)
				return;

			NewsStoryBoard.Stop();
			newsGrid.SelectedItem = null;

			if (data.Length == 0)
				return;

			newsAnimation.From = ActualHeight;
			newsAnimation.To = -newsGrid.ActualHeight;
			var secs = (newsAnimation.From.Value - newsAnimation.To.Value) / 50;
			newsAnimation.Duration = new Duration(TimeSpan.FromSeconds(secs));

			NewsStoryBoard.Begin();
		}

		private void mainPage_MouseEnter(object sender, MouseEventArgs e)
		{
			NewsStoryBoard.Pause();
		}

		private void mainPage_MouseLeave(object sender, MouseEventArgs e)
		{
			NewsStoryBoard.Resume();
		}

		public class ClickArgs : EventArgs
		{
			public int Index;
		}

		public event EventHandler<ClickArgs> ItemClicked;

		private void newsGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (ItemClicked != null)
				ItemClicked(this, new ClickArgs { Index = newsGrid.SelectedIndex });
		}
	}
}
