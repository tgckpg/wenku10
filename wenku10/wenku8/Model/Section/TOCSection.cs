using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Linq;
using Net.Astropenguin.UI.Icons;

namespace wenku8.Model.Section
{
    using Book;
    using Config;
    using Storage;
    using Settings.Layout;
    using ThemeIcons;

    sealed class TOCSection : ActiveData
    {
        public bool AnchorAvailable
        {
            get { return AutoAnchor != null; }
        }

        public Grid ToggleDir { get; private set; }

        public Volume[] Volumes { get; private set; }
        public Chapter[] Chapters { get; private set; }
        public Chapter AutoAnchor { get; private set; }

        public Converters.TOCTemplateSelector TemplateSelector { get; set; }
        public IObservableVector<object> VolumeCollections { get; private set; }

        private BookItem CurrentBook;
        private ContentReader CRSettings;

        private int DirMode;
        private int InitMode;

        public TOCSection( BookItem b )
        {
            TemplateSelector = new Converters.TOCTemplateSelector();
            CRSettings = new ContentReader();

            CurrentBook = b;
            Volumes = b.GetVolumes();

            if ( Properties.MISC_CHUNK_SINGLE_VOL )
                VirtualizeVolumes();

            ToggleDir = new Grid();
            ToggleDir.FlowDirection = FlowDirection.LeftToRight;

            SetAutoAnchor();

            SetDirection();
            DirMode = InitMode;
        }

        private void SetDirection()
        {
            IconNavigateArrow ArrowIcon = null;
            IconAlign AlignIcon = null;

            if ( CRSettings.IsHorizontal )
            {
                if ( CRSettings.IsRightToLeft )
                {
                    InitMode = 0;

                    ArrowIcon = new IconNavigateArrow() { AutoScale = true, Width = 20, Height = 20, Direction = Direction.Rotate180 };
                    ArrowIcon.HorizontalAlignment = HorizontalAlignment.Right;
                    AlignIcon = new IconAlign() { AutoScale = true, Direction = Direction.Rotate90 };
                }
                else
                {
                    InitMode = 1;

                    ArrowIcon = new IconNavigateArrow() { AutoScale = true, Width = 20, Height = 20 };
                    ArrowIcon.HorizontalAlignment = HorizontalAlignment.Left;
                    AlignIcon = new IconAlign() { AutoScale = true, Direction = Direction.Rotate90 | Direction.MirrorHorizontal };
                }

                ArrowIcon.VerticalAlignment = VerticalAlignment.Bottom;
            }
            else
            {
                InitMode = 2;

                ArrowIcon = new IconNavigateArrow() { AutoScale = true, Width = 20, Height = 20, Direction = Direction.Rotate90 };
                ArrowIcon.HorizontalAlignment = HorizontalAlignment.Left;
                ArrowIcon.VerticalAlignment = VerticalAlignment.Top;
                AlignIcon = new IconAlign() { AutoScale = true };
            }

            ArrowIcon.Margin = new Thickness( 5 );
            AlignIcon.Opacity = 0.5;

            ToggleDir.Children.Clear();
            ToggleDir.Children.Add( AlignIcon );
            ToggleDir.Children.Add( ArrowIcon );
        }

        // This groups 30+ ChapterList to virtual volumes for easier navigation
        private void VirtualizeVolumes()
        {
            int l = Volumes.Count();
            if ( l == 0 || !( l == 1 && 30 < Volumes.First().ChapterList.Count() ) ) return;
            Volumes = VirtualVolume.Create( Volumes.First() );
        }

        public void ToggleDirection()
        {
            switch ( ++DirMode )
            {
                case 0:
                    CRSettings.IsHorizontal = true;
                    CRSettings.IsRightToLeft = true;
                    break;
                case 1:
                    CRSettings.IsHorizontal = true;
                    CRSettings.IsRightToLeft = false;
                    break;
                case 2:
                    CRSettings.IsHorizontal = false;
                    CRSettings.IsRightToLeft = false;
                    break;
                default:
                    DirMode = 0;
                    goto case 0;
            }

            SetDirection();
        }

        public void SelectVolume( Volume v )
        {
            Chapters = v.ChapterList;
            NotifyChanged( "Chapters" );
        }

        public void SetViewSource( CollectionViewSource ViewSource )
        {
            if ( TemplateSelector.IsHorizontal ) return;

            ViewSource.Source = Volumes.Remap( x => new ChapterGroup( x ) );

            VolumeCollections = ViewSource.View.CollectionGroups;
            NotifyChanged( "VolumeCollections" );
        }

        public void SetAutoAnchor()
        {
            // Set the autoanchor
            string AnchorId = new AutoAnchor().GetBookmark( CurrentBook.Id );

            foreach ( Volume V in Volumes )
            {
                foreach ( Chapter C in V.ChapterList )
                {
                    if ( C.cid == AnchorId )
                    {
                        AutoAnchor = C;
                        goto EndLoop;
                    }
                }
            }
            EndLoop:

            NotifyChanged( "AnchorAvailable" );
        }

        internal class ChapterGroup : List<Chapter>
        {
            public Volume Vol { get; set; }

            public ChapterGroup( Volume V )
                : base( V.ChapterList )
            {
                Vol = V;
            }
        }
    }
}