using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;

namespace wenku8.Model.Pages.ContentReader
{
    using Config;
    class ClockContext : ActiveData, IDisposable
    {
        public SolidColorBrush HHBrush { get; private set; }
        public SolidColorBrush MHBrush { get; private set; }
        public SolidColorBrush SBrush { get; private set; }

        public ScaleTransform RenderTransform { get; private set; }

        public ClockContext()
        {
            AppSettings.PropertyChanged += AppSettings_PropertyChanged;

            RenderTransform = new ScaleTransform();
            RenderTransform.ScaleX = RenderTransform.ScaleY = 1.5;

            UpdateBrush( Parameters.APPEARANCE_CONTENTREADER_CLOCK_HHCOLOR );
            UpdateBrush( Parameters.APPEARANCE_CONTENTREADER_CLOCK_MHCOLOR );
            UpdateBrush( Parameters.APPEARANCE_CONTENTREADER_CLOCK_SCOLOR );
        }

        ~ClockContext() { Dispose(); }

        public void Dispose()
        {
            AppSettings.PropertyChanged -= AppSettings_PropertyChanged;
        }

        private void AppSettings_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            UpdateBrush( e.PropertyName );
        }

        private void UpdateBrush( string Choice )
        {
            switch ( Choice )
            {
                case Parameters.APPEARANCE_CONTENTREADER_CLOCK_HHCOLOR:
                    HHBrush = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_CLOCK_HHCOLOR );
                    NotifyChanged( "HHBrush" );
                    break;
                case Parameters.APPEARANCE_CONTENTREADER_CLOCK_MHCOLOR:
                    MHBrush = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_CLOCK_MHCOLOR );
                    NotifyChanged( "MHBrush" );
                    break;
                case Parameters.APPEARANCE_CONTENTREADER_CLOCK_SCOLOR:
                    SBrush = new SolidColorBrush( Properties.APPEARANCE_CONTENTREADER_CLOCK_SCOLOR );
                    NotifyChanged( "SBrush" );
                    break;
            }
        }
    }
}
