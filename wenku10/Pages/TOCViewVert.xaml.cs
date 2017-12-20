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

namespace wenku10.Pages
{
	sealed partial class TOCViewVert : TOCPageBase
	{
		private TOCViewVert()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public TOCViewVert( BookItem Book ) : this() { Init( Book ); }

		protected override void SetTemplate()
		{
			base.SetTemplate();
			LayoutRoot.FlowDirection = LayoutSettings.IsRightToLeft
				? FlowDirection.RightToLeft
				: FlowDirection.LeftToRight
				;
		}

		protected override void SetTOC( BookItem b )
		{
			base.SetTOC( b );
			TOCData.SetViewSource( VolumesViewSource );
			LayoutRoot.DataContext = TOCData;
		}

	}
}