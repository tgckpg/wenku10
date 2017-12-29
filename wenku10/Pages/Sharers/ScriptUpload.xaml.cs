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
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using GR.AdvDM;
using GR.CompositeElement;
using GR.Model.Book;
using GR.Model.Interfaces;
using GR.Model.ListItem;
using GR.Model.ListItem.Sharers;
using GR.Model.REST;
using GR.Resources;
using CryptAES = GR.GSystem.CryptAES;
using AESManager = GR.GSystem.AESManager;
using TokenManager = GR.GSystem.TokenManager;

namespace wenku10.Pages.Sharers
{
	sealed partial class ScriptUpload : Page, ICmdControls, INavPage
	{
		public static readonly string ID = typeof( ScriptUpload ).Name;

		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get; }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private SpiderBook SelectedBook;
		private BookItem BindBook;

		private AESManager AESMgr;
		private TokenManager TokMgr;

		private Action<string, string> OnExit;
		public Action Canceled;

		private KeyValuePair<string, SpiderScope>[] Scopes;

		private AppBarButtonEx UploadBtn;

		private string ReservedId;
		private bool LockedFile = false;
		private volatile bool Uploading = false;

		public ScriptUpload()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public ScriptUpload( HubScriptItem HSI, Action<string, string> OnExit )
			: this()
		{
			// Set Update template
			ReservedId = HSI.Id;

			Anon.IsChecked = string.IsNullOrEmpty( HSI.AuthorId );

			Encrypt.IsChecked = HSI.Encrypted;
			ForceCommentEnc.IsChecked = HSI.ForceEncryption;

			NameInput.Text = HSI.Name;
			DescInput.Text = HSI.Desc;

			ZoneInput.Text = string.Join( ", ", HSI.Zone );
			TypesInput.Text = string.Join( ", ", HSI.Type );
			TagsInput.Text = string.Join( ", ", HSI.Tags );

			ScopeLevel.SelectedIndex = Array.IndexOf( Scopes, Scopes.First( x => x.Value == HSI.Scope ) );

			AddToken_Btn.IsEnabled
				= AddKey_Btn.IsEnabled
				= ForceCommentEnc.IsEnabled
				= Encrypt.IsEnabled
				= Anon.IsEnabled
				= Keys.IsEnabled
				= AccessTokens.IsEnabled
				= false;

			PredefineFile( HSI.Id );

			this.OnExit = OnExit;
		}

		public ScriptUpload( Action<string, string> OnExit )
			: this()
		{
			this.OnExit = OnExit;
		}

		public ScriptUpload( BookItem Book, Action<string, string> OnExit )
			: this()
		{
			LockedFile = true;
			BindBook = Book;

			PredefineFile( Book.Id );
			this.OnExit = OnExit;
		}

		public void SoftOpen() { if ( BindBook != null ) BindBook.PropertyChanged += Book_PropertyChanged; }
		public void SoftClose() { if ( BindBook != null ) BindBook.PropertyChanged -= Book_PropertyChanged; }

		private void Book_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			BookItem B = ( BookItem ) sender;
			switch ( e.PropertyName )
			{
				case "Title":
					NameInput.PlaceholderText = B.Title;
					if ( string.IsNullOrEmpty( NameInput.Text ) ) NameInput.Text = B.Title;
					break;
				case "Intro":
					DescInput.PlaceholderText = B.Intro;
					if ( string.IsNullOrEmpty( DescInput.Text ) ) DescInput.Text = B.Intro;
					break;
				case "Press":
					ZoneInput.PlaceholderText = B.Info.Press;
					if ( string.IsNullOrEmpty( ZoneInput.Text ) ) ZoneInput.Text = B.Info.Press;
					break;
			}
		}

		private async void PredefineFile( string Id )
		{
			SelectedBook = await SpiderBook.CreateAsyncSpider( Id );
			FileName.Text = SelectedBook.MetaLocation;
		}

		private void SetTemplate()
		{
			InitAppBar();

			AESMgr = new AESManager();
			AESMgr.PropertyChanged += KeyMgr_PropertyChanged;
			TokMgr = new TokenManager();
			TokMgr.PropertyChanged += TokMgr_PropertyChanged;

			Keys.DataContext = AESMgr;
			AccessTokens.DataContext = TokMgr;

			StringResources stx = new StringResources();
			FileName.Text = stx.Text( "PickAFile" );

			Scopes = new KeyValuePair<string, SpiderScope>[]
			{
				new KeyValuePair<string, SpiderScope>( stx.Text( "HS_Book" ), SpiderScope.BOOK )
				, new KeyValuePair<string, SpiderScope>( stx.Text( "HS_Zone" ), SpiderScope.ZONE )
			};

			ScopeLevel.ItemsSource = Scopes;
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources();
			UploadBtn = UIAliases.CreateAppBarBtnEx( Symbol.Send, stx.Text( "SubmitScript" ) );
			UploadBtn.Click += Upload;

			MajorControls = new ICommandBarElement[] { UploadBtn };
		}

		private void KeyMgr_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "SelectedItem" ) Keys.SelectedItem = AESMgr.SelectedItem;
		}

		private void TokMgr_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
		{
			if ( e.PropertyName == "SelectedItem" ) AccessTokens.SelectedItem = TokMgr.SelectedItem;
		}

		private void PreSelectScope( object sender, RoutedEventArgs e )
		{
			if ( ScopeLevel.SelectedValue == null )
				ScopeLevel.SelectedIndex = 0;
		}

		private void PreSelectKey( object sender, RoutedEventArgs e )
		{
			if ( string.IsNullOrEmpty( ReservedId ) )
			{
				Keys.SelectedItem = AESMgr.SelectedItem;
			}
			else
			{
				Keys.SelectedValue = AESMgr.GetAuthById( ReservedId )?.Value;
			}
		}

		private void PreSelectToken( object sender, RoutedEventArgs e )
		{
			if ( string.IsNullOrEmpty( ReservedId ) )
			{
				AccessTokens.SelectedItem = TokMgr.SelectedItem;
			}
			else
			{
				AccessTokens.SelectedValue = TokMgr.GetAuthById( ReservedId )?.Value;
			}
		}

		private async void Upload( object sender, RoutedEventArgs e )
		{
			if ( MarkUpload() ) return;

			CryptAES Crypt = null;

			// Validate inputs
			try
			{
				Message.Text = "";
				if ( SelectedBook == null )
					throw new ValidationError( "VL_NoBook" );

				if ( Encrypt.IsChecked == true )
				{
					Crypt = Keys.SelectedItem as CryptAES;

					if ( Crypt == null )
						throw new ValidationError( "VL_NoKey" );
				}

				if ( string.IsNullOrEmpty( ReservedId ) && AccessTokens.SelectedItem == null )
					throw new ValidationError( "VL_NoToken" );

				if ( ScopeLevel.SelectedItem == null )
					throw new ValidationError( "VL_NoScope" );
			}
			catch ( ValidationError ex )
			{
				StringResources stx = new StringResources( "Error" );

				Message.Text = stx.Str( ex.Message );
				MarkNotUpload();
				return;
			}

			// Check whether the script uuid is reserved
			NameValue<string> Token = ( NameValue<string> ) AccessTokens.SelectedItem;
			if ( string.IsNullOrEmpty( ReservedId ) )
			{
				ReservedId = await ReserveId( Token.Value );
			}

			string Id = ReservedId;
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
			string[] Types = TypesInput.Text.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );
			string[] Tags = TagsInput.Text.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );

			SelectedBook.AssignId( Id );
			string Data = SelectedBook.PSettings.ToString();

			if ( Crypt != null ) Data = Crypt.Encrypt( Data );

			new RuntimeCache().POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.ScriptUpload(
					Token?.Value, Id
					, Data, Name, Desc
					, Zone, Types, Tags
					, ( SpiderScope ) ScopeLevel.SelectedValue
					, Encrypt.IsChecked == true
					, ForceCommentEnc.IsChecked == true
					, Anon.IsChecked == true )
				, ( Res, QueryId ) =>
				{
					try
					{
						JsonStatus.Parse( Res.ResponseString );
						if ( Token != null ) TokMgr.AssignId( Token.Name, Id );
						if ( Crypt != null ) AESMgr.AssignId( Crypt.Name, Id );

						var j = Dispatcher.RunIdleAsync( ( x ) => { OnExit( Id, Token?.Value ); } );
					}
					catch ( Exception ex )
					{
						ServerMessage( ex.Message );
					}

					MarkNotUpload();
				}
				, ( c1, c2, ex ) =>
				{
					ServerMessage( ex.Message );
					MarkNotUpload();
				}
				, false
			);
		}

		private async void PickFile( object sender, RoutedEventArgs e )
		{
			if ( LockedFile ) return;

			Message.Text = "";
			IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
			if ( ISF == null ) return;

			UploadBtn.IsLoading = true;

			try
			{
				SelectedBook = await SpiderBook.ImportFile( await ISF.ReadString(), false );
				if ( !SelectedBook.CanProcess )
				{
					StringResources stx = new StringResources( "ERROR" );
					throw new InvalidDataException( stx.Str( "HS_INVALID" ) );
				}

				FileName.Text = ISF.Name;
				int LDot = ISF.Name.LastIndexOf( '.' );
				NameInput.PlaceholderText = ~LDot == 0 ? ISF.Name : ISF.Name.Substring( 0, LDot );
			}
			catch ( Exception ex )
			{
				Message.Text = ex.Message;
			}

			UploadBtn.IsLoading = false;
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
					catch ( Exception ex )
					{
						Logger.Log( ID, ex.Message, LogType.WARNING );
						TCS.TrySetResult( null );
					}
				}
				, ( cache, Id, ex ) =>
				{
					Logger.Log( ID, ex.Message, LogType.WARNING );
					TCS.TrySetResult( null );
				}
				, false
			);

			return await TCS.Task;
		}

		private void ServerMessage( string Mesg )
		{
			var j = Dispatcher.RunIdleAsync( ( x ) => { Message.Text = Mesg; } );
		}

		private void AddKey( object sender, RoutedEventArgs e ) { AESMgr.NewAuth(); }
		private void AddToken( object sender, RoutedEventArgs e ) { TokMgr.NewAuth(); }

		private bool MarkUpload()
		{
			if ( Uploading ) return true;
			Uploading = true;

			var j = Dispatcher.RunIdleAsync( ( x ) =>
			{
				UploadBtn.IsLoading = true;
			} );

			return false;
		}

		private void MarkNotUpload()
		{
			Uploading = false;
			var j = Dispatcher.RunIdleAsync( ( x ) =>
			{
				UploadBtn.IsLoading = false;
			} );
		}

		private class ValidationError : Exception { public ValidationError( string Mesg ) : base( Mesg ) { } }
	}
}