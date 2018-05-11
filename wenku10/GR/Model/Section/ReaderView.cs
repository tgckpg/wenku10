using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Messaging;

namespace GR.Model.Section
{
	using Config;
	using Database.Models;
	using ListItem;
	using Loaders;
	using Settings;
	using Storage;
	using GSystem;
	using Text;

	using BookItem = Book.BookItem;
	using ChapterVModel = Book.ChapterVModel;

	class ReaderView : ActiveData, IDisposable
	{
		private bool AutoBookmark = GRConfig.ContentReader.AutoBookmark;
		private bool AutoAnchor = GRConfig.ContentReader.ReadingAnchor;
		private bool DoubleTap = GRConfig.ContentReader.DoubleTap;

		public bool UsePageClick => !AutoAnchor;
		public bool UseDoubleTap => DoubleTap;

		public Converters.ParaTemplateSelector TemplateSelector { get; set; }

		private static SolidColorBrush TapBrush = new SolidColorBrush( GRConfig.ContentReader.TapBrushColor );
		public Brush BackgroundBrush => new SolidColorBrush( GRConfig.ContentReader.BackgroundColor );

		public IList<Paragraph> Data { get; private set; }
		public Paragraph SelectedData
		{
			get { return Selected; }
			private set
			{
				if ( Selected != null ) Selected.FontColor = null;

				if ( value != null )
					value.FontColor = TapBrush;

				NotifyChanged( "SelectedIndex" );
				Selected = value;
			}
		}

		public IEnumerable<ActiveData> CustomAnchors => GetAnchors();
		public int SelectedIndex => Selected == null ? 0 : Data.IndexOf( SelectedData );

		public FlowDirection FlowDir { get; private set; }
		public Thickness Margin { get; private set; }
		public string AlignMode { get; private set; }

		public Action OnComplete { get; private set; }

		private AutoAnchor Anchors;
		private ChapterLoader CL;
		private Chapter BindChapter;
		private Paragraph Selected;

		private int AutoAnchorOvd = -1;

		/// <summary>
		/// For Use in Settings
		/// </summary>
		public ReaderView()
		{
			GRConfig.ConfigChanged.AddHandler( this, CRConfigChanged );
			InitParams();
		}

		public ReaderView( BookItem B, Chapter C )
			: this()
		{
			BindChapter = C;

			Anchors = new AutoAnchor( B );
			CL = new ChapterLoader( B, SetContent );
			OverrideParams( B );
		}

		public void Dispose()
		{
			try
			{
				CL = null;
				Data = null;
			}
			catch ( Exception ) { }
		}

		~ReaderView() { Dispose(); }

		private void InitParams()
		{
			SetLayout( GRConfig.ContentReader.IsHorizontal, GRConfig.ContentReader.IsRightToLeft );
		}

		private void OverrideParams( BookItem B )
		{
			SetLayout(
				B.Entry.TextLayout.HasFlag( LayoutMethod.VerticalWriting )
				, B.Entry.TextLayout.HasFlag( LayoutMethod.RightToLeft )
			);
		}

		private void SetLayout( bool IsHorz, bool RTL )
		{
			if ( IsHorz )
			{
				AlignMode = "ContentReaderListViewHorizontal";
				Margin = new Thickness( 0, 10, 0, 10 );
			}
			else
			{
				AlignMode = "ContentReaderListViewVertical";
				Margin = new Thickness( 10, 0, 10, 0 );
			}

			TemplateSelector = new Converters.ParaTemplateSelector();
			TemplateSelector.IsHorizontal = IsHorz;
			Paragraph.SetHorizontal( IsHorz );

			if ( RTL )
			{
				FlowDir = FlowDirection.RightToLeft;
			}
			else
			{
				FlowDir = FlowDirection.LeftToRight;
			}
		}

		public void Load( bool Cache = true )
		{
			CL.Load( BindChapter, Cache );
		}

		private void SetContent( Chapter C )
		{
			Data = new ChapterVModel( C ).GetParagraphs();
			ApplyCustomAnchors( C.Meta[ AppKeys.GLOBAL_CID ], Data );

			NotifyChanged( "Data", "SelectedData" );
			SelectedData = GetAutoAnchor();
		}

		private IEnumerable<BookmarkListItem> GetAnchors()
		{
			List<BookmarkListItem> Items = new List<BookmarkListItem>();

			Volume[] Vols = CL.CurrentBook.GetVolumes();

			foreach ( Volume Vol in Vols )
			{
				Items.Add( new BookmarkListItem( Vol ) );
				foreach ( Chapter C in Vol.Chapters )
				{
					IEnumerable<XParameter> Params = Anchors.GetCustomAncs( C.Meta[ AppKeys.GLOBAL_CID ] );
					if ( Params == null ) continue;
					foreach ( XParameter Param in Params )
					{
						Items.Add( new BookmarkListItem( Vol, Param ) );
					}
				}
			}

			return Items;
		}

		internal void RemoveAnchor( BookmarkListItem flyoutTargetItem )
		{
			int index = flyoutTargetItem.AnchorIndex;
			Anchors.RemoveCustomAnc( flyoutTargetItem.GetChapter().Meta[ AppKeys.GLOBAL_CID ], index );
			if ( index < Data.Count() )
			{
				Data[ index ].AnchorColor = null;
			}
			NotifyChanged( "CustomAnchors" );
		}

		/// <summary>
		/// Get Paragraph anchor using auto index for this chapter
		/// </summary>
		public Paragraph GetAutoAnchor()
		{
			if ( Data != null )
			{
				int index = -1;
				if ( AutoAnchor )
				{
					index = Anchors.GetAutoChAnc( BindChapter.Meta[ AppKeys.GLOBAL_CID ] );
				}

				if ( AutoAnchorOvd != -1 )
				{
					index = AutoAnchorOvd;
					AutoAnchorOvd = -1;
				}

				if ( index < Data.Count() && index != -1 )
				{
					return Data[ index ];
				}
			}

			return null;
		}

		public void ApplyCustomAnchor( int anchor )
		{
			AutoAnchorOvd = anchor;
		}

		public void SelectAndAnchor( Paragraph P )
		{
			SelectedData = P;
			if ( AutoAnchor )
			{
				Anchors.SaveAutoChAnc( BindChapter.Meta[ AppKeys.GLOBAL_CID ], Data.IndexOf( P ) );
			}
		}

		public void SelectIndex( int i )
		{
			if ( i < Data.Count() && 0 <= i )
			{
				SelectAndAnchor( Data[ i ] );
			}
		}

		public void AutoVolumeAnchor()
		{
			CL.CurrentBook.Entry.LastAccess = DateTime.Now;
			CL.CurrentBook.SaveInfo();

			if ( AutoBookmark )
			{
				Anchors.SaveAutoVolAnc( BindChapter.Meta[ AppKeys.GLOBAL_CID ] );
			}
		}

		public void SetCustomAnchor( string Name, Paragraph P )
		{
			Anchors.SetCustomAnc(
				BindChapter.Meta[ AppKeys.GLOBAL_CID ]
				, Name
				, Data.IndexOf( P )
				, ThemeManager.ColorString( P.AnchorColor.Color )
			);

			NotifyChanged( "CustomAnchors" );
		}

		private void ApplyCustomAnchors( string cid, IList<Paragraph> data )
		{
			IEnumerable<XParameter> ThisAnchors = Anchors.GetCustomAncs( cid );
			if ( ThisAnchors == null ) return;
			int l = data.Count();
			foreach ( XParameter Anchors in ThisAnchors )
			{
				int Index = int.Parse( Anchors.GetValue( AppKeys.LBS_INDEX ) );
				if ( Index < l )
				{
					Data[ Index ].AnchorColor = new SolidColorBrush(
						ThemeManager.StringColor( Anchors.GetValue( AppKeys.LBS_COLOR ) )
					);
				}
			}
		}

		private void CRConfigChanged( Message Mesg )
		{
			if ( Mesg.TargetType == typeof( Config.Scopes.Conf_ContentReader ) )
			{
				switch ( Mesg.Content )
				{
					case "BackgroundColor":
						NotifyChanged( "BackgroundBrush" );
						break;
					case "TapBrushColor":
						TapBrush = new SolidColorBrush( ( Color ) Mesg.Payload );
						break;
				}
			}
		}

	}
}