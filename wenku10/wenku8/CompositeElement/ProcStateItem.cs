using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace wenku8.CompositeElement
{
	[TemplateVisualState( GroupName = HoverStatesGroup, Name = "Loading" )]
	[TemplateVisualState( GroupName = HoverStatesGroup, Name = "Idle" )]
	public sealed class ProcStateItem : Control
	{
		public static readonly string ID = typeof( ProcStateItem ).Name;

		private const string HoverStatesGroup = "HoverStates";

		public static readonly DependencyProperty PSizeProperty = DependencyProperty.Register( "PSize", typeof( double ), typeof( ProcStateItem ), new PropertyMetadata( 10, VisualDataChanged ) );
		public static readonly DependencyProperty ProcColorProperty = DependencyProperty.Register( "ProcColor", typeof( Brush ), typeof( ProcStateItem ), new PropertyMetadata( null, VisualDataChanged ) );
		public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register( "IsLoading", typeof( bool ), typeof( ProcStateItem ), new PropertyMetadata( false, ChangeState ) );

		public static readonly DependencyProperty StateProperty = DependencyProperty.Register( "State", typeof( HoverStates ), typeof( ProcStateItem ), new PropertyMetadata( HoverStates.Idle, OnStateChanged ) );

		public double PSize
		{
			get { return ( double ) GetValue( PSizeProperty ); }
			set { SetValue( PSizeProperty, value ); }
		}

		public bool IsLoading
		{
			get { return ( bool ) GetValue( IsLoadingProperty ); }
			set { SetValue( IsLoadingProperty, value ); }
		}

		public Brush ProcColor
		{ 
			get { return ( Brush ) GetValue( ProcColorProperty ); }
			set { SetValue( ProcColorProperty, value ); }
		}

		public HoverStates State
		{
			get { return ( HoverStates ) GetValue( StateProperty ); }
			set { SetValue( StateProperty, value ); }
		}

		public ProcStateItem()
		{
			DefaultStyleKey = typeof( ProcStateItem );
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
			( ( ProcStateItem ) d ).UpdateVisualState( true );
		}

		private static void ChangeState( DependencyObject d, DependencyPropertyChangedEventArgs e )
		{
			( ( ProcStateItem ) d ).State = ( ( bool ) e.NewValue ) ? HoverStates.Loading : HoverStates.Idle;
		}
	}
}