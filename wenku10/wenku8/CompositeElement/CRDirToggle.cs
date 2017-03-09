using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.UI.Icons;

namespace wenku8.CompositeElement
{
	using Resources;
	using Settings.Layout;

	sealed class CRDirToggle : AppBarButton
	{
		private int DirMode;
		private int InitMode;

		private ContentReader CRSettings;

		TextBlock ArrowIcon { get; set; }
		TextBlock AlignIcon { get; set; }

		CompositeTransform AlignTransform;

		public CRDirToggle()
			: base()
		{
			DefaultStyleKey = typeof( CRDirToggle );
			CRSettings = new ContentReader();
		}

		private void SetDirection()
		{
			if ( CRSettings.IsHorizontal )
			{
				if ( CRSettings.IsRightToLeft )
				{
					InitMode = 0;

					ArrowIcon.Text = SegoeMDL2.LeftArrowKeyTime0;
					ArrowIcon.TextAlignment = TextAlignment.Right;
					AlignTransform.Rotation = 90;
				}
				else
				{
					InitMode = 1;

					ArrowIcon.TextAlignment = TextAlignment.Left;
					ArrowIcon.Text = SegoeMDL2.RightArrowKeyTime0;
					AlignTransform.Rotation = 270;
					AlignTransform.ScaleX = -1;
				}

				ArrowIcon.VerticalAlignment = VerticalAlignment.Bottom;
			}
			else
			{
				AlignTransform.Rotation = 0;
				AlignTransform.ScaleX = 1;

				InitMode = 2;

				ArrowIcon.Text = SegoeMDL2.Down;
				ArrowIcon.TextAlignment = TextAlignment.Left;
				ArrowIcon.VerticalAlignment = VerticalAlignment.Top;
			}
		}

		public void ToggleDirection()
		{
			switch ( ++DirMode )
			{
				case 0:
					CRSettings.IsHorizontal = true;
					CRSettings.IsRightToLeft = true;
					break;
				case 1:
					CRSettings.IsHorizontal = true;
					CRSettings.IsRightToLeft = false;
					break;
				case 2:
					CRSettings.IsHorizontal = false;
					CRSettings.IsRightToLeft = false;
					break;
				default:
					DirMode = 0;
					goto case 0;
			}

			SetDirection();
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			ArrowIcon = ( TextBlock ) GetTemplateChild( "ArrowIcon" );
			AlignIcon = ( TextBlock ) GetTemplateChild( "AlignIcon" );

			AlignIcon.Text = SegoeMDL2.List;

			AlignTransform = new CompositeTransform();
			AlignIcon.RenderTransform = AlignTransform;

			AlignTransform.CenterX = 0.5 * AlignIcon.Width;
			AlignTransform.CenterY = 0.5 * AlignIcon.Height;

			SetDirection();
			DirMode = InitMode;

			Click += ( s, e ) => ToggleDirection();
		}

	}
}