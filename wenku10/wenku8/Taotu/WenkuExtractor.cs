using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

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
                MessageBus.Send( new Message( typeof( ProceduresPanel ), "SubEdit", this ) );
            }
        }

        public override async Task<ProcConvoy> Run( ProcConvoy Convoy )
        {
            string LoadUrl = TargetUrl;

            IStorageFile ISF = null;
            if ( Incoming )
            {
                ProcManager.PanelMessage( this, "Checking Incoming Data", LogType.INFO );

                ProcConvoy UsableConvoy = ProcManager.TracePackage(
                    Convoy, ( P, C ) =>
                    {
                        return C.Payload is IEnumerable<string>
                        || C.Payload is string
                        || C.Payload is IEnumerable<IStorageFile>;
                    }
                );

                if ( UsableConvoy != null )
                {
                    if ( UsableConvoy.Payload is string )
                    {
                        LoadUrl = UsableConvoy.Payload as string;
                    }
                    else if ( UsableConvoy.Payload is IEnumerable<string> )
                    {
                        LoadUrl = ( UsableConvoy.Payload as IEnumerable<string> ).FirstOrDefault();
                    }
                    else // IEnumerable StorageFIle
                    {
                        ISF = ( UsableConvoy.Payload as IEnumerable<IStorageFile> ).FirstOrDefault();
                    }

                    if( !string.IsNullOrEmpty( LoadUrl ) )
                    {
                        LoadUrl = WebUtility.HtmlDecode( LoadUrl );
                    }
                }
            }

            if( ISF == null && string.IsNullOrEmpty( LoadUrl ) )
            {
                ProcManager.PanelMessage( this, "No usable convoy found, Skipping", LogType.WARNING );
                return Convoy;
            }

            ProcConvoy BookConvoy = ProcManager.TracePackage(
                Convoy, ( D, C ) => C.Payload is BookItem
            );

            BookItem BookInst = ( BookConvoy == null )
                ? new BookInstruction()
                : ( BookConvoy.Payload as BookItem )
                ;

            if( !string.IsNullOrEmpty( LoadUrl ) )
            {
                BookInst.ReadParam( AppKeys.BINF_ORGURL, LoadUrl );
            }

            if( ISF == null )
            {
                ISF = await ProceduralSpider.DownloadSource( LoadUrl );
            }

            await ExtractProps( BookInst, await ISF.ReadString() );

            return new ProcConvoy( this, BookInst );
        }

        private async Task ExtractProps( BookItem Inst, string Content )
        {
            foreach( PropExt Extr in PropDefs )
            {
                if ( !Extr.Enabled ) continue;

                string PropValue = MatchSingle( Extr, Content );
                if ( string.IsNullOrEmpty( PropValue ) ) continue;

                if( Extr.SubProc.HasProcedures )
                {
                    ProcManager.PanelMessage( this, "Running Sub procedures", LogType.INFO );
                    ProcPassThru PPass = new ProcPassThru( new ProcConvoy( this, Inst ) );
                    await Extr.SubProc.CreateSpider().Crawl( new ProcConvoy( PPass, PropValue ) );
                }
                else
                {
                    // If the website split a single property into serveral pages
                    // That website is stupid. Would not support.
                    if( !Inst.ReadParam( Extr.PType.ToString(), PropValue.ToCTrad() ) )
                    {
                        ProcManager.PanelMessage( this, "Invalid param: " + Extr.PType.ToString(), LogType.WARNING );
                    }
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

            XParameter[] ExtParams = Param.GetParametersWithKey( "i" );
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
                ExtParam.ID += i;
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

                XParameter Sub = Param.GetParameter( "SubProc" );
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
                SubParam.ID = "SubProc";
                Param.SetParameter( SubParam );

                return Param;
            }
        }
    }
}
