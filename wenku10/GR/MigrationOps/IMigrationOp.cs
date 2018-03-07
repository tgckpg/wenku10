using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GR.MigrationOps
{
	interface IMigrationOp
	{
		bool ShouldMigrate { get; set; }
		Action<string> Mesg { get; set; }
		Action<string> MesgR { get; set; }

		Task Up();
	}
}