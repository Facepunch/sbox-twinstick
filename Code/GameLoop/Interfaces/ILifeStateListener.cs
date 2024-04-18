using static Twinstick.HealthComponent;

namespace Twinstick;

/// <summary>
/// When life state changes, we can listen for changes on components in the same tree.
/// </summary>
public interface ILifeStateListener
{
	public void OnLifeStateChanged( LifeState before, LifeState after );
}
