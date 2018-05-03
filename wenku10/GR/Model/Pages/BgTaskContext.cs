using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Loaders;

namespace GR.Model.Pages
{
	public class BgTaskContext : ActiveData
	{
		private bool _IsLoading;
		public bool IsLoading
		{
			get => _IsLoading;
			set
			{
				_IsLoading = value;
				NotifyChanged( "IsLoading" );
			}
		}

		private string _Mesg;
		public string Mesg
		{
			get => _Mesg;
			set
			{
				_Mesg = value;
				NotifyChanged( "Mesg" );
			}
		}

		public volatile int CurrWork;

		private StringResBg _stx;
		private StringResBg Stx => _stx ?? ( _stx = new StringResBg( "LoadingMessage" ) );

		public Task<T> RunAsync<IN, T>( Func<IN, Task<T>> Work, IN Args )
		{
			if ( 0 < CurrWork )
			{
				throw new InvalidOperationException( "There are working task" );
			}

			IsLoading = true;

			Mesg = Stx.Text( "ProgressIndicator_PleaseWait" );

			Task<T> CurrentWork = Task.Run( () => Work( Args ) );
			CurrWork = CurrentWork.Id;

			CurrentWork.ContinueWith( x =>
			{
				IsLoading = false;
				CurrWork = 0;
			} );

			return CurrentWork;
		}

		public void Notify()
		{
			NotifyChanged( "IsLoading", "Mesg" );
		}
	}
}