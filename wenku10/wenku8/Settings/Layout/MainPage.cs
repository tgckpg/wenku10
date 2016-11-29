using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

namespace wenku8.Settings.Layout
{
    using Ext;
    using Model.ListItem;

    class MainPage : ActiveData, IMainPageSettings
    {
        private const string TFileName = FileLinks.ROOT_SETTING + FileLinks.LAYOUT_MAINPAGE;
        private const string EN_CUSTOM = "CustomSection";
        private const string EN_SPICKS = "StaffPicks";

        private XRegistry LayoutSettings;

        #region Section Definitions
        private static readonly Dictionary<string, Tuple<Type, string>> SectionDefs = new Dictionary<string, Tuple<Type, string>>()
        {
            {
                "NewestEntries"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.WNavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_NEW_ARRIVALS" )
                )
            },
            {
                "RecentUpdate"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.WNavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_RECENT_UPDATE" )
                )
            },
            {
                "TopList_DDigest"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.WNavList )
                    ,  X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_DDigest" )
                )
            },
            {
                "TopList_HITs"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.WNavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_HITs" )
                )
            },
            {
                "TopList_WDigest"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.WNavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_WDigest" )
                )
            },
            {
                "TopList_Favourite"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.WNavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_Favourite" )
                )
            },
            {
                "Finished"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.WNavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_FIN" )
                )
            },
        };
        #endregion

        private IEnumerable<ActiveItem> _secList = null;
        // The Param of Selected Section
        private XParameter WSSec
        {
            get { return Customs.First( ( x ) => x.GetBool( "custom" ) ); }
        }
        private IList<XParameter> Customs
        {
            get { return LayoutSettings.Parameters( "custom" ); }
        }

        public ActiveItem SelectedSection
        {
            get
            {
                ActiveItem Item = SectionList.First( ( x ) => x.Payload == WSSec.Id );
                return new SubtleUpdateItem( Item.Name, Item.Desc, Item.Payload, PayloadCommand( Item.Payload ).Item2 );
            }
        }

        public IEnumerable<ActiveItem> SectionList
        {
            get
            {
                if ( _secList == null )
                {
                    List<ActiveItem> Items = new List<ActiveItem>();

                    StringResources stx = new StringResources( "NavigationTitles" );
                    foreach ( string Key in SectionDefs.Keys )
                    {
                        Items.Add( new ActiveItem(
                            stx.Text( Key )
                            , stx.Text( "Desc_" + Key )
                            , Key
                        ) );
                    }
                    _secList = Items;
                }

                return _secList;
            }
        }

        public MainPage()
        {
			LayoutSettings = new XRegistry( AppKeys.TS_CXML, TFileName );
            InitParams();
        }

        public void InitParams()
        {
            bool Changed = false;
            XParameter SectionKey;
            // Create Item if not availble
            foreach ( string Key in SectionDefs.Keys )
            {
                SectionKey = LayoutSettings.Parameter( Key );
                if ( SectionKey == null )
                {
                    LayoutSettings.SetParameter( Key, new XKey( "custom", false ) );
                    Changed = true;
                }
            }

            SectionKey = LayoutSettings.Parameters().FirstOrDefault(
                ( x ) => x.GetBool( "custom" )
            );

            if ( SectionKey == null )
            {
                LayoutSettings.SetParameter(
                    "NewestEntries", new XKey( "custom", true )
                );
                LayoutSettings.SetParameter( EN_CUSTOM, new XKey( "enable", true ) );
                LayoutSettings.SetParameter( EN_SPICKS, new XKey( "enable", true ) );
                Changed = true;
            }

            if ( Changed ) LayoutSettings.Save();
        }

        public bool ChangeCustSection( ActiveItem A )
        {
            if( A.Payload == SelectedSection.Desc2 )
                return false;

            foreach( XParameter Param in Customs )
            {
                Param.SetValue( new XKey( "custom", Param.Id == A.Payload ) );
                LayoutSettings.SetParameter( Param );
            }

            LayoutSettings.Save();

            return true;
        }

        public Tuple<Type, string> PayloadCommand( string Payload )
        {
            return SectionDefs[ Payload ];
        }

        public IEnumerable<SubtleUpdateItem> NavSections()
        {
            IEnumerable<XParameter> Params = Customs.Where( ( x ) => !x.GetBool( "custom" ) );

            List<SubtleUpdateItem> Secs = new List<SubtleUpdateItem>();
            StringResources stx = new StringResources( "NavigationTitles" );
            foreach ( XParameter Param in Params )
            {
                if ( !SectionDefs.ContainsKey( Param.Id ) ) continue;
                Tuple<Type, string> Def = SectionDefs[ Param.Id ];
                Secs.Add(
                    new SubtleUpdateItem(
                        stx.Text( Param.Id ), stx.Text( "Desc_" + Param.Id )
                        , Def.Item1 , Def.Item2
                    )
                );
            }

            return Secs;
        }
    }

}