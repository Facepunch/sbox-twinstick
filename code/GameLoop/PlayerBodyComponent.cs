public sealed class PlayerBodyComponent : Component
{
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public HighlightOutline Outliner { get; set; }

	static private Color[] PlayerColors = new[]
	{
		Color.Red,
		Color.Green,
		Color.Blue,
		Color.White
	};

	internal void SetPlayerId( int playerId )
	{
		Outliner.Color = PlayerColors[playerId];
		Outliner.InsideColor = Outliner.Color.WithAlpha( 0.25f );
	}
}
