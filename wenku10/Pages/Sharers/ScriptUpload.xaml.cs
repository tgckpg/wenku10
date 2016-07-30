using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Logging;

using wenku8.AdvDM;
using wenku8.Model.ListItem;
using wenku8.Model.REST;
using wenku8.Resources;
using CryptAES = wenku8.System.CryptAES;
using AESManager = wenku8.System.AESManager;
using TokenManager = wenku8.System.TokenManager;

namespace wenku10.Pages.Sharers
{
    public sealed partial class ScriptUpload : Page
    {
        public static readonly string ID = typeof( ScriptUpload ).Name;

        private SpiderBook SelectedBook;
        private AESManager AESMgr;
        private TokenManager TokMgr;
        private Action<string,string> OnExit;

        public ScriptUpload()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        public ScriptUpload( Action<string,string> OnExit )
            :this()
        {
            this.OnExit = OnExit;
        }

        private void SetTemplate()
        {
            AESMgr = new AESManager();
            AESMgr.PropertyChanged += KeyMgr_PropertyChanged;
            TokMgr = new TokenManager();
            TokMgr.PropertyChanged += TokMgr_PropertyChanged;

            Keys.DataContext = AESMgr;
            AccessTokens.DataContext = TokMgr;
        }

        private void KeyMgr_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if( e.PropertyName == "SelectedItem" ) Keys.SelectedItem = AESMgr.SelectedItem;
        }

        private void TokMgr_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if( e.PropertyName == "SelectedItem" ) AccessTokens.SelectedItem = TokMgr.SelectedItem;
        }

        private void PreSelectKey( object sender, RoutedEventArgs e ) { Keys.SelectedItem = AESMgr.SelectedItem; }
        private void PreSelectToken( object sender, RoutedEventArgs e ) { AccessTokens.SelectedItem = TokMgr.SelectedItem; }

        private async void Upload( object sender, RoutedEventArgs e )
        {
            CryptAES Crypt = null;

            // Validate inputs
            try
            {
                Message.Text = "";

                if( SelectedBook == null )
                    throw new ValidationError( "No book seleceted" );

                if ( Encrypt.IsChecked == true )
                {
                    Crypt = Keys.SelectedItem as CryptAES;

                    if ( Crypt == null )
                        throw new ValidationError( "Please select a key first" );
                }

                if( AccessTokens.SelectedItem == null )
                    throw new ValidationError( "You need an access token to upload this script" );
            }
            catch( ValidationError ex )
            {
                Message.Text = ex.Message;
                return;
            }

            // Check whether the script uuid is reserved
            KeyValuePair<string, string> Token = ( KeyValuePair<string, string> ) AccessTokens.SelectedItem;
            string Id = await ReserveId( Token.Value );

            string Name = NameInput.Text.Trim();
            if ( string.IsNullOrEmpty( Name ) )
                Name = NameInput.PlaceholderText;

            string Desc = DescInput.Text.Trim();
            if ( string.IsNullOrEmpty( Id ) )
            {
                Message.Text = "Failed to reserve id";
                return;
            }

            string Zone = ZoneInput.Text;
            string[] Types = TypesInput.Text.Split( ',' );
            string[] Tags = TagsInput.Text.Split( ',' );

            SelectedBook.AssignId( Id );
            string Data = SelectedBook.PSettings.ToString();

            if ( Crypt != null ) Data = Crypt.Encrypt( Data );

            RuntimeCache RCache = new RuntimeCache();
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ScriptUpload(
                    Token.Value, Id
                    , Data, Name, Desc
                    , Zone, Types, Tags
                    , Encrypt.IsChecked == true
                    , ForceCommentEnc.IsChecked == true
                    , Anon.IsChecked == true )
                , ( Res, QueryId ) =>
                {
                    try
                    {
                        JsonStatus.Parse( Res.ResponseString );
                        TokMgr.AssignId( Token.Key, Id );
                        if ( Crypt != null ) AESMgr.AssignId( Crypt.Name, Id );

                        Worker.UIInvoke( () => { OnExit( Id, Token.Value ); } );
                    }
                    catch ( Exception ex )
                    {
                        ServerMessage( ex.Message );
                    }
                }
                , ( c1, c2, ex ) => ServerMessage( ex.Message )
                , false
            );
        }

        private async void PickFile( object sender, RoutedEventArgs e )
        {
            IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
            if ( ISF == null ) return;

            LoadingRing.IsActive = true;

            try
            {
                SelectedBook = await SpiderBook.CreateAsnyc( await ISF.ReadString(), true );
                FileName.Text = ISF.Name;
                int LDot = ISF.Name.LastIndexOf( '.' );
                NameInput.PlaceholderText = ~LDot == 0 ? ISF.Name : ISF.Name.Substring( 0, LDot );
            }
            catch ( Exception ex )
            {
                Message.Text = ex.Message;
            }

            LoadingRing.IsActive = false;
        }

        public async Task<string> ReserveId( string AccessToken )
        {
            TaskCompletionSource<string> TCS = new TaskCompletionSource<string>();

            RuntimeCache RCache = new RuntimeCache();
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ReserveId( AccessToken )
                , ( e, QueryId ) =>
                {
                    try
                    {
                        JsonObject JDef = JsonStatus.Parse( e.ResponseString );
                        string Id = JDef.GetNamedString( "data" );
                        TCS.SetResult( Id );
                    }
                    catch( Exception ex )
                    {
                        Logger.Log( ID, ex.Message, LogType.WARNING );
                        TCS.SetCanceled();
                    }
                }
                , ( cache, Id, ex ) =>
                {
                    Logger.Log( ID, ex.Message, LogType.WARNING );
                    TCS.SetCanceled();
                }
                , false
            );

            return await TCS.Task;
        }

        private void ServerMessage( string Mesg )
        {
            Worker.UIInvoke( () => { Message.Text = Mesg; } );
        }

        private void AddKey( object sender, RoutedEventArgs e ) { AESMgr.NewAuth(); }
        private void AddToken( object sender, RoutedEventArgs e ) { TokMgr.NewAuth(); }

        private class ValidationError : Exception { public ValidationError( string Mesg ) : base( Mesg ) { } }
    }
}