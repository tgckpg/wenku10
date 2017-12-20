using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.UI;
using GR.Effects;

namespace GR.CompositeElement
{
	public enum HoverStates { Idle, Loading }

	[TemplateVisualState( GroupName = HoverStatesGroup, Name = "Loading" )]
	[TemplateVisualState( GroupName = HoverStatesGroup, Name = "Idle" )]
	public sealed class LSBookItem : Control
	{
		public static readonly string ID = typeof( LSBookItem ).Name;

		private const string HoverStatesGroup = "HoverStates";

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register( "Title", typeof( string ), typeof( LSBookItem ), new PropertyMetadata( "{Title}", VisualDataChanged ) );
		public static readonly DependencyProperty DescProperty = DependencyProperty.Register( "Desc", typeof( string ), typeof( LSBookItem ), new PropertyMetadata( "{Desc}", VisualDataChanged ) );

		public static readonly DependencyProperty FavStateProperty = DependencyProperty.Register( "FavState", typeof( TransitionState ), typeof( LSBookItem ), new PropertyMetadata( TransitionState.Inactive, VisualDataChanged ) );
		public static readonly DependencyProperty SpiderStateProperty = DependencyProperty.Register( "SpiderState", typeof( TransitionState ), typeof( LSBookItem ), new PropertyMetadata( TransitionState.Inactive, VisualDataChanged ) );
		public static readonly DependencyProperty FailedStateProperty = DependencyProperty.Register( "FailedState", typeof( ControlState ), typeof( LSBookItem ), new PropertyMetadata( ControlState.Foreatii, VisualDataChanged ) );
		public static readonly DependencyProperty CheckedStateProperty = DependencyProperty.Register( "CheckedState", typeof( ControlState ), typeof( LSBookItem ), new PropertyMetadata( ControlState.Foreatii, VisualDataChanged ) );
		public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register( "IsLoading", typeof( bool ), typeof( LSBookItem ), new PropertyMetadata( false, ChangeState ) );

		public static readonly DependencyProperty StateProperty = DependencyProperty.Register( "State", typeof( HoverStates ), typeof( LSBookItem ), new PropertyMetadata( HoverStates.Idle, OnStateChanged ) );

		public string Title
		{
			get { return ( string ) GetValue( TitleProperty ); }
			set { SetValue( TitleProperty, value ); }
		}

		public string Desc
		{
			get { return ( string ) GetValue( DescProperty ); }
			set { SetValue( DescProperty, value ); }
		}

		public ControlState CheckedState
		{
			get { return ( ControlState ) GetValue( CheckedStateProperty ); }
			set { SetValue( CheckedStateProperty, value ); }
		}

		public ControlState FavState
		{
			get { return ( ControlState ) GetValue( FavStateProperty ); }
			set { SetValue( FavStateProperty, value ); }
		}

		public ControlState SpiderState
		{
			get { return ( ControlState ) GetValue( SpiderStateProperty ); }
			set { SetValue( SpiderStateProperty, value ); }
		}

		public ControlState FailedState
		{
			get { return ( ControlState ) GetValue( FailedStateProperty ); }
			set { SetValue( FailedStateProperty, value ); }
		}

		public bool IsLoading
		{
			get { return ( bool ) GetValue( IsLoadingProperty ); }
			set { SetValue( IsLoadingProperty, value ); }
		}

		public HoverStates State
		{
			get { return ( HoverStates ) GetValue( StateProperty ); }
			set { SetValue( StateProperty, value ); }
		}

		public LSBookItem()
		{
			DefaultStyleKey = typeof( LSBookItem );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}

		private void UpdateVisualState( bool useTransitions )
		{
			switch ( State )
			{
				case HoverStates.Loading:
					VisualStateManager.GoToState( this, "Loading", useTransitions );
					break;
				case HoverStates.Idle:
				default:
					VisualStateManager.GoToState( this, "Idle", useTransitions );
					break;
			}
		}

		private static void VisualDataChanged( DependencyObject d, DependencyPropertyChangedEventArgs e ) { }

		private static void OnStateChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			( ( LSBookItem ) d ).UpdateVisualState( true );
		}

		private static void ChangeState( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			( ( LSBookItem ) d ).State = ( ( bool ) e.NewValue ) ? HoverStates.Loading : HoverStates.Idle;
		}
	}
}