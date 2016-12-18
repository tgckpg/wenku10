using System;
using System.Collections.Generic;
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
using wenku8.Model.Interfaces;
using wenku8.Model.Section;
using wenku8.Model.ListItem;
using wenku8.Model.Pages;
using wenku8.Model.Book;
using BInfConfig = wenku8.Settings.Layout.BookInfoView;

namespace wenku10.Pages
{
    public sealed partial class History : Page, ICmdControls, IAnimaPage, INavPage
    {
        public static readonly string ID = typeof( History ).Name;

        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get { return true; } }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get; private set; }

        private HistorySection HistoryContext;

        private string ActiveId = "-1";

        private volatile bool Locked = false;

        public History()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        #region Anima
        Storyboard AnimaStory = new Storyboard();

        public async Task EnterAnima()
        {
            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, HistoryView, "Opacity", 0, 1 );
            SimpleStory.DoubleAnimation( AnimaStory, HistoryView.RenderTransform, "Y", 30, 0 );

            AnimaStory.Begin();
            await Task.Delay( 350 );
        }

        public async Task ExitAnima()
        {
            AnimaStory.Stop();
            AnimaStory.Children.Clear();

            SimpleStory.DoubleAnimation( AnimaStory, HistoryView, "Opacity", 1, 0 );
            SimpleStory.DoubleAnimation( AnimaStory, HistoryView.RenderTransform, "Y", 0, 30 );

            AnimaStory.Begin();
            await Task.Delay( 350 );
        }
        #endregion

        public void SoftOpen() {}
        public void SoftClose() { MessageBus.OnDelivery -= MessageBus_OnDelivery; }

        private void MessageBus_OnDelivery( Message Mesg )
        {
            if( ActiveId.Equals( Mesg.Payload ) )
            {
                LoadingMessage.Text = Mesg.Content;
            }
        }

        private void SetTemplate()
        {
            HistoryView.RenderTransform = new TranslateTransform();

            HistoryContext = new HistorySection();
            HistoryView.DataContext = HistoryContext;
            HistoryContext.Load();
        }

        private async void History_ItemClick( object sender, ItemClickEventArgs e )
        {
            if ( Locked ) return;
            Locked = true;

            ActiveItem Item = ( ActiveItem ) e.ClickedItem;
            ActiveId = Item.Payload;

            MessageBus.OnDelivery += MessageBus_OnDelivery;
            BookItem Book = await ItemProcessor.GetBookFromId( Item.Payload );

            if ( Book == null )
            {
                StringResources stx = new StringResources( "Message" );
                await Popups.ShowDialog(
                    UIAliases.CreateDialog( "Item source has either been lost or deleted", "Item" )
                );

                Locked = false;
                return;
            }

            if ( Book.IsLocal() )
            {
                ControlFrame.Instance.SubNavigateTo( this, () => new BInfConfig().HorizontalTOC ? new TOCViewHorz( Book ) : ( Page ) new TOCViewVert( Book ) );
            }
            else
            {
                ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( Book ) );
            }

            MessageBus.OnDelivery -= MessageBus_OnDelivery;
            LoadingMessage.Text = "";
            Locked = false;
        }

        private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
        {
            HistoryContext.SearchTerm = sender.Text.Trim();
        }
    }
}