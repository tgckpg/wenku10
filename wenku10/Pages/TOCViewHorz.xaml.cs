using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using GR.Model.Book;
using GR.Model.Pages;
using GR.Database.Models;

namespace wenku10.Pages
{
	sealed partial class TOCViewHorz : TOCPageBase
	{
		private TOCViewHorz()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public TOCViewHorz( BookItem Book )
			: this()
		{
			Init( Book );
			ToggleDir();
		}

		protected override void SetTOC( BookItem b )
		{
			base.SetTOC( b );
			LayoutRoot.DataContext = TOCData;

			if ( VolList != null && 0 < VolList.Items.Count() )
			{
				VolList.SelectedIndex = 0;
			}
		}

		protected override void ToggleDir()
		{
			if ( ThisBook.Entry.TextLayout.HasFlag( LayoutMethod.VerticalWriting ) )
			{
				LayoutRoot.FlowDirection = ThisBook.Entry.TextLayout.HasFlag( LayoutMethod.RightToLeft )
					? FlowDirection.RightToLeft
					: FlowDirection.LeftToRight
				;
			}
			else
			{
				PageProcessor.NavigateToTOC( this, ThisBook );
			}
		}

	}
}