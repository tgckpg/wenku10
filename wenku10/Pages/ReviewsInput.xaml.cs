using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

using wenku8.Model.Book;
using wenku8.Model.Comments;

namespace wenku10.Pages
{
	sealed partial class ReviewsInput : Page
	{
		private int MinTitleLimit = 4;
		private int MinContentLimit = 4;

		public bool IsReview { get; private set; }

		public string RTitle { get; private set; }
		public string RContent { get; private set; }

		private ReviewsInput()
		{
			this.InitializeComponent();
			STopicType.SelectedIndex = 0;

			if( global::wenku8.Config.Properties.REVIEWS_SIGN == null )
			{
				StringResources stx = new StringResources( "Settings" );
				global::wenku8.Config.Properties.REVIEWS_SIGN
					= stx.Text( "Account_Reviews_Post_Sign_Default" );
			}

			Sign.Text = global::wenku8.Config.Properties.REVIEWS_SIGN;
		}

		public ReviewsInput( BookItem B )
			:this()
		{
			IsReview = false;

			StringResources stx = new StringResources( "AppBar" );
			Title.Text = stx.Str( "AddComment" );
		}

		public ReviewsInput( Review R )
			:this()
		{
			IsReview = true;

			StringResources stx = new StringResources( "AppBar" );
			Title.Text = stx.Text( "Reply" );
			TitleSection.Visibility = Visibility.Collapsed;
		}

		public async Task<bool> Validate()
		{
			global::wenku8.SelfCencorship SS = new global::wenku8.SelfCencorship();

			string Cont = "";

			Editor.Document.GetText( Windows.UI.Text.TextGetOptions.None, out Cont );
			Cont = Cont.Trim();

			if ( !await SS.Passed( Cont ) ) return false;

			string Title = BTitle.Text.Trim();
			if ( !await SS.Passed( Title ) ) return false;

			string Msg;
			StringResources stx = new StringResources();

			if( !IsReview && Title.Length < MinTitleLimit )
			{
				Msg = stx.Text( "Reviews_MinLimit" )
					+ stx.Text( "Desc_Reviews_Title_A" )
					+ MinTitleLimit.ToString()
					+ stx.Text( "Desc_Reviews_Title_B" )
					;

				await Popups.ShowDialog(
					new Windows.UI.Popups.MessageDialog( Msg )
				);

				return false;
			}

			if( Cont.Length < MinContentLimit )
			{
				Msg = stx.Text( "Reviews_MinLimit" )
					+ stx.Text( "Desc_Reviews_Title_A" )
					+ MinContentLimit.ToString()
					+ stx.Text( "Desc_Reviews_Content_B" )
					;
				await Popups.ShowDialog(
					new Windows.UI.Popups.MessageDialog( Msg )
				);
				return false;
			}

			RTitle = GetPrefix() + Title;
			RContent = Cont + GetSuffix();

			return true;
		}

		private void STopicType_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if ( e.AddedItems.Count() < 1 ) return;
			SetPlaceHolders();
		}

		private void SetPlaceHolders()
		{
			if ( BTitle == null ) return;

			StringResources stx = new StringResources();
			switch ( STopicType.SelectedIndex )
			{
				case 0:
					MinTitleLimit = 4;
					MinContentLimit = 4;
					BTitle.PlaceholderText = stx.Text( "Desc_Reviews_Title_A" )
						+ MinTitleLimit.ToString()
						+ stx.Text( "Desc_Reviews_Title_B" )
						;
					break;
				case 1:
					MinTitleLimit = 0;
					MinContentLimit = 12;
					BTitle.PlaceholderText = stx.Text( "Desc_Reviews_Title_Optional" );
					break;
				case 2:
					MinTitleLimit = 2;
					MinContentLimit = 12;
					BTitle.PlaceholderText = stx.Text( "Desc_Reviews_Title_A" )
						+ MinTitleLimit.ToString()
						+ stx.Text( "Desc_Reviews_Title_B" )
						;
					break;
			}

			Editor.PlaceholderText =
				stx.Text( "Desc_Reviews_Title_A" )
				+ MinContentLimit.ToString()
				+ stx.Text( "Desc_Reviews_Content_B" );
		}

		private string GetPrefix()
		{
			StringResources stx = new StringResources();
			switch ( STopicType.SelectedIndex )
			{
				case 1:
					return "[" + stx.Text( "Reviews_Topic_Type1" ) +"] ";
				case 2:
					return "[" + stx.Text( "Reviews_Topic_Type2" ) + "] ";
			}

			return "";
		}

		private string GetSuffix()
		{
			return string.IsNullOrEmpty( Sign.Text ) ? "" : ( "\n\n" + Sign.Text );
		}

	}
}