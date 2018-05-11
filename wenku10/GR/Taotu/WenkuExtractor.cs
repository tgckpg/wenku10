using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.UI.Icons;

using libtaotu.Pages;

using wenku10.Pages.Dialogs.Taotu;

namespace GR.Taotu
{
	sealed class WenkuExtractor : GrimoireExtractor
	{
		protected override IconBase Icon { get { return new IconLogout() { AutoScale = true, Direction = Direction.Rotate270 }; } }

		public override async Task Edit()
		{
			await Popups.ShowDialog( new EditProcExtract( this ) );
			if ( SubEdit != null )
			{
				MessageBus.Send( typeof( ProceduresPanel ), "SubEdit", this );
			}
		}
	}
}