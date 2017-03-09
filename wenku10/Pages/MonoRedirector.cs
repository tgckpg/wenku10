using Windows.UI.Xaml.Controls;

using wenku8.Model.Book;

namespace wenku10.Pages
{
	sealed class MonoRedirector : Page
	{
		public void InfoView( BookItem Book )
		{
			var j = Dispatcher.RunIdleAsync( ( x ) =>
			{
				ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( Book ) );
			} );
		}

	}
}