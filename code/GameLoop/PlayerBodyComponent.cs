public sealed class PlayerBodyComponent : Component
{
	[Property] public ModelRenderer Renderer { get; set; }

	static private Color[] PlayerColors = new[]
	{
		Color.Red,
		Color.Green,
		Color.Blue,
		Color.White
	};

	internal void SetPlayerId( int playerId )
	{
		Renderer.Tint = PlayerColors[playerId];
	}
}
