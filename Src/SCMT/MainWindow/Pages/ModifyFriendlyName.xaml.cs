﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UICore.Controls.Metro;

namespace SCMTMainWindow.Pages
{
	/// <summary>
	/// ModifyFriendlyName.xaml 的交互逻辑
	/// </summary>
	public partial class ModifyFriendlyName : MetroWindow
	{
		private ModifyFriendlyName()
		{
			InitializeComponent();
		}

		public static ModifyFriendlyName NewDlg(string strFriendlyName)
		{
			return new ModifyFriendlyName();
		}

		private void CancelModify_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ConfirmModify_OnClick(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}
