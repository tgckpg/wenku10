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
            , BOOK_SPIDER_VIEW = "BookSpiderView"
            , ZONE_SPIDER_VIEW = "ZoneSpiderView"
            , LOCAL_DOCS_VIEW = "LocalDocsView"
            , ONLINE_SCRIPTS_VIEW = "OnlineScriptsView"
            , HISTORY = "History"
            , PROC_PANEL = "ProceduresPanel"
            , CONTENT_READER = "ContentReader"
            , SCRIPT_DETAILS = "ScriptDetails"
            , MAIN_SETTINGS = "MainSettings"
            , SH_USER_INFO = "SHUserInfo"
            , ABOUT = "About"

            , W_BOOKSHELF = "WBookshelf"
            , W_COMMENT = "WComment"
            , W_SEARCH = "WSearch"
            , W_USER_INFO = "WUserInfo"
            , W_NAV_SEL = "WNavSelections"
            , W_NAV_LIST = "WNavList-"

            , SG_W = "SuperGiants-W"
            , SG_SH = "SuperGiants-SH"

            , NULL = "Null"
        ;

        // Cannot be stored in backstacks
        public static readonly string[] NonStackables = new string[] { CONTENT_READER };

        // Can only have single instance in stack
        public static readonly string[] MonoStack = new string[] { BOOK_INFO_VIEW };

    }
}