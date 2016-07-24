using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

using wenku8.AdvDM;
using wenku8.Model.ListItem;
using wenku8.Model.REST;
using wenku8.Resources;
using CryptAES = wenku8.System.CryptAES;
using AuthManager = wenku8.System.AuthManager;

namespace wenku10.ShHub
{
    public sealed partial class ScriptUpload : Page
    {
        private SpiderBook SelectedBook;
        private AuthManager AuthMgr;
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
            AuthMgr = new AuthManager();
            AuthMgr.PropertyChanged += KeyMgr_PropertyChanged;
            Keys.DataContext = AuthMgr;
            AccessTokens.DataContext = AuthMgr;
        }

        private void KeyMgr_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            switch( e.PropertyName )
            {
                case "SelectedKey":
                    Keys.SelectedItem = AuthMgr.SelectedKey;
                    break;
                case "SelectedToken":
                    AccessTokens.SelectedItem = AuthMgr.SelectedToken;
                    break;
            }
        }

        private void PreSelectKey( object sender, RoutedEventArgs e ) { Keys.SelectedItem = AuthMgr.SelectedKey; }
        private void PreSelectToken( object sender, RoutedEventArgs e ) { AccessTokens.SelectedItem = AuthMgr.SelectedToken; }

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
            string Id = await AuthMgr.ReserveId( Token.Value );

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
                        AuthMgr.AssignTokenId( Token.Key, Id );
                        if ( Crypt != null ) AuthMgr.AssignKeyId( Crypt.Name, Id );

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

        private void ServerMessage( string Mesg )
        {
            Worker.UIInvoke( () => { Message.Text = Mesg; } );
        }

        private void AddKey( object sender, RoutedEventArgs e ) { AuthMgr.NewKey(); }
        private void AddToken( object sender, RoutedEventArgs e ) { AuthMgr.NewAccessToken(); }

        private class ValidationError : Exception { public ValidationError( string Mesg ) : base( Mesg ) { } }
    }
}