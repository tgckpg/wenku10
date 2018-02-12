using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wenku10.Pages
{
	static class PageId
	{
		public const string
			BOOK_INFO_VIEW = "BookInfoView"
			, MASTER_EXPLORER = "MasterExplorer"
			, ONLINE_SCRIPTS_VIEW = "OnlineScriptsView"
			, MANAGE_PINS = "ManagePins"
			, PROC_PANEL = "ProceduresPanel"
			, CONTENT_READER_H = "ContentReaderHorz"
			, CONTENT_READER_V = "ContentReaderVert"
			, SCRIPT_DETAILS = "ScriptDetails"
			, MAIN_SETTINGS = "MainSettings"
			, SH_USER_INFO = "SHUserInfo"
			, ABOUT = "About"

			, W_COMMENT = "WComment"
			, W_USER_INFO = "WUserInfo"
			, W_DEATHBLOW = "WDeathblow"

			, SG_W = "SuperGiants-W"
			, SG_SH = "SuperGiants-SH"

			, MONO_REDIRECTOR = "MonoRedirector"

			, NULL = "Null"
		;

		// Cannot be stored in backstacks
		public static readonly string[] NonStackables = new string[] { CONTENT_READER_H, CONTENT_READER_V, W_DEATHBLOW, MONO_REDIRECTOR };

		// Can only have single instance in stack
		public static readonly string[] MonoStack = new string[] { BOOK_INFO_VIEW, SCRIPT_DETAILS };

	}
}