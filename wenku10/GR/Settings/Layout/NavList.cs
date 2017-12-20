using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.IO;

namespace GR.Settings.Layout
{
	using Ext;

	sealed class NavList : INavList
	{
		private const string TFileName = FileLinks.ROOT_SETTING + FileLinks.LAYOUT_NAVPAGE;
		private const string Horizontal = "IsHorizontal";

		private XRegistry LayoutSettings;

		public bool IsHorizontal
		{
			get
			{
				return LayoutSettings.Parameter( Horizontal ).GetBool( "enable" );
			}
			set
			{
				LayoutSettings.SetParameter( Horizontal, new XKey( "enable", value ) );
				LayoutSettings.Save();
			}
		}

		public NavList()
		{
			LayoutSettings = new XRegistry( AppKeys.TS_CXML, TFileName );
			InitParams();
		}

		private void InitParams()
		{
			if( LayoutSettings.Parameter( Horizontal ) == null )
			{
				IsHorizontal = !wenku10.MainStage.Instance.IsPhone;
			}
		}
	}
}