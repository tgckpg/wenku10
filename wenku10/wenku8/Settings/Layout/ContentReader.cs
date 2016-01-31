using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.IO;

namespace wenku8.Settings.Layout
{
    using Resources;
    public class ContentReader 
    {
        private const string TFileName = FileLinks.ROOT_SETTING + FileLinks.LAYOUT_CONTREADER;
        private const string RightToLeft = "RightToLeft";
        private const string Horizontal = "IsHorizontal";

        private XRegistry LayoutSettings;

        public bool IsHorizontal
        {
            get
            {
                return LayoutSettings.GetParameter( Horizontal ).GetBool( "enable" );
            }
            set
            {
                LayoutSettings.SetParameter( Horizontal, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        public bool IsRightToLeft
        {
            get
            {
                return LayoutSettings.GetParameter( RightToLeft ).GetBool( "enable" );
            }
            set
            {
                LayoutSettings.SetParameter( RightToLeft, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        public ContentReader()
        {
			LayoutSettings = new XRegistry( AppKeys.TS_CXML, TFileName );
            InitParams();
        }

        private void InitParams()
        {
            if( LayoutSettings.GetParameter( Horizontal ) == null )
            {
                IsHorizontal = Shared.LocaleDefaults.Get<bool>( "ContentReader.IsHorizontal" );
            }

            if( LayoutSettings.GetParameter( RightToLeft ) == null )
            {
                IsRightToLeft = Shared.LocaleDefaults.Get<bool>( "ContentReader.IsRightToLeft" );
            }
        }
    }
}
