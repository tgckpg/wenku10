using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Graphics.Canvas;

using GR.Effects.P2DFlow;
using GR.Effects.P2DFlow.ForceFields;

namespace wenku10.Scenes
{
	abstract class PFScene : IDisposable
	{
		protected bool ShowWireFrame = false;

		protected PFSimulator PFSim = new PFSimulator();

		virtual public void Dispose()
		{
			try
			{
				lock ( PFSim )
				{
					PFSim.Reapers.Clear();
					PFSim.Fields.Clear();
					PFSim.Spawners.Clear();
				}

				PFSim = null;
			}
			catch( Exception ) { }
		}

		virtual public void Enter() { }

		protected void DrawWireFrames( CanvasDrawingSession ds )
		{
#if DEBUG
			lock ( PFSim )
			{
				if ( ShowWireFrame )
				{
					foreach ( IForceField IFF in PFSim.Fields )
					{
						IFF.WireFrame( ds );
						IFF.FreeWireFrame();
					}
				}
				else
				{
					foreach ( IForceField IFF in PFSim.Fields )
						IFF.FreeWireFrame();
				}
			}
#endif
		}
	}
}