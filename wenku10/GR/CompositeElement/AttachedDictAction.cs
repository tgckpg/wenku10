using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku10.Pages.Dialogs;

namespace GR.CompositeElement
{
	using Model.Text;

	public sealed class AttachedDictAction
	{
		public static readonly string ID = typeof( AttachedDictAction ).Name;
		// Source
		public static string GetSource( DependencyObject d ) { return ( string ) d.GetValue( SourceProperty ); }
		public static void SetSource( DependencyObject d, string Source ) { d.SetValue( SourceProperty, Source ); }

		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.RegisterAttached( "Source", typeof( string ),
			typeof( AttachedDictAction ), new PropertyMetadata( null, InitiateContextMenu ) );

		private static void InitiateContextMenu( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			FrameworkElement Elem = d as FrameworkElement;
			if ( d == null ) return;

			object HasMenu = d.GetValue( FlyoutBase.AttachedFlyoutProperty );

			if ( HasMenu == null )
			{
				d.SetValue( FlyoutBase.AttachedFlyoutProperty, CreateMenuFlyout( d ) );

				Elem.RightTapped += ( s, e2 ) =>
				{
					FlyoutBase.ShowAttachedFlyout( ( FrameworkElement ) s );
				};
			}
			else
			{
				Logger.Log( ID, "Menuflyout already attached", LogType.DEBUG );
			}
		}

		private static MenuFlyout CreateMenuFlyout( DependencyObject d )
		{
			MenuFlyout Menu = new MenuFlyout();
			MenuFlyoutItem DictAction = new MenuFlyoutItem();
			StringResources stx = new StringResources( "ContextMenu" );
			DictAction.Text = stx.Text( "Search_Dict" );

			DictAction.Click += ( s, e ) =>
			{
				var j = Popups.ShowDialog( new EBDictSearch( new Paragraph( GetSource( d ) ) ) );
			};

			Menu.Items.Add( DictAction );

			return Menu;
		}
	}
}