using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TacticPlanner.models.network {
	[Serializable]
	class NetPackage {
		public NetPackageTypes contentType;
		public object content;
	}

	enum NetPackageTypes {
		Tactic,
		ClientList,
		AllowPing,
		DenyPing,
		AllowDraw,
		DenyDraw,
		Ping,
		SetTimer,
		ShowStatic,
		ShowDynamic,
		ShowPlay,
		AddStaticIcon,
		ModifyStaticIcon,
		RemoveStaticIcon,
		AddDynamicTank,
		ModifyDinamicTank,
		RemoveDynamicTank,
		RefreshDrawAt,
		ResetDrawAt,
		CloneDrawAt
	}
}
