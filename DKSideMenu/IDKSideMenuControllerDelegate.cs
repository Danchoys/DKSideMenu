using System;

namespace DKSideMenu
{
	public interface IDKSideMenuControllerDelegate
	{
		bool ShouldPopFromViewControlleStack { get; }
	}
}

