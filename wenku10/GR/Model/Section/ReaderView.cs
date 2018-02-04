﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;

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
		public bool AutoBookmark = Properties.CONTENTREADER_AUTOBOOKMARK;
		public bool AutoAnchor = Properties.APPEARANCE_CONTENTREADER_ENABLEREADINGANCHOR;
		public bool DoubleTap = Properties.APPEARANCE_CONTENTREADER_ENABLEDOUBLETAP;
		public bool UsePageClick { get { return !AutoAnchor; } }
		public bool UseDoubleTap { get { return DoubleTap; } }

		public Settings.Layout.ContentReader Settings { get; set; }

		public Converters.ParaTemplateSelector TemplateSelector { get; set; }

		public Brush BackgroundBrush
		{
			get
			{
				return new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_BACKGROUND );
			}
		}

		public IList<Paragraph> Data { get; private set; }
		public Paragraph SelectedData
		{
			get { return Selected; }
			private set
			{
				if( Selected != null ) Selected.FontColor = null;

				if( value != null )
					value.FontColor = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_TAPBRUSHCOLOR );

				NotifyChanged( "SelectedIndex" );
				Selected = value;
			}
		}

		public int SelectedIndex
		{
			get { return Selected == null ? 0 : Data.IndexOf( SelectedData ); }
		}

		public IEnumerable<ActiveData> CustomAnchors
		{
			get { return GetAnchors(); }
		}

		public FlowDirection FlowDir { get; private set; }
		public Thickness Margin { get; private set; }
		public string AlignMode { get; private set; }

		public Action OnComplete { get; private set; }

		private BookStorage BS = new BookStorage();
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
			Settings = new Settings.Layout.ContentReader();

			AppSettings.PropertyChanged += AppSettings_PropertyChanged;
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
				AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
				foreach ( Paragraph P in Data ) P.Dispose();
				CL = null;
				BS = null;
				Data = null;
			}
			catch ( Exception ) { }
		}

		~ReaderView() { Dispose(); }

		private void InitParams()
		{
			SetLayout( Settings.IsHorizontal, Settings.IsRightToLeft );
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

			foreach( Volume Vol in Vols )
			{
				Items.Add( new BookmarkListItem( Vol ) );
				foreach ( Chapter C in Vol.Chapters )
				{
					IEnumerable<XParameter> Params = Anchors.GetCustomAncs( C.Meta[ AppKeys.GLOBAL_CID ] );
					if ( Params == null ) continue;
					foreach( XParameter Param in Params )
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
			if( index < Data.Count() )
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
			if( Data != null )
			{
				int index = -1;
				if ( AutoAnchor )
				{
					index = Anchors.GetAutoChAnc( BindChapter.Meta[ AppKeys.GLOBAL_CID ] );
				}

				if( AutoAnchorOvd != -1 )
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
			if( AutoAnchor )
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
			BS.BookRead( BindChapter.Meta[ AppKeys.GLOBAL_CID ] );

			if( AutoBookmark )
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
			foreach( XParameter Anchors in ThisAnchors )
			{
				int Index = int.Parse( Anchors.GetValue( AppKeys.LBS_INDEX ) );
				if( Index < l )
				{
					Data[ Index ].AnchorColor = new SolidColorBrush(
						ThemeManager.StringColor( Anchors.GetValue( AppKeys.LBS_COLOR ) )
					);
				}
			}
		}

		private void AppSettings_PropertyChanged( object sender, global::System.ComponentModel.PropertyChangedEventArgs e )
		{
			switch( e.PropertyName )
			{
				case Parameters.APPEARANCE_CONTENTREADER_BACKGROUND:
					NotifyChanged( "BackgroundBrush" );
					break;
			}
		}

	}
}