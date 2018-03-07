using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Messaging;

namespace GR.GSystem
{
	sealed class PageExtOperations : ActiveData
	{
		private static readonly Type SType = typeof( PageExtOperations );

		private static Messenger Msgr = new Messenger();

		public static async Task<T> Run<T>( Task<T> Op )
		{
			try
			{
				Msgr.Deliver( new Message( SType, "OP_START" ) );
				return await Op;
			}
			finally
			{
				Msgr.Deliver( new Message( SType, "OP_END" ) );
			}
		}

		public PageExtOperations()
		{
			Msgr.AddHandler( this, MandleLoading );
			MessageBus.Subscribe( this, HandleMessage );
		}

		private void MandleLoading( Message Mesg )
		{
			switch( Mesg.Content )
			{
				case "OP_START":
					IsLoading = true;
					break;
				case "OP_END":
					IsLoading = false;
					break;
			}
		}

		private void HandleMessage( Message Mesg )
		{
			Message = Mesg.Content;
		}

		private bool _IsLoading;
		public bool IsLoading
		{
			get => _IsLoading;
			private set
			{
				_IsLoading = value;
				NotifyChanged( "IsLoading" );
			}
		}

		private string _Message;
		public string Message
		{
			get => _Message;
			private set
			{
				_Message = value;
				NotifyChanged( "Message" );
			}
		}
	}
}