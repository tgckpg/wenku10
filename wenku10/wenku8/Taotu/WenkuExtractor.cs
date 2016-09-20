using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.UI.Icons;

using libtaotu.Controls;
using libtaotu.Crawler;
using libtaotu.Models.Interfaces;
using libtaotu.Models.Procedure;
using libtaotu.Pages;

using wenku10.Pages.Dialogs.Taotu;

namespace wenku8.Taotu
{
    using Model.Book;
    using Model.Book.Spider;
    using Resources;
    using Settings;

    class WenkuExtractor : Procedure, ISubProcedure
    {
        public ObservableCollection<PropExt> PropDefs { get; set; }
        public string TargetUrl { get; internal set; }
        public bool Incoming { get; internal set; }

        public static IEnumerable<GenericData<BookInfo>> PossibleTypes { get; set; }
        public PropExt SubEdit { get; set; }

        public ProcManager SubProcedures
        {
            get { return SubEdit.SubProc; } 
            set { throw new InvalidOperationException(); }
        }

        protected override Color BgColor { get { return Color.FromArgb( 255, 60, 60, 60 ); } }
        protected override IconBase Icon { get { return new IconLogout(){ AutoScale = true, Direction = Direction.Rotate270 }; } }

        public WenkuExtractor()
            : base( ProcType.EXTRACT )
        {
            PossibleTypes = GenericData<BookInfo>.Convert( Enum.GetValues( typeof( BookInfo ) ) );
            PropDefs = new ObservableCollection<PropExt>();
        }

        public override async Task Edit()
        {
            await Popups.ShowDialog( new EditProcExtract( this ) );
            if( SubEdit != null )
            {
                MessageBus.Send( typeof( ProceduresPanel ), "SubEdit", this );
            }
        }

        public override async Task<ProcConvoy> Run( ProcConvoy Convoy )
        {
            await base.Run( Convoy );

            string LoadUrl = TargetUrl;
            string Content = "";

            ProcConvoy UsableConvoy = ProcManager.TracePackage(
                Convoy, ( P, C ) =>
                    C.Payload is IEnumerable<IStorageFile>
                    || C.Payload is IEnumerable<string>
                    || C.Payload is IStorageFile
                    || C.Payload is string
            );

            IStorageFile ISF = null;

            if ( UsableConvoy != null )
            {
                ProcManager.PanelMessage( this, () => Res.RSTR( "IncomingCheck" ), LogType.INFO );

                if ( UsableConvoy.Payload is IEnumerable<IStorageFile> )
                {
                    ISF = ( UsableConvoy.Payload as IEnumerable<IStorageFile> ).FirstOrDefault();
                }
                else if ( UsableConvoy.Payload is IStorageFile )
                {
                    ISF = ( IStorageFile ) UsableConvoy.Payload;
                }

                if ( Incoming )
                {
                    if ( UsableConvoy.Payload is IEnumerable<string> )
                    {
                        LoadUrl = ( UsableConvoy.Payload as IEnumerable<string> ).FirstOrDefault();
                    }
                    else if ( UsableConvoy.Payload is string )
                    {
                        LoadUrl = ( string ) UsableConvoy.Payload;
                    }

                    if ( ISF == null && string.IsNullOrEmpty( LoadUrl ) )
                    {
                        ProcManager.PanelMessage( this, () => Res.RSTR( "NoUsablePayload" ), LogType.WARNING );
                        return Convoy;
                    }

                    if ( !string.IsNullOrEmpty( LoadUrl ) )
                    {
                        LoadUrl = WebUtility.HtmlDecode( LoadUrl );
                    }
                }
                else // Incomings are Content
                {
                    if ( UsableConvoy.Payload is IEnumerable<string> )
                    {
                        Content = string.Join( "\n", ( IEnumerable<string> ) UsableConvoy.Payload );
                    }
                    else if ( UsableConvoy.Payload is string )
                    {
                        Content = ( string ) UsableConvoy.Payload;
                    }
                }
            }

            ProcConvoy BookConvoy = ProcManager.TracePackage( Convoy, ( D, C ) => C.Payload is BookItem );

            BookItem BookInst = ( BookConvoy == null )
                ? new BookInstruction()
                : ( BookConvoy.Payload as BookItem )
                ;

            if ( !string.IsNullOrEmpty( LoadUrl ) )
            {
                BookInst.ReadParam( AppKeys.BINF_ORGURL, LoadUrl );

                if ( string.IsNullOrEmpty( Content ) && ISF == null )
                {
                    ISF = await ProceduralSpider.DownloadSource( LoadUrl );
                }
            }

            if ( ISF != null ) Content = await ISF.ReadString();

            await ExtractProps( BookInst, Content );

            return new ProcConvoy( this, BookInst );
        }

        private async Task ExtractProps( BookItem Inst, string Content )
        {
            foreach( PropExt Extr in PropDefs )
            {
                if ( !Extr.Enabled ) continue;

                string PropValue = MatchSingle( Extr, Content );

                if ( Extr.SubProc.HasProcedures )
                {
                    ProcManager.PanelMessage( this, () => Res.RSTR( "SubProcRun" ), LogType.INFO );
                    ProcPassThru PPass = new ProcPassThru( new ProcConvoy( this, Inst ) );
                    ProcConvoy SubConvoy = await Extr.SubProc.CreateSpider().Crawl( new ProcConvoy( PPass, PropValue ) );

                    // Process ReceivedConvoy
                    if ( SubConvoy.Payload is string )
                        PropValue = ( string ) SubConvoy.Payload;
                    else if ( SubConvoy.Payload is IEnumerable<string> )
                        PropValue = string.Join( "\n", ( IEnumerable<string> ) SubConvoy.Payload );
                    else if ( SubConvoy.Payload is IStorageFile )
                        PropValue = await ( ( IStorageFile ) SubConvoy.Payload ).ReadString();
                    else if ( SubConvoy.Payload is IEnumerable<IStorageFile> )
                        PropValue = await ( ( IEnumerable<IStorageFile> ) SubConvoy.Payload ).First().ReadString();
                    else continue;
                }

                // If the website split a single property into serveral pages
                // That website is stupid. Would not support.
                if( !Inst.ReadParam( Extr.PType.ToString(), PropValue.ToCTrad() ) )
                {
                    ProcManager.PanelMessage( this, () => Res.RSTR( "InvalidParam", Extr.PType ), LogType.WARNING );
                }
            }
        }

        // Match a single item
        public string MatchSingle( PropExt R, string Content )
        {
            // Set the value if patter left empty
            if ( string.IsNullOrEmpty( R.Pattern )
                && !string.IsNullOrEmpty( R.Format )
            ) return R.Format;

            MatchCollection matches = R.RegExObj.Matches( Content );

            string PropValue = "";
            foreach ( Match match in matches )
            {
                PropValue += string.Format(
                    R.Format.Unescape()
                    , match.Groups
                        .Cast<Group>()
                        .Select( g => g.Value )
                        .ToArray()
                );
            }

            return PropValue;
        }

        public override void ReadParam( XParameter Param )
        {
            base.ReadParam( Param );

            TargetUrl = Param.GetValue( "TargetUrl" );
            Incoming = Param.GetBool( "Incoming" );

            XParameter[] ExtParams = Param.Parameters( "i" );
            foreach ( XParameter ExtParam in ExtParams )
            {
                PropDefs.Add( new PropExt( ExtParam ) );
            }
        }

        public override XParameter ToXParam()
        {
            XParameter Param = base.ToXParam();

            Param.SetValue( new XKey[] {
                new XKey( "TargetUrl", TargetUrl )
                , new XKey( "Incoming", Incoming )
            } );

            int i = 0;
            foreach( PropExt Extr in PropDefs )
            {
                XParameter ExtParam = Extr.ToXParam();
                ExtParam.Id += i;
                ExtParam.SetValue( new XKey( "i", i++ ) );

                Param.SetParameter( ExtParam );
            }

            return Param;
        }

        public void SubEditComplete()
        {
            SubEdit = null;
        }

        public class PropExt : ProcFind.RegItem
        {
            public static readonly Type BINF = typeof( BookInfo );

            public ProcManager SubProc { get; set; }
            public bool HasSubProcs { get { return SubProc.HasProcedures; } }

            public override bool Enabled
            {
                get
                {
                    if ( Valid
                        && string.IsNullOrEmpty( Pattern )
                        && !string.IsNullOrEmpty( Format ) )
                        return true;

                    return base.Enabled;
                }

                set { base.Enabled = value; }
            }

            public GenericData<BookInfo> SelectedType 
            {
                get
                {
                    return Types.First( x => x.Data == PType );
                }
            }

            public IEnumerable<GenericData<BookInfo>> Types { get { return PossibleTypes; } }
            public BookInfo PType { get; set; }

            public PropExt( BookInfo PType = BookInfo.Others )
            {
                this.PType = PType;
                this.SubProc = new ProcManager();
                Enabled = true;
            }

            public PropExt( XParameter Param )
                : base( Param )
            {
                string SType = Param.GetValue( "Type" );
                this.SubProc = new ProcManager();

                XParameter Sub = Param.Parameter( "SubProc" );
                if ( Sub != null ) SubProc.ReadParam( Sub );

                PType = Enum.GetValues( BINF )
                    .Cast<BookInfo>()
                    .FirstOrDefault( x => Enum.GetName(  BINF, x ) == SType );
            }

            public override XParameter ToXParam()
            {
                XParameter Param =  base.ToXParam();
                Param.SetValue( new XKey( "Type", PType ) );

                XParameter SubParam = SubProc.ToXParam();
                SubParam.Id = "SubProc";
                Param.SetParameter( SubParam );

                return Param;
            }
        }
    }
}