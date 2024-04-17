using Twinstick;

public sealed class PlayerBodyComponent : Component
{
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public HighlightOutline Outliner { get; set; }
	[Property] public GameObject EffectsGameObject { get; set; }
	[Property] public GameObject BoostEffectsGameObject { get; set; }
	[Property] public GameObject UIObject { get; set; }

	PlayerManager PlayerManager => GameStateManager.Instance.PlayerManager;

	internal void SetPlayerId( int playerId )
	{
		Outliner.Color = PlayerManager.PlayerColors[playerId];
		Outliner.InsideColor = Outliner.Color.WithAlpha( 0.25f );
	}

	internal void SetShouldRender( bool shouldRender )
	{
		Renderer.Enabled = shouldRender;
		Outliner.Enabled = shouldRender;
		UIObject.Enabled = shouldRender;

		if ( !shouldRender )
		{
			BoostEffectsGameObject.Enabled = shouldRender;
			EffectsGameObject.Enabled = shouldRender;
		}
	}

	private bool isBoost = false;
	internal void SetBoosting( bool boosting )
	{
		isBoost = boosting;
		BoostEffectsGameObject.Enabled = isBoost;
		EffectsGameObject.Enabled = !isBoost;
	}
}
