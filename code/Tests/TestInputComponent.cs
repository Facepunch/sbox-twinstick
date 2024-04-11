public sealed class TestInputComponent : Component
{
	protected override void OnUpdate()
	{
		using ( Input.PlayerScope( 1 ) )
		{
			if ( Input.Pressed( "Run" ) )
			{
				Log.Info( "Run was pressed on player 1" );
			}
		}

		using ( Input.PlayerScope( 0 ) )
		{
			if ( Input.Pressed( "Run" ) )
			{
				Log.Info( "Run was pressed on player 0" );
			}
		}
	}
}
