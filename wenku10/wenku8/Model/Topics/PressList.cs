using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

namespace wenku8.Model.Topics
{
	using Ext;
	using ListItem;
	using Resources;
	using Settings;
	using System;

	class PressList
	{
		public PressList( Action<PressList> CompleteHandler )
		{
			if ( Shared.Storage.FileExists( FileLinks.ROOT_WTEXT + FileLinks.PRESS_LISTF ) )
			{
				CompleteHandler( this );
				return;
			}

			IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );
			wCache.InitDownload(
				"PRESSLIST"
				, X.Call<XKey[]>(
					XProto.WRequest
					, "GetXML"
					, X.Const<string>( XProto.WProtocols, "COMMAND_TLIST_PARAM_SORT" ) )
				, ( DRequestCompletedEventArgs e, string id ) => {
					Shared.Storage.WriteBytes( FileLinks.ROOT_WTEXT + FileLinks.PRESS_LISTF, e.ResponseBytes );
					CompleteHandler( this );
				}, Utils.DoNothing, false
			 );
		}

		public Press[] GetList()
		{
			Press[] pr = null;
			if ( Shared.Storage.FileExists( FileLinks.ROOT_WTEXT + FileLinks.PRESS_LISTF ) )
			{
				XDocument Xml = XDocument.Parse( Shared.Storage.GetString( FileLinks.ROOT_WTEXT + FileLinks.PRESS_LISTF ) );
				IEnumerable<XElement> press = Xml.Descendants( "item" );
				int l;
				pr = new Press[l = press.Count()];
				for ( int i = 0; i < l; i++ )
				{
					pr[i] = new Press( press.ElementAt( i ).Value, press.ElementAt( i ).Attribute( "sort" ).Value, "" );
				}
			}
			return pr;
		}
	}
}
