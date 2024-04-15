using System;
using Twinstick;

public sealed class PlayerBodyComponent : Component
{
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public HighlightOutline Outliner { get; set; }

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
	}
}
