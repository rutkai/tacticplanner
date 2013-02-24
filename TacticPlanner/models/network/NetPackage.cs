using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TacticPlanner.models.network {
	[Serializable]
	class NetPackage {
		public NetPackageTypes contentType;
		public string sender;
		public object content;

		public NetPackage(NetPackageTypes type, string sender, object content) {
			contentType = type;
			this.sender = sender;
			this.content = content;
		}
	}

	enum NetPackageTypes {
		Tactic,
		ClientList,
		Settings,
		Ping,
		SetTimer,
		ShowStatic,
		ShowDynamic,
		ShowPlayStatic,
        ShowPlayDynamic,
		DrawPoints,
		DrawEraserPoints,
		DrawLine,
		DrawArrow,
		DrawStamp,
		ResetDrawAt,
		CloneDrawAt,
		ReloadDynamic,
		StaticTimer,
		DynamicTimer
	}
}
