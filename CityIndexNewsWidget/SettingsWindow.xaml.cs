﻿using System;
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
	public partial class SettingsWindow : ChildWindow
	{
		public SettingsWindow()
		{
			InitializeComponent();
		}

		private void ChildWindow_Loaded(object sender, RoutedEventArgs e)
		{
			DataContext = ApplicationSettings.Instance;
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			ApplicationSettings.Instance.Save();
			this.DialogResult = true;
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
		}
	}
}

