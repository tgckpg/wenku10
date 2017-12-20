using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Helpers;

namespace GR.Model.Section.SharersHub
{
	using ListItem.Sharers;

	sealed class Activities : ObservableCollection<Activity>
	{
		public async void CheckActivity( Activity Act )
		{
			Act.Value();

			// Roughly wait a moment then remove it
			await Task.Delay( 400 );
			Remove( Act );
		}

		public void Add( Func<string> StxText, Action A )
		{
			Worker.UIInvoke( () =>
			{
				Add( new Activity( StxText(), A ) );
			} );
		}

		public void AddUI( Activity Act )
		{
			Worker.UIInvoke( () => Add( Act ) );
		}

	}
}