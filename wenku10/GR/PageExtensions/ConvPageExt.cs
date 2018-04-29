using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Storage;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

namespace GR.PageExtensions
{
	using DataSources;
	using Model.Interfaces;

	sealed class ConvPageExt : PageExtension
	{
		private ConvViewSource ViewSource;

		public ConvPageExt( ConvViewSource ViewSource )
			: base()
		{
			this.ViewSource = ViewSource;
		}

		public override void Unload()
		{
		}

		protected override void SetTemplate()
		{
		}

	}
}
