using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Settings.Layout
{
    using AdvDM;
    using Effects;
    using Model.Book;
    using ModuleThumbnail;
    using Resources;
    using SrcView = wenku10.Pages.BookInfoView;

    sealed class BookInfoView
    {
        public static readonly string ID = typeof( BookInfoView ).Name;

        private const string TFileName = FileLinks.ROOT_SETTING + FileLinks.LAYOUT_BOOKINFOVIEW;
        private const string RightToLeft = "RightToLeft";
        private const string HrTOCName = "HorizontalTOC";

        private Dictionary<string, BgContext> SectionBgs;

        public bool IsRightToLeft
        {
            get
            {
                return LayoutSettings.Parameter( RightToLeft ).GetBool( "enable" );
            }
            set
            {
                LayoutSettings.SetParameter( RightToLeft, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        public bool HorizontalTOC
        {
            get
            {
                return LayoutSettings.Parameter( HrTOCName ).GetBool( "enable" );
            }
            set
            {
                LayoutSettings.SetParameter( HrTOCName, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        private ListView Disp = null;
        private XRegistry LayoutSettings;

        private XParameter[] Modules
        {
            get { return LayoutSettings.Parameters( "order" ); }
        }

        private Type[] LayoutDefs = new Type[]
        {
            typeof( ModuleThumbnail.InfoView )
            , typeof( ModuleThumbnail.Reviews )
            , typeof(  ModuleThumbnail.TOCView )
        };

        private Dictionary<string, ThumbnailBase> TBInstance;

        public BookInfoView()
        {
            LayoutSettings = new XRegistry( AppKeys.TS_CXML, TFileName );
            SectionBgs = new Dictionary<string, BgContext>();
            InitParams();
        }

        public BookInfoView( ListView DisplayList )
            : this()
        {
            Disp = DisplayList;
            DisplayList.DragItemsCompleted += OnReorder;
        }

        ~BookInfoView()
        {
            if ( Disp != null )
            {
                Net.Astropenguin.Helpers.Worker.UIInvoke(
                    () => Disp.DragItemsCompleted -= OnReorder
                );
            }
        }

        public void InitParams()
        {
            TBInstance = new Dictionary<string, ThumbnailBase>();

            int i = 0;

            bool Changed = false;

            // Get the last available index
            if ( Modules != null )
            {
                foreach ( XParameter P in Modules )
                {
                    int j = int.Parse( P.GetValue( "order" ) );
                    if ( i < j ) i = j;
                }
            }

            if ( LayoutSettings.Parameter( RightToLeft ) == null )
            {
                LayoutSettings.SetParameter(
                    RightToLeft
                    , new XKey( "enable", Shared.LocaleDefaults.Get<bool>( "BookInfoView.IsRightToLeft" ) )
                );
            }

            if ( LayoutSettings.Parameter( HrTOCName ) == null )
            {
                LayoutSettings.SetParameter(
                    HrTOCName
                    , new XKey( "enable", Shared.LocaleDefaults.Get<bool>( "BookInfoView.HorizontalTOC" ) )
                );
            }

            // Create Index Item if not available
            foreach ( Type P in LayoutDefs )
            {
                ThumbnailBase Tb = Activator.CreateInstance( P ) as ThumbnailBase;
                TBInstance.Add( Tb.ModName, Tb );

                XParameter LayoutKey = LayoutSettings.Parameter( Tb.ModName );
                if ( LayoutKey == null )
                {
                    LayoutSettings.SetParameter(
                        Tb.ModName, new XKey[] {
                            new XKey( "order", ++i )
                            , new XKey( "enable", Tb.DefaultValue )
                        }
                    );

                    Changed = true;
                }
            }

            if ( Changed ) LayoutSettings.Save();
        }

        public BgContext GetBgContext( string Section )
        {
            if ( SectionBgs.ContainsKey( Section ) ) return SectionBgs[ Section ];

            BgContext b = new BgContext( LayoutSettings, Section );

            return SectionBgs[ Section ] = b; ;
        }

        public void SetOrder()
        {
            List<ThumbnailBase> Thumbnails = new List<ThumbnailBase>();

            IEnumerable<XParameter> Params = Modules.OrderBy(
                ( x ) => x.GetSaveInt( "order" )
            );

            foreach ( XParameter Param in Params )
            {
                if ( !Param.GetBool( "enable" ) ) continue;

                Disp.Items.Add( TBInstance[ Param.Id ] );
            }
        }

        public List<string> GetViewOrders()
        {
            List<string> Names = new List<string>();
            foreach (
                XParameter P in Modules
                    .Where( ( x ) => x.GetBool( "enable" ) )
                    .OrderBy( ( x ) => x.GetSaveInt( "order" ) )
            ) {
                Names.Add( TBInstance[ P.Id ].ViewName );
            }

            return Names;
        }

        public void Remove( string Name )
        {
            Disp.Items.Remove(
                Disp.Items.First( ( x ) => ( x as ThumbnailBase ).ModName == Name )
            );

            LayoutSettings.SetParameter( Name, new XKey( "enable", false ) );
            LayoutSettings.Save();
        }

        public void Insert( string Name )
        {
            if ( LayoutSettings.Parameter( Name ).GetBool( "enable" ) ) return;

            int Index = LayoutSettings.Parameter( Name ).GetSaveInt( "order" );
            IEnumerable<XParameter> Params = Modules.OrderBy(
                ( x ) => -x.GetSaveInt( "order" )
            );

            int InsertIdx = 0;
            foreach ( XParameter Param in Params )
            {
                if ( !Param.GetBool( "enable" ) ) continue;
                if ( Param.GetSaveInt( "order" ) <= Index )
                {
                    InsertIdx = Disp.Items.IndexOf(
                        TBInstance[ Param.Id ]
                    ) + 1;
                    break;
                }
            }

            Disp.Items.Insert( InsertIdx, TBInstance[ Name ] );

            LayoutSettings.SetParameter( Name, new XKey( "enable", true ) );
            LayoutSettings.Save();
        }

        public bool Toggle( string Name )
        {
            return LayoutSettings.Parameter( Name ).GetBool( "enable" );
        }

        private void OnReorder( ListViewBase sender, DragItemsCompletedEventArgs args )
        {
            int InsertIdx = 0;
            // Give orders to the enabled first
            foreach ( object Inst in Disp.Items )
            {
                ThumbnailBase Inste = ( ThumbnailBase ) Inst;
                Logger.Log( ID, string.Format( "Order: {0} => {1}", InsertIdx, Inste.ModName ), LogType.DEBUG );

                LayoutSettings.SetParameter(
                    Inste.ModName, new XKey( "order", ++InsertIdx )
                );
            }

            // Then the disables
            IEnumerable<XParameter> Params = Modules.Where(
                ( XParameter x ) => !x.GetBool( "enable" )
            );

            foreach ( XParameter Param in Params )
            {
                Param.SetValue( new XKey( "order", ++InsertIdx ) );
                LayoutSettings.SetParameter( Param );
            }

            LayoutSettings.Save();
        }


        /// <summary>
        /// Background Context Object, Controls section backgrounds
        /// </summary>
        internal class BgContext : ActiveData
        {
            XRegistry LayoutSettings;

            private string Current;

            private ImageSource bg, bg2;
            public ImageSource Background
            {
                get { return bg; }
                private set
                {
                    bg = value;
                    NotifyChanged( "Background" );
                }
            }
            public ImageSource Background2
            {
                get { return bg2; }
                private set
                {
                    bg2 = value;
                    NotifyChanged( "Background2" );
                }
            }

            private bool bgs = false, bgs2 = false;
            public bool BGState
            {
                get { return bgs; }
                private set
                {
                    bgs = value;
                    NotifyChanged( "BGState" );
                }
            }
            public bool BGState2
            {
                get { return bgs2; }
                private set
                {
                    bgs2 = value;
                    NotifyChanged( "BGState2" );
                }
            }

            public string Section { get; private set; }
            public string BgType { get { return LayoutSettings.Parameter( Section )?.GetValue( "type" ); } }

            private bool SwState = false;

            public BgContext( XRegistry LayoutSettings, string Section )
            {
                this.LayoutSettings = LayoutSettings;
                this.Section = Section;
            }

            public void Reload()
            {
                LayoutSettings = new XRegistry( "<NaN />", LayoutSettings.Location );
                ApplyBackgrounds();
            }

            public async void ApplyBackgrounds()
            {
                XParameter P = LayoutSettings.Parameter( Section );

                // Default value
                if ( P == null )
                {
                    SetBackground( "System" );
                    return;
                }

                string value = P.GetValue( "value" );
                if ( value == null ) return;

                bool UseDefault = false;

                switch ( P.GetValue( "type" ) )
                {
                    case "None":
                        ApplyImage( null );
                        break;
                    case "Custom":
                        IStorageFolder isf = await AppStorage.FutureAccessList.GetFolderAsync( value );
                        if ( isf == null ) return;

                        // Randomly pick an image
                        string[] Acceptables = new string[] { ".JPG", ".PNG", ".GIF" };
                        IEnumerable<IStorageFile> sfs = await isf.GetFilesAsync();

                        sfs = sfs.TakeWhile( x => Acceptables.Contains( x.FileType.ToUpper() ) );
                        int l = NTimer.RandInt( sfs.Count() );

                        int i = 0;

                        IStorageFile Choice = null;
                        foreach ( IStorageFile f in sfs )
                        {
                            Choice = f;
                            if ( i++ == l ) break;
                        }

                        if ( Choice == null ) return;

                        // Copy this file to temp storage
                        await Choice.CopyAsync(
                            await Shared.Storage.CreateDirFromISOStorage( FileLinks.ROOT_BANNER )
                            , Section + ".image", NameCollisionOption.ReplaceExisting );

                        ApplyImage( FileLinks.ROOT_BANNER + Section + ".image" );
                        break;
                    case "Preset":
                        BookItem B = SrcView.Instance.ThisBook;

                        try
                        {
                            List<string> ImagePaths = new List<string>();
                            foreach ( Volume V in B.GetVolumes() )
                            {
                                foreach ( Chapter C in V.ChapterList )
                                {
                                    if ( C.HasIllustrations )
                                    {
                                        ImagePaths.AddRange(
                                            Shared.Storage.GetString( C.IllustrationPath )
                                            .Split( new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries )
                                        );
                                    }
                                }
                            }

                            if ( 0 < ImagePaths.Count )
                            {
                                string Url = ImagePaths[ NTimer.RandInt( ImagePaths.Count() ) ];
                                TryUseImage( Url );
                            }
                            else
                            {
                                UseDefault = true;
                            }
                        }
                        catch ( Exception ex )
                        {
                            Logger.Log( ID, ex.Message, LogType.ERROR );
                        }
                        break;
                    default:
                    case "System":
                        UseDefault = true;
                        break;
                }

                if ( UseDefault )
                {
                    SwapImage( await Image.NewBitmap( new Uri( value, UriKind.Absolute ) ) );
                }
            }

            public async void SetBackground( string type )
            {
                XParameter SecParam = LayoutSettings.Parameter( Section );
                if ( SecParam == null ) SecParam = new XParameter( Section );

                string value = null;
                switch ( type )
                {
                    case "Custom":
                        IStorageFolder Location = await PickDirFromPicLibrary();
                        if ( Location == null ) return;

                        value = SecParam.GetValue( "value" );
                        if ( value == null ) value = Guid.NewGuid().ToString();

                        AppStorage.FutureAccessList.AddOrReplace( value, Location );

                        break;
                    // Preset value fall offs to system as default value
                    case "Preset":
                    case "System":
                        switch ( Section )
                        {
                            case "TOC":
                                value = "ms-appx:///Assets/Samples/BgTOC.jpg";
                                break;
                            case "INFO_VIEW":
                                value = "ms-appx:///Assets/Samples/BgInfoView.jpg";
                                break;
                            case "COMMENTS":
                                value = "ms-appx:///Assets/Samples/BgComments.jpg";
                                break;
                            case "CONTENT_READER":
                                value = "ms-appx:///Assets/Samples/BgContentReader.jpg";
                                break;
                        }

                        break;
                }

                SecParam.SetValue( new XKey[] {
                    new XKey( "type", type )
                    , new XKey( "value", value )
                } );

                LayoutSettings.SetParameter( SecParam );

                ApplyBackgrounds();
                LayoutSettings.Save();
            }

            private async Task<IStorageFolder> PickDirFromPicLibrary()
            {
                return await AppStorage.OpenDirAsync( x =>
                {
                    x.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                    x.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                } );
                ;
            }

            private void SwapImage( BitmapImage b )
            {
                Action<BitmapImage> Front = async x =>
                {
                    if ( BGState = ( x != null ) )
                    {
                        Background = x;
                        BGState2 = false;
                        await Task.Delay( 2500 );
                        Image.Destroy( Background2 );
                    }
                };

                Action<BitmapImage> Back = async x =>
                {
                    if ( BGState2 = ( x != null ) )
                    {
                        Background2 = x;
                        BGState = false;
                        await Task.Delay( 2500 );
                        Image.Destroy( Background );
                    }
                };

                if ( SwState = !SwState ) Back = Front;

                // Show the back
                Back( b );
            }

            private async void TryUseImage( string Url )
            {
                WBackgroundTransfer Transfer = new WBackgroundTransfer();
                Guid id = Guid.Empty;

                Transfer.OnThreadComplete += ( DTheradCompleteArgs DArgs ) =>
                {
                    if( DArgs.Id.Equals( id ) )
                    {
                        ApplyImage( DArgs.FileLocation );
                    }
                };

                string fileName = Url.Substring( Url.LastIndexOf( '/' ) + 1 );
                string imageLocation = FileLinks.ROOT_IMAGE + fileName;

                if ( Shared.Storage.FileExists( imageLocation ) )
                {
                    ApplyImage( imageLocation );
                }
                else
                {
                    id = await Transfer.RegisterImage( Url, imageLocation );
                }
            }

            private async void ApplyImage( string Location )
            {
                BitmapImage b = await Image.NewBitmap();
                b.SetSourceFromUrl( Location );
                SwapImage( b );
            }
        }
    }
}