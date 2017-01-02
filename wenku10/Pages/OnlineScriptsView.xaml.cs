using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Messaging;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Ext;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.Section.SharersHub;
using wenku8.Resources;
using wenku8.Section;
using wenku8.Settings;

namespace wenku10.Pages
{
    using Dialogs;
    using Sharers;
    using SHHub;

    public sealed partial class OnlineScriptsView : Page, ICmdControls, INavPage, IAnimaPage
    {
#pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get { return true; } }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get; private set; }

        private SharersHub SHHub;
        private SHMember Member;
        private AppBarButtonEx ActivyBtn;

        public OnlineScriptsView()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        public void SoftOpen()
        {
            MessageBus.OnDelivery += MessageBus_OnDelivery;
            Member.OnStatusChanged += Member_OnStatusChanged;

            lock ( PendingRemove )
            {
                // Consume items to remove
                foreach ( HubScriptItem HSI in PendingRemove )
                    SHHub.SearchSet.Remove( HSI );

                PendingRemove.Clear();
            }
        }

        public void SoftClose()
        {
            MessageBus.OnDelivery -= MessageBus_OnDelivery;
            Member.OnStatusChanged -= Member_OnStatusChanged;
        }

        #region Anima
        Storyboard AnimaStory = new Storyboard();

        public async Task EnterAnima()
        {
            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1 );
            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 30, 0 );

            AnimaStory.Begin();
            await Task.Delay( 350 );
        }

        public async Task ExitAnima()
        {
            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            LayoutRoot.RenderTransform = new TranslateTransform();

            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0 );
            SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30 );

            AnimaStory.Begin();
            await Task.Delay( 350 );
        }
        #endregion

        private void SetTemplate()
        {
            InitAppBar();

            Member = X.Singleton<SHMember>( XProto.SHMember );

            ActivyList.DataContext = Member;
            ActivyBtn.SetBinding( AppBarButtonEx.CountProperty, new Binding() { Source = Member.Activities, Path = new PropertyPath( "Count" ) } );

            PendingRemove.Clear();
            MessageBus.OnDelivery += StoreItemsToRemove;

            SHHub = new SharersHub();
            SHHub.PropertyChanged += SHHub_PropertyChanged;

            UpdateActivities();

            LayoutRoot.DataContext = SHHub;
            SHHub.Search( "" );

            if ( Member.Status == MemberStatus.RE_LOGIN_NEEDED )
            {
                var j = ControlFrame.Instance.CommandMgr.Authenticate();
            }
        }

        private static HashSet<HubScriptItem> PendingRemove = new HashSet<HubScriptItem>();

        private static void StoreItemsToRemove( Message Mesg )
        {
            if( Mesg.Content == AppKeys.SH_SCRIPT_REMOVE )
            {
                lock ( PendingRemove ) PendingRemove.Add( ( HubScriptItem ) Mesg.Payload );
            }
        }

        private void MessageBus_OnDelivery( Message Mesg )
        {
            switch ( Mesg.Content )
            {
                case AppKeys.SH_SHOW_GRANTS:
                    ControlFrame.Instance.SubNavigateTo( this, () =>
                    {
                        ManageAuth MAuth = new ManageAuth();
                        MAuth.GotoRequests();
                        return MAuth;
                    } );
                    break;
            }
        }

        private void Member_OnStatusChanged( object sender, MemberStatus args ) { UpdateActivities(); }

        private async void UpdateActivities()
        {
            if ( Member.IsLoggedIn )
            {
                ActivyBtn.IsLoading = true;
                await new MyRequests().Get();
                await new MyInbox().Get();
                ActivyBtn.IsLoading = false;
            }
        }

        private void SHHub_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            switch( e.PropertyName )
            {
                case "Loading":
                    ActivyBtn.IsLoading = SHHub.Loading;
                    break;
            }
        }

        private void InitAppBar()
        {
            StringResources stx = new StringResources( "AppResources", "ContextMenu" );

            ActivyBtn = new AppBarButtonEx()
            {
                Icon = new SymbolIcon( Symbol.Message )
                , Label = stx.Text( "Messages" )
                , Foreground = new SolidColorBrush( LayoutSettings.RelativeMajorBackgroundColor )
            };

            ActivyBtn.Click += ToggleActivities;

            SecondaryIconButton UploadBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Upload, stx.Text( "SubmitScript" ) );
            UploadBtn.Click += ( s, e ) => ControlFrame.Instance.SubNavigateTo( this, () => new ScriptUpload( UploadExit ) );

            SecondaryIconButton MAuthBtn = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.Manage, stx.Text( "ManageAuths", "ContextMenu" ) );
            MAuthBtn.Click += ManageAuths;

            MajorControls = new ICommandBarElement[] { ActivyBtn };

#if DEBUG
            StringResources sts = new StringResources( "Settings" );
            SecondaryIconButton ChangeServer = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.DirectAccess, sts.Text( "Advanced_Server" ) );
            ChangeServer.Click += async ( s, e ) =>
            {
                ValueHelpInput VH =  new ValueHelpInput(
                    Shared.ShRequest.Server.ToString()
                    , sts.Text( "Advanced_Server" ), "Address"
                ) ;

                await Popups.ShowDialog( VH );
                if ( VH.Canceled ) return;

                try
                {
                    Shared.ShRequest.Server = new Uri( VH.Value );
                }
                catch ( Exception ) { }
            };

            Major2ndControls = new ICommandBarElement[] { UploadBtn, MAuthBtn, ChangeServer };
#else
            Major2ndControls = new ICommandBarElement[] { UploadBtn, MAuthBtn };
#endif
        }

        private async void ToggleActivities( object sender, RoutedEventArgs e )
        {
            if ( !( await ControlFrame.Instance.CommandMgr.Authenticate() ) ) return;

            if ( Member.Activities.Count == 0 )
            {
                UpdateActivities();
            }
            else
            {
                if ( TransitionDisplay.GetState( ActivyList ) == TransitionState.Active )
                {
                    TransitionDisplay.SetState( ActivyList, TransitionState.Inactive );
                }
                else
                {
                    TransitionDisplay.SetState( ActivyList, TransitionState.Active );
                }
            }
        }

        private void Activities_ItemClick( object sender, ItemClickEventArgs e )
        {
            TransitionDisplay.SetState( ActivyList, TransitionState.Inactive );
            Member.Activities.CheckActivity( ( Activity ) e.ClickedItem );
        }

        private void SearchBox_QuerySubmitted( AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args )
        {
            SHHub.Search( args.QueryText );
        }

        private void HSItemClick( object sender, ItemClickEventArgs e )
        {
            HubScriptItem HSI = ( HubScriptItem ) e.ClickedItem;

            if ( HSI.Faultered )
            {
                // Report to admin
            }
            else
            {
                ControlFrame.Instance.NavigateTo( PageId.SCRIPT_DETAILS, () => new ScriptDetails( HSI ) );
            }
        }

        private void UploadExit( string Id, string AccessToken )
        {
            var j = ControlFrame.Instance.CloseSubView();
            SHHub.Search( "uuid: " + Id, new string[] { AccessToken } );
        }

        private void ManageAuths( object sender, RoutedEventArgs e )
        {
            ControlFrame.Instance.SubNavigateTo( this, () => new ManageAuth() );
        }

    }
}