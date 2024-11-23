using Sandbox;

public sealed class ManagedLight : Component
{
	[Property] public Light TargetLight
	{
		get => _targetLight;
		set
		{
			if ( value == _targetLight )
				return;
			
			if ( _targetLight.IsValid() )
			{
				_targetLight.Tags.Set( GetTagIndex, false );
			}

			_targetLight = value;
			if ( !value.IsValid() )
				return;

			Index = _nextTagIndex;
			_nextTagIndex++;
		}
	}
	private Light _targetLight;
	[Property, ReadOnly] public int Index { get; private set; }
	[Property, Range(0f, 2048f, 64f)] public int ShadowResolution 
	{
		get => _shadowResolution;
		set
		{
			_shadowResolution = value;
			UpdateShadowResolution();
		}
	}
	private int _shadowResolution;

	private static int _nextTagIndex = 0;
	private SceneLight _so;

	private string GetTagIndex => $"light_so_{Index}";

	protected override void OnUpdate()
	{
		if ( TargetLight.IsValid() && ( !_so.IsValid() || !_so.Tags.Has( GetTagIndex ) ) )
		{
			TargetLight.Tags.Set( GetTagIndex, true );
			_so = FindSceneObject();
			UpdateShadowResolution();
		}
	}

	private SceneLight FindSceneObject()
	{
		return Scene.SceneWorld.SceneObjects
				.OfType<SceneLight>()
				.FirstOrDefault( so => so.Tags.Has( GetTagIndex ) );
	} 

	private void UpdateShadowResolution()
	{
		if ( !_so.IsValid() )
			return;

		_so.ShadowTextureResolution = ShadowResolution;
	}
}
