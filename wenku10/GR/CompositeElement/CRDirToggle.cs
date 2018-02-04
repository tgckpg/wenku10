using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace GR.CompositeElement
{
	using GR.Database.Models;
	using GR.Model.Book;
	using Resources;

	sealed class CRDirToggle : AppBarButton
	{
		private int DirMode;
		private int InitMode;

		TextBlock ArrowIcon { get; set; }
		TextBlock AlignIcon { get; set; }

		private BookItem Bk;

		CompositeTransform AlignTransform;

		public Action OnToggle { get; set; }

		public CRDirToggle( BookItem Bk )
			: base()
		{
			DefaultStyleKey = typeof( CRDirToggle );
			this.Bk = Bk;
		}

		private void SetDirection()
		{
			if ( Bk.Entry.TextLayout.HasFlag( LayoutMethod.VerticalWriting ) )
			{
				if ( Bk.Entry.TextLayout.HasFlag( LayoutMethod.RightToLeft ) )
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

		private void ToggleDirection()
		{
			switch ( ++DirMode )
			{
				case 0:
					Bk.Entry.TextLayout = LayoutMethod.VerticalWriting | LayoutMethod.RightToLeft;
					break;
				case 1:
					Bk.Entry.TextLayout = LayoutMethod.VerticalWriting;
					break;
				case 2:
					Bk.Entry.TextLayout = 0;
					break;
				default:
					DirMode = 0;
					goto case 0;
			}

			Bk.SaveInfo();
			SetDirection();

			OnToggle?.Invoke();
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