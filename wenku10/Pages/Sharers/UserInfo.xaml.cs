using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;

using wenku8.AdvDM;
using wenku8.CompositeElement;
using wenku8.Model.Interfaces;
using wenku8.Model.REST;
using wenku8.Resources;

namespace wenku10.Pages.Sharers
{
    sealed partial class UserInfo : Page, ICmdControls
    {
        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get; }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get ; private set; }

        private RuntimeCache RCache;

        private string CurrentDispName;

        public UserInfo()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void SetTemplate()
        {
            InitAppBar();

            RCache = new RuntimeCache();

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.MyProfile()
                , ( e, id ) =>
                {
                    try
                    {
                        JsonObject JDef = JsonStatus.Parse( e.ResponseString );
                        JsonObject JData = JDef.GetNamedObject( "data" );
                        var j = Dispatcher.RunIdleAsync( ( x ) => SetProfileData( JData ) );
                    }
                    catch ( Exception ex )
                    {
                        ShowErrorMessage( ex.Message );
                    }
                    MarkIdle();
                }
                , ( a, b, ex ) =>
                {
                    ShowErrorMessage( ex.Message );
                    MarkIdle();
                }
                , false
            );
        }

        private void InitAppBar()
        {
            StringResources stx = new StringResources( "Settings", "Message", "ContextMenu" );
            AppBarButton LogoutBtn = UIAliases.CreateAppBarBtn( SegoeMDL2.ChevronLeft, stx.Text( "Account_Logout" ) );
            LogoutBtn.Click += async ( s, e ) =>
            {
                bool Yes = false;
                await Popups.ShowDialog( UIAliases.CreateDialog(
                    stx.Str( "ConfirmLogout", "Message" )
                    , () => Yes = true
                    , stx.Str( "Yes", "Message" ), stx.Str( "No", "Message" )
                ) );

                if ( Yes )
                {
                    ControlFrame.Instance.CommandMgr.SHLogout();
                    ControlFrame.Instance.GoBack();
                    ControlFrame.Instance.BackStack.Remove( PageId.SH_USER_INFO );
                }
            };

            SecondaryIconButton ManageAuth = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Manage, stx.Text( "ManageAuths", "ContextMenu" ) );
            ManageAuth.Click += ( s, e ) => ControlFrame.Instance.SubNavigateTo( this, () => new ManageAuth() );

            MajorControls = new ICommandBarElement[] { LogoutBtn };
            Major2ndControls = new ICommandBarElement[] { ManageAuth };
        }

        private void SetProfileData( JsonObject JData )
        {
            CurrentDispName = JData.GetNamedString( "display_name" );
            DisplayName.Text = CurrentDispName;
        }

        private void DispNameEnter( object sender, KeyRoutedEventArgs e )
        {
            if ( e.Key == Windows.System.VirtualKey.Enter )
            {
                Focus( FocusState.Pointer );
            }
        }

        private void DispNameLostFocus( object sender, RoutedEventArgs e )
        {
            SubmitDispName();
        }

        private void SubmitDispName()
        {
            string NewDispName = DisplayName.Text.Trim();
            if ( NewDispName == CurrentDispName ) return;

            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.EditProfile( NewDispName )
                , ( e, id ) =>
                {
                    try
                    {
                        JsonObject JDef = JsonStatus.Parse( e.ResponseString );
                    }
                    catch ( Exception ex )
                    {
                        ShowErrorMessage( ex.Message );
                    }
                    MarkIdle();
                }
                , ( a, b, ex ) =>
                {
                    ShowErrorMessage( ex.Message );
                    MarkIdle();
                }
                , false
            );

            MarkBusy();
        }

        private void ShowErrorMessage( string Mesg )
        {
            var j = Dispatcher.RunIdleAsync( ( x ) => ErrorMessage.Text = Mesg );
        }

        private void MarkIdle()
        {
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                DisplayName.IsEnabled = true;
                LoadingRing.IsActive = false;
            } );
        }

        private void MarkBusy()
        {
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                DisplayName.IsEnabled = false;

                LoadingRing.IsActive = true;
            } );
        }

        private async void ChangePassword( object sender, RoutedEventArgs e )
        {
            await Popups.ShowDialog( new Dialogs.Sharers.ChangePassword() );
        }
    }
}