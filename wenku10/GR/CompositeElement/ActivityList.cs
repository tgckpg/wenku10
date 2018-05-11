using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace GR.CompositeElement
{
	// This is not a generic UI element, so it only works on certain project
	public class ActivityList : Control
	{
		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
			"ItemsSource", typeof( object ), typeof( ActivityList )
			, new PropertyMetadata( null ) );

		public object ItemsSource
		{
			get { return ( object ) GetValue( ItemsSourceProperty ); }
			set { SetValue( ItemsSourceProperty, value ); }
		}

		public static readonly DependencyProperty TargetBtnProperty = DependencyProperty.Register(
			"TargetBtn", typeof( AppBarButton ), typeof( ActivityList )
			, new PropertyMetadata( null, OnTargetBtnUpdate ) );

		public AppBarButton TargetBtn
		{
			get { return ( AppBarButton ) GetValue( TargetBtnProperty ); }
			set { SetValue( TargetBtnProperty, value ); }
		}

		public ItemClickEventHandler ItemClick;

		public ActivityList()
			: base()
		{
			DefaultStyleKey = typeof( ActivityList );
		}

		Polygon Pointergon;
		ListView ItemList;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Pointergon = ( Polygon ) GetTemplateChild( "Pointergon" );
			ItemList = ( ListView ) GetTemplateChild( "ItemList" );
			if( ItemList != null )
			{
				ItemList.ItemClick += ItemList_ItemClick;
			}

			UpdateDisplay();
		}

		private void ItemList_ItemClick( object sender, ItemClickEventArgs e ) => ItemClick?.Invoke( sender, e );

		private void UpdateDisplay()
		{
			if ( TargetBtn != null )
			{
				StackPanel Panel = VisualTreeHelper.GetParent( TargetBtn ) as StackPanel;
				if ( Panel != null )
				{
					int TotalBtns = Panel.Children.Count;
					int BtnIndex = Panel.Children.IndexOf( TargetBtn );

					TranslateTransform TT = new TranslateTransform();
					TT.X = -( TotalBtns - BtnIndex ) * TargetBtn.ActualWidth;

					Pointergon.RenderTransform = TT;
				}
			}
		}

		private static void OnTargetBtnUpdate( DependencyObject d, DependencyPropertyChangedEventArgs e ) => ( ( ActivityList ) d ).UpdateDisplay();
	}
}