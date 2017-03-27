using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using wenku8.Model.ListItem;
using wenku8.Model.Section;

namespace wenku10.Pages
{
	sealed partial class WCateList : Page
	{
		public static readonly string ID = typeof( Page ).Name;

		CategorizedSection CS;

		public WCateList()
		{
			this.InitializeComponent();
		}

		public WCateList( SubtleUpdateItem S )
			:this()
		{
			CS = new CategorizedSection();
			CS.PropertyChanged += CS_PropertyChanged;
			MainList.DataContext = CS;
			CS.Load( S.Payload );
		}

		private void CS_PropertyChanged( object sender, PropertyChangedEventArgs e )
		{
			switch ( e.PropertyName )
			{
				case "Data":
					ControlFrame.Instance.NavigateTo( PageId.W_NAV_LIST + CS.ListName, () => new WNavList( CS ) );
					break;
				case "NavListItem":
					ControlFrame.Instance.NavigateTo( PageId.W_NAV_LIST + CS.ListName, () => new WNavList( CS.NavListItem ) );
					break;
			}
		}

		private void ListView_ItemClick( object sender, ItemClickEventArgs e )
		{
			CS.LoadSubSections( ( ActiveItem ) e.ClickedItem );
		}

	}
}