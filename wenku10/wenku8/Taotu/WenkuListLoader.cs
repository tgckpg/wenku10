using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.UI.Icons;

using libtaotu.Controls;
using libtaotu.Pages;
using libtaotu.Models.Interfaces;
using libtaotu.Models.Procedure;

using wenku10.Pages.Dialogs.Taotu;

namespace wenku8.Taotu
{
    using Model.Book.Spider;
    using Model.Interfaces;
    using Resources;

    class WenkuListLoader : Procedure, ISubProcedure
    {
        public static readonly string ID = typeof( WenkuListLoader ).Name;

        public bool SubEdit { get; set; }

        public string ItemPattern { get; set; }
        public string ItemParam { get; set; }

        public bool HasSubProcs { get { return ItemProcs.HasProcedures; } }

        protected override Color BgColor { get { return Colors.Crimson; } }
        protected override IconBase Icon { get { return new IconTOC() { AutoScale = true }; } }

        private ProcManager ItemProcs;

        public ProcManager SubProcedures
        {
            get { return ItemProcs; }
            set { throw new InvalidOperationException(); }
        }

        public WenkuListLoader()
            : base( ProcType.LIST )
        {
            ItemProcs = new ProcManager();
        }

        public override async Task Edit()
        {
            await Popups.ShowDialog( new EditProcListLoader( this ) );
            if ( SubEdit )
            {
                MessageBus.Send( typeof( ProceduresPanel ), "SubEdit", this );
            }
        }

        public void SubEditComplete() { SubEdit = false; }

        public void SetProp( string PropName, string Val )
        {
            switch ( PropName )
            {
                case "ItemPattern": ItemPattern = Val; break;
                case "ItemParam": ItemParam = Val; break;
            }
        }

        public override void ReadParam( XParameter Param )
        {
            base.ReadParam( Param );

            ItemPattern = Param.GetValue( "ItemPattern" );
            ItemParam = Param.GetValue( "ItemParam" );

            ItemProcs = new ProcManager( Param.Parameter( "ItemProcs" ) );
        }

        public override XParameter ToXParam()
        {
            XParameter Param = base.ToXParam();
            Param.SetValue( new XKey[]
            {
                new XKey( "ItemPattern", ItemPattern )
                , new XKey( "ItemParam", ItemParam )
            } );

            XParameter EProc = ItemProcs.ToXParam();
            EProc.Id = "ItemProcs";
            Param.SetParameter( EProc );

            return Param;
        }

        public override async Task<ProcConvoy> Run( ProcConvoy Convoy )
        {
            Convoy = await base.Run( Convoy );

            ProcConvoy UsableConvoy;
            if ( !TryGetConvoy(
                out UsableConvoy
                , ( P, C ) =>
                    C.Payload is IEnumerable<IStorageFile>
                    || C.Payload is IEnumerable<string>
                    || C.Payload is IStorageFile
                    || C.Payload is string
                ) ) return Convoy;

            List<BookInstruction> SpItemList = null;

            // Search for the closest Instruction Set
            ProcConvoy SpiderInst = ProcManager.TracePackage(
                Convoy
                , ( P, C ) => C.Payload is IEnumerable<BookInstruction>
            );

            if ( SpiderInst != null )
            {
                SpItemList = ( List<BookInstruction> ) SpiderInst.Payload;
            }

            if ( SpItemList == null )
            {
                SpItemList = new List<BookInstruction>();
            }

            ProcPassThru PPass = new ProcPassThru( new ProcConvoy( this, SpItemList ) );
            ProcConvoy KnownBook = ProcManager.TracePackage( Convoy, ( P, C ) => C.Payload is BookInstruction );

            if ( UsableConvoy.Payload is IEnumerable<IStorageFile> )
            {
                IEnumerable<IStorageFile> ISFs = ( IEnumerable<IStorageFile> ) UsableConvoy.Payload;

                foreach ( IStorageFile ISF in ISFs )
                {
                    string Content = await ISF.ReadString();
                    await SearchBooks( SpItemList, PPass, KnownBook, Content );
                }
            }
            else if ( UsableConvoy.Payload is IEnumerable<string> )
            {
                IEnumerable<string> Contents = ( IEnumerable<string> ) UsableConvoy.Payload;

                foreach ( string Content in Contents )
                {
                    await SearchBooks( SpItemList, PPass, KnownBook, Content );
                }
            }
            else if ( UsableConvoy.Payload is IStorageFile )
            {
                IStorageFile ISF = ( IStorageFile ) UsableConvoy.Payload;

                string Content = await ISF.ReadString();
                await SearchBooks( SpItemList, PPass, KnownBook, Content );
            }
            else // string
            {
                await SearchBooks( SpItemList, PPass, KnownBook, ( string ) UsableConvoy.Payload );
            }

            return new ProcConvoy( this, SpItemList );
        }

        private async Task SearchBooks( List<BookInstruction> ItemList, ProcPassThru PPass, ProcConvoy KnownBook, string Content )
        {
            ProcFind.RegItem RegParam = new ProcFind.RegItem( ItemPattern, ItemParam, true );

            if ( !RegParam.Validate() ) return;

            MatchCollection matches = RegParam.RegExObj.Matches( Content );
            foreach ( Match match in matches )
            {
                if ( HasSubProcs && RegParam.Valid )
                {
                    string FParam = string.Format(
                        RegParam.Format
                        , match.Groups
                            .Cast<Group>()
                            .Select( g => g.Value )
                            .ToArray()
                    );

                    ProcConvoy ItemConvoy = await ItemProcs.CreateSpider().Crawl( new ProcConvoy( PPass, FParam ) );
                    ItemConvoy = ProcManager.TracePackage( ItemConvoy, ( P, C ) => C.Payload is BookInstruction );

                    if ( !( ItemConvoy == null || ItemConvoy == KnownBook ) )
                    {
                        ItemList.Add( ( BookInstruction ) ItemConvoy.Payload );
                    }
                    else
                    {
                        ProcManager.PanelMessage( this, () =>
                        {
                            StringResources stx = new StringResources( "Error" );
                            return stx.Str( "NotABook" );
                        }, LogType.WARNING );
                    }
                }
            }
        }

    }
}