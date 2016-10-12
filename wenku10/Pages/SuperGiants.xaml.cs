using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Effects.P2DFlow;

namespace wenku10.Pages
{
    public sealed partial class SuperGiants : Page
    {
        List<Scenes.FireFlies> FireFlies;

        public SuperGiants()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        private void FloatyButton_Loaded( object sender, RoutedEventArgs e )
        {
            FloatyButton Floaty = ( ( FloatyButton ) sender );
            Floaty.BindTimer( NTimer.Instance );

            Floaty.TextSpeed = NTimer.RandDouble( -2, 2 );
        }

        private void SetTemplate()
        {
            NTimer.Instance.Start();
            FireFlies = new List<Scenes.FireFlies>() {
                new Scenes.FireFlies( Stage1 )
                , new Scenes.FireFlies( Stage2 )
                , new Scenes.FireFlies( Stage3 )
                , new Scenes.FireFlies( Stage4 )
            };

            Stack<Particle> PStack = new Stack<Particle>();

            int l = MainStage.Instance.IsPhone ? 100 : 500;
 
            for ( int i = 0; i < l; i++ )
                PStack.Push( new Particle() );

            foreach ( Scenes.FireFlies FF in FireFlies )
                FF.Start( PStack );

            // Scenes.MagicTrails LoadingTrails = new Scenes.MagicTrails( Stage1, FireFlies.First().Textures );
            // LoadingTrails.Start();

            LayoutRoot.ViewChanged += LayoutRoot_ViewChanged;

            Unloaded += SuperGiants_Unloaded;
        }

        private float PrevOffset = 0;

        private void LayoutRoot_ViewChanged( object sender, ScrollViewerViewChangedEventArgs e )
        {
            float CurrOffset = ( float ) LayoutRoot.VerticalOffset;
            FireFlies?.ForEach( x => x.WindBlow( CurrOffset - PrevOffset ) );
            PrevOffset = CurrOffset;
        }

        private void SuperGiants_Unloaded( object sender, RoutedEventArgs e )
        {
            Stage1.RemoveFromVisualTree();
            Stage2.RemoveFromVisualTree();
            Stage3.RemoveFromVisualTree();
            Stage4.RemoveFromVisualTree();
            Stage1 = Stage2 = Stage3 = Stage4 = null;

            FireFlies.ForEach( x => x.Dispose() );
            FireFlies = null;
        }

    }
}