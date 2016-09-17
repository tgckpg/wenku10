using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Section
{
    using Resources;
    using Settings;

    sealed class ZoneList : ActiveData
    {
        public static readonly string ID = typeof( ZoneList ).Name;

        public ObservableCollection<ZoneSpider> Zones { get; private set; }

        // For releasing memories
        private ZoneSpider PrevZone = null;
        public ZoneSpider CurrentZone { get; private set; }

        public ZoneList()
        {
            Zones = new ObservableCollection<ZoneSpider>();

            var j = Task.Run( () =>
            {
                string[] StoredZones = Shared.Storage.ListFiles( FileLinks.ROOT_ZSPIDER );
                foreach ( string Zone in StoredZones )
                {
                    try
                    {
                        ReadZone( Shared.Storage.GetString( FileLinks.ROOT_ZSPIDER + Zone ), true );
                    }
                    catch ( Exception ex )
                    {
                        Logger.Log( ID, "Removing faulty zone: " + Zone, LogType.WARNING );
                        Logger.Log( ID, ex.Message, LogType.DEBUG );
                    }
                }
            } );
        }

        public void EnterZone( ZoneSpider ZS )
        {
            CurrentZone = ZS;
            NotifyChanged( "CurrentZone" );

            try
            {
                var j = ZS.Init();
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.WARNING );
                ExitZone();
            }
        }

        public void ExitZone()
        {
            if ( CurrentZone != PrevZone )
            {
                PrevZone?.Reset();
                PrevZone = CurrentZone;
            }

            CurrentZone = null;
            NotifyChanged( "CurrentZone" );
        }

        public async void OpenFile()
        {
            IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
            if ( ISF == null ) return;

            try
            {
                ZoneSpider ZS = ReadZone( await ISF.ReadString() );
                EnterZone( ZS );
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.WARNING );
            }
        }

        private ZoneSpider ReadZone( string ZData, bool Init = false )
        {
            ZoneSpider ZS = new ZoneSpider();
            XRegistry ZDef = new XRegistry( ZData, null, false );

            if ( ZS.Open( ZDef ) )
            {
                // Remove the old Zone
                if ( Init )
                {
                    AddZone( ZS );
                }
                else
                {
                    RemoveZone( Zones.FirstOrDefault( x => x.ZoneId == ZS.ZoneId ) );
                    AddZone( ZS );
                    var j = Task.Run( () => { Shared.Storage.WriteString( ZS.MetaLocation, ZData ); } );
                }

                return ZS;
            }

            return null;
        }

        private void AddZone( ZoneSpider ZS )
        {
            Worker.UIInvoke( () =>
            {
                Zones.Add( ZS );
            } );
        }

        public void RemoveZone( ZoneSpider ZS )
        {
            if ( ZS == null ) return;

            try
            {
                Shared.Storage.DeleteFile( ZS.MetaLocation );
            }
            catch ( Exception ) { }

            Worker.UIInvoke( () =>
            {
                Zones.Remove( ZS );
                ZS.Reset();
            } );
        }
    }
}