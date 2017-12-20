using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

namespace GR.Model.Section
{
	using AdvDM;
	using Book;
	using Ext;
	using ListItem;
	using Loaders;
	using Resources;
	using Settings;
	using GSystem;

	class CategorizedSection : ActiveData
	{
		private ObservableCollection<ActiveItem> _list_data;
		public ObservableCollection<ActiveItem> ListData
		{
			get
			{
				return _list_data;
			}
			set
			{
				_list_data = value;
			}
		}

		public SubtleUpdateItem NavListItem { get; private set; }

		public string ListName { get; private set; }

		public IEnumerable<BookItem> Data { get; private set; }

		#region Orientation Controls
		public ScrollMode HScroll
		{
			get { return Settings.IsHorizontal ? ScrollMode.Auto : ScrollMode.Disabled; }
		}

		public ScrollMode VScroll
		{
			get { return Settings.IsHorizontal ? ScrollMode.Disabled : ScrollMode.Auto; }
		}

		public ScrollBarVisibility HVis
		{
			get { return Settings.IsHorizontal ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden; }
		}

		public ScrollBarVisibility VVis
		{
			get { return Settings.IsHorizontal ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto; }
		}

		public Orientation Ori
		{
			get { return Settings.IsHorizontal ? Orientation.Vertical : Orientation.Horizontal; }
		}

		public VerticalAlignment VAlignment
		{
			get { return Settings.IsHorizontal ? VerticalAlignment.Top : VerticalAlignment.Center; }
		}

		public HorizontalAlignment HAlignment
		{
			get { return Settings.IsHorizontal ? HorizontalAlignment.Center : HorizontalAlignment.Left; }
		}

		public string AlignMode
		{
			get
			{
				return Settings.IsHorizontal
				  ? typeof( CompositeElement.PassiveSplitView ).AssemblyQualifiedName
				  : "PassiveSplitVertical";
			}
		}

		public double AvailableHeight
		{
			get
			{
				return Settings.IsHorizontal ? Resources.LayoutSettings.ScreenHeight : 60;
			}
		}

		public double HPadding { get { return Settings.IsHorizontal ? 60 : 0; } }
		public double VPadding { get { return Settings.IsHorizontal ? 0 : 60; } }
		#endregion

		private Settings.Layout.NavList Settings;

		public CategorizedSection()
		{
			ListData = new ObservableCollection<ActiveItem>();
			Settings = new Settings.Layout.NavList();
		}

		// Do the switching in overloads
		public void Load( string ListName )
		{
			if ( ListName == X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_STOPICS" ) )
			{
				// This cannot be generalize because of the nature
				// of this list
				// Download is handled in NavSelection's Push Special Topics
				LoadComplete( new Topics.Special().Topics );
			}
			else if ( ListName == X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_PRESS" ) )
			{
				Topics.PressList List = new Topics.PressList(
					( L ) => { LoadComplete( L.GetList() ); }
				);
			}
		}

		private void LoadComplete( IEnumerable<ActiveItem> Items )
		{
			foreach ( ActiveItem p in Items )
			{
				ListData.Add( p );
			}
			NotifyChanged( "ListData" );
		}

		public void LoadSubSections( ActiveItem Item )
		{
			Type ItemType = Item.GetType();
			if ( ItemType == typeof( Topic ) )
			{
				Topic Tp = Item as Topic;
				int TopicIndex = ListData.IndexOf( Item ) + 1;
				if ( ListData.IndexOf( Tp.Collections[ 0 ] ) != -1 )
				{
					foreach ( Digests d in Tp.Collections )
						ListData.Remove( d );
				}
				else
				{
					foreach ( Digests d in Tp.Collections )
						ListData.Insert( TopicIndex, d );
				}
				NotifyChanged( "ListData" );
			}
			else if ( ItemType == typeof( Digests ) )
			{
				Digests D = Item as Digests;
				SetListName( D );
				DownloadCategoryXml( D );
			}
			else if( ItemType == typeof( Press ) )
			{
				Press P = Item as Press;
				OpenNavigationList( P );
			}
		}

		private void SetListName( Digests D )
		{
			Type TopicType = typeof( Topic );
			foreach( ActiveItem Item in ListData )
			{
				if( Item.GetType() == TopicType )
				{
					Topic Tp = Item as Topic;
					if( Tp.Collections.Contains( D ) )
					{
						ListName = Tp.Name + " " + D.Name;
						NotifyChanged( "ListName" );
						return;
					}
				}
			}
		}

		private void SetListName( ActiveItem Item )
		{
			ListName = Item.Name;
			NotifyChanged( "ListName" );
		}

		private void OpenNavigationList( ActiveItem Item )
		{
			NavListItem = new SubtleUpdateItem( Item.Name, Item.Desc, Item.Desc2, Item.Payload );
			// This will cause an direct navigation to the navigation list view
			// in TopList mode
			NotifyChanged( "NavListItem" );
		}

		private void DownloadCategoryXml( Digests D )
		{
			if ( !Shared.Storage.FileExists( FileLinks.ROOT_WTEXT + D.Payload + ".xml" ) )
			{
				IRuntimeCache wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache );
				wCache.InitDownload(
					D.Payload
					, X.Call<XKey[]>( XProto.WRequest, "GetXML", D.Payload )
					, SListView, Utils.DoNothing, false );
			}
			else
			{
				SetFrameData(
					Shared.Storage.GetString( FileLinks.ROOT_WTEXT + D.Payload + ".xml" )
				);
			}
		}

		private void SListView( DRequestCompletedEventArgs e, string id )
		{
			// Write String Here
			Shared.Storage.WriteString( FileLinks.ROOT_WTEXT + id + ".xml", e.ResponseString );
			SetFrameData( e.ResponseString );
		}

		private void SetFrameData( string XmlData )
		{
			BookPool Bp = Shared.BooksCache;
			string[] Ids = PassBookFromList( XmlData, Bp );

			Expression<Action<IList<BookItem>>> handler = B => UpdateData( B );
			IListLoader LL = X.Instance<IListLoader>( XProto.ListLoader, Ids, Bp, handler.Compile() );
		}

		private void UpdateData( IList<BookItem> B )
		{
			Data = B;
			// Categroized List is listening to this property
			// Then Trigger Navigation to the NavList once data is available
			NotifyChanged( "Data" );
		}

		private string[] PassBookFromList( string xml, BookPool BookReference )
		{
			string[] p = null;
			try
			{
				XDocument xd = XDocument.Parse( xml );
				IEnumerable<XElement> books = xd.Descendants( "item" );
				p = new string[ books.Where( id => id.Attribute( AppKeys.GLOBAL_AID ).Value != "" ).Count() ];
				int i = 0;
				foreach ( XElement book in books )
				{
					string id = book.Attribute( AppKeys.GLOBAL_AID ).Value;
					BookItem b;
					if ( id != "" )
					{
						b = X.Instance<BookItem>( XProto.BookItemEx, id );
						BookReference[ id ] = b;
						b.Title = book.Value;
						p[ i++ ] = id;
					}
					else
					{
						b = new NonCollectedBook( book.Value );
						BookReference[ "-1" ] = b;
					}
				}
			}
			catch ( Exception )
			{

			}
			return p;
		}

	}
}
