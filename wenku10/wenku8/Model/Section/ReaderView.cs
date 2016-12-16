using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;

namespace wenku8.Model.Section
{
    using Book;
    using Config;
    using ListItem;
    using Loaders;
    using Settings;
    using Storage;
    using System;
    using Text;

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

        public FlowDirection FlowDir
        {
            get
            {
                return Settings.IsRightToLeft
                    ? FlowDirection.RightToLeft
                    : FlowDirection.LeftToRight
                    ;
            }
        }

        public Thickness Margin
        {
            get
            {
                return Settings.IsHorizontal
                    ? new Thickness( 0, 10, 0, 10 )
                    : new Thickness( 10, 0, 10, 0 );
            }
        }

        public string AlignMode
        {
            get
            {
                return Settings.IsHorizontal
                    ? "ContentReaderListViewHorizontal"
                    : "ContentReaderListViewVertical";
            }
        }

        public Action OnComplete { get; private set; }

        private BookStorage BS = new BookStorage();
        private AutoAnchor Anchors;
        private ChapterLoader CL;
        private Chapter BindChapter;
        private Paragraph Selected;

        private int AutoAnchorOvd = -1;

        public ReaderView( BookItem B, Chapter C )
            :this()
        {
            BindChapter = C;
            Anchors = new AutoAnchor( B );
            CL = new ChapterLoader( B, SetContent );
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

        /// <summary>
        /// For Use in Settings
        /// </summary>
        public ReaderView()
        {
            Settings = new Settings.Layout.ContentReader();

            AppSettings.PropertyChanged += AppSettings_PropertyChanged;
            InitParams();
        }

        ~ReaderView() { Dispose(); }

        private void InitParams()
        {
            TemplateSelector = new Converters.ParaTemplateSelector();
            TemplateSelector.IsHorizontal = Settings.IsHorizontal;

            Paragraph.SetHorizontal( Settings.IsHorizontal );
        }

        public void Load( bool Cache = true )
        {
            CL.Load( BindChapter, Cache );
        }

        private void SetContent( Chapter C )
        {
            Data = C.GetParagraphs();
            ApplyCustomAnchors( C.cid, Data );

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
                foreach( Chapter C in Vol.ChapterList )
                {
                    IEnumerable<XParameter> Params = Anchors.GetCustomAncs( C.cid );
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
            Anchors.RemoveCustomAnc( flyoutTargetItem.GetChapter().cid, index );
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
                    index = Anchors.GetAutoChAnc( BindChapter.cid );
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
                Anchors.SaveAutoChAnc( BindChapter.cid, Data.IndexOf( P ) );
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
            BS.BookRead( BindChapter.aid );

            if( AutoBookmark )
            {
                Anchors.SaveAutoVolAnc( BindChapter.cid );
            }
        }

        public void SetCustomAnchor( string Name, Paragraph P )
        {
            Anchors.SetCustomAnc(
                BindChapter.cid
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