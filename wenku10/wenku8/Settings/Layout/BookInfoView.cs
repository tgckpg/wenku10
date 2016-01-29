using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

using wenku8.Settings.Layout.ModuleThumbnail;
using wenku8.System;

namespace wenku8.Settings.Layout
{
    class BookInfoView
    {
        public static readonly string ID = typeof( BookInfoView ).Name;

        private const string TFileName = FileLinks.ROOT_SETTING + FileLinks.LAYOUT_BOOKINFOVIEW;
        private const string RightToLeft = "RightToLeft";

        public bool IsRightToLeft
        {
            get
            {
                return LayoutSettings.GetParameter( RightToLeft ).GetBool( "enable" );
            }
            set
            {
                LayoutSettings.SetParameter( RightToLeft, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        private ListView Disp = null;
        private XRegistry LayoutSettings;

        private XParameter[] Modules
        {
            get { return LayoutSettings.GetParametersWithKey( "order" ); }
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
            InitParams();
        }

        public BookInfoView( ListView DisplayList )
            :this()
        {
            Disp = DisplayList;
            DisplayList.DragItemsCompleted += OnReorder;
        }

        ~BookInfoView()
        {
            if( Disp != null )
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

            if ( LayoutSettings.GetParameter( RightToLeft ) == null )
            {
                LayoutSettings.SetParameter( RightToLeft, new XKey( "enable", true ) );
            }

            // Create Index Item if not available
            foreach ( Type P in LayoutDefs )
            {
                ThumbnailBase Tb = Activator.CreateInstance( P ) as ThumbnailBase;
                TBInstance.Add( Tb.ModName, Tb );

                XParameter LayoutKey = LayoutSettings.GetParameter( Tb.ModName );
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

        public void SetOrder()
        {
            List<ThumbnailBase> Thumbnails = new List<ThumbnailBase>();

            IEnumerable<XParameter> Params = Modules.OrderBy(
                ( x ) => x.GetSaveInt( "order" )
            );

            foreach( XParameter Param in Params )
            {
                if ( !Param.GetBool( "enable" ) ) continue;

                Disp.Items.Add( TBInstance[ Param.ID ] );
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
                Names.Add( TBInstance[ P.ID ].ViewName );
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
            if ( LayoutSettings.GetParameter( Name ).GetBool( "enable" ) ) return;

            int Index = LayoutSettings.GetParameter( Name ).GetSaveInt( "order" );
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
                        TBInstance[ Param.ID ]
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
            return LayoutSettings.GetParameter( Name ).GetBool( "enable" );
        }

        private void OnReorder( ListViewBase sender, DragItemsCompletedEventArgs args )
        {
            int InsertIdx = 0;
            // Give orders to the enabled first
            foreach( object Inst in Disp.Items )
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

            foreach( XParameter Param in Params )
            {
                Param.SetValue( new XKey( "order", ++InsertIdx ) );
                LayoutSettings.SetParameter( Param );
            }

            LayoutSettings.Save();
        }
    }
}
