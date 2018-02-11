using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

namespace GR.Model.Section
{

	sealed class ZSContext : ActiveData
	{
		public static readonly string ID = typeof( ZSContext ).Name;

		public ZSContext() { }


	}
}