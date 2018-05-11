using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Loaders;

namespace Microsoft.Phone.Controls
{
	// This is a hack
	class ControlResources : StringResources
	{
		public ControlResources()
		{
			_Load( "DateTimeUnits" );
			DefaultRes = BgResCont[ "DateTimeUnits" ];
		}
	}
}