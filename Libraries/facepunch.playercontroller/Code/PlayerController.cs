using Sandbox.Citizen;

[Group( "Walker" )]
[Title( "Walker - Player Controller" )]
public sealed class PlayerController : Component
{
	[Property] public CharacterController CharacterController { get; set; }
	[Property] public float CrouchMoveSpeed { get; set; } = 64.0f;
	
	[Property] public float WalkMoveSpeed { get; set; } = 190.0f;
	[Property] public float RunMoveSpeed { get; set; } = 190.0f;
	[Property] public float CrouchSprintMoveSpeed { get; set; } = 95.0f;
	[Property] public float SprintMoveSpeed { get; set; } = 320.0f;
	[Property] public float MaxSpeed { get; set; } = 320.0f;
	[Property] private CameraComponent camera { get; set; }
	[Property] float MinFOV = 90;
	[Property] float MaxFOV = 120;
	[Property] float FOVTransitionSpeed = 2f;

	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }

	[Sync] public bool Crouching { get; set; }
	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public Vector3 WishVelocity { get; set; }

	public bool WishCrouch;
	public float EyeHeight = 64;

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			MouseInput();
			Transform.Rotation = new Angles( 0, EyeAngles.yaw, 0 );
		}

		UpdateAnimation();
		UpdateFOV();
		void UpdateFOV()
{
  		// you can Clamp the CurrentSpeed/MaxSpeed between 0 and 1 if you don't want to go over 105 for the fov value
  		var targetFOV = MathX.Lerp(MinFOV, MaxFOV, CurrentMoveSpeed/MaxSpeed, false);
  		var dynamicFOV = MathX.Lerp(camera.FieldOfView, targetFOV, Time.Delta * FOVTransitionSpeed);
  		camera.FieldOfView = dynamicFOV;
}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		CrouchingInput();
		MovementInput();
	}

	protected override void OnStart() {
	Transform.World = Random.Shared.FromArray( Game.ActiveScene.GetAllComponents<SpawnPoint>().Select( x => x.Transform.World ).ToArray(), global::Transform.Zero );

	
}

	private void MouseInput()
	{
		var e = EyeAngles;
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp( -90, 90 );
		e.roll = 0.0f;
		EyeAngles = e;
	}

	float CurrentMoveSpeed
	{
		get
		{
			if(Crouching)
			{
 				 if(Input.Down("run")) return CrouchSprintMoveSpeed;

  				return CrouchMoveSpeed;
			}
			if ( Input.Down( "run" ) ) return SprintMoveSpeed;
			if ( Input.Down( "walk" ) ) return WalkMoveSpeed;

			return RunMoveSpeed;
		}
	}



	RealTimeSince lastGrounded;
	RealTimeSince lastUngrounded;
	RealTimeSince lastJump;

	float GetFriction()
	{
		if ( CharacterController.IsOnGround ) return 6.0f;

		// air friction
		return 0.2f;
	}

	private void MovementInput()
	{
		if ( CharacterController is null )
			return;

		var cc = CharacterController;

		Vector3 halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		WishVelocity = Input.AnalogMove;

		if ( lastGrounded < 0.2f && lastJump > 0.3f && Input.Pressed( "jump" ) )
		{
			lastJump = 0;
			cc.Punch( Vector3.Up * 300 );
		}

		if ( !WishVelocity.IsNearlyZero() )
		{
			WishVelocity = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation() * WishVelocity;
			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.ClampLength( 1 );
			WishVelocity *= CurrentMoveSpeed;

			if ( !cc.IsOnGround )
			{
				WishVelocity = WishVelocity.ClampLength( 50 );
			}
		}


		cc.ApplyFriction( GetFriction() );

		if ( cc.IsOnGround )
		{
			cc.Accelerate( WishVelocity );
			cc.Velocity = CharacterController.Velocity.WithZ( 0 );
		}
		else
		{
			cc.Velocity += halfGravity;
			cc.Accelerate( WishVelocity );

		}

		//
		// Don't walk through other players, let them push you out of the way
		//
		var pushVelocity = PlayerPusher.GetPushVector( Transform.Position + Vector3.Up * 40.0f, Scene, GameObject );
		if ( !pushVelocity.IsNearlyZero() )
		{
			var travelDot = cc.Velocity.Dot( pushVelocity.Normal );
			if ( travelDot < 0 )
			{
				cc.Velocity -= pushVelocity.Normal * travelDot * 0.6f;
			}

			cc.Velocity += pushVelocity * 128.0f;
		}

		cc.Move();

		if ( !cc.IsOnGround )
		{
			cc.Velocity += halfGravity;
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
		}

		if ( cc.IsOnGround )
		{
			lastGrounded = 0;
		}
		else
		{
			lastUngrounded = 0;
		}
	}
	float DuckHeight = (64 - 36);

	bool CanUncrouch()
	{
		if ( !Crouching ) return true;
		if ( lastUngrounded < 0.2f ) return false;

		var tr = CharacterController.TraceDirection( Vector3.Up * DuckHeight );
		return !tr.Hit; // hit nothing - we can!
	}

	public void CrouchingInput()
	{
		WishCrouch = Input.Down( "duck" );

		if ( WishCrouch == Crouching )
			return;

		// crouch
		if ( WishCrouch )
		{
			CharacterController.Height = 36;
			Crouching = WishCrouch;

			// if we're not on the ground, slide up our bbox so when we crouch
			// the bottom shrinks, instead of the top, which will mean we can reach
			// places by crouch jumping that we couldn't.
			if ( !CharacterController.IsOnGround )
			{
				CharacterController.MoveTo( Transform.Position += Vector3.Up * DuckHeight, false );
				Transform.ClearLerp();
				EyeHeight -= DuckHeight;
			}

			return;
		}

		// uncrouch
		if ( !WishCrouch )
		{
			if ( !CanUncrouch() ) return;

			CharacterController.Height = 64;
			Crouching = WishCrouch;
			return;
		}


	}

	private void UpdateCamera()
	{
		var camera = Scene.GetAllComponents<CameraComponent>().Where( x => x.IsMainCamera ).FirstOrDefault();
		if ( camera is null ) return;

		var targetEyeHeight = Crouching ? 28 : 64;
		EyeHeight = EyeHeight.LerpTo( targetEyeHeight, RealTime.Delta * 10.0f );

		var targetCameraPos = Transform.Position + new Vector3( 0, 0, EyeHeight );

		// smooth view z, so when going up and down stairs or ducking, it's smooth af
		if ( lastUngrounded > 0.2f )
		{
			targetCameraPos.z = camera.Transform.Position.z.LerpTo( targetCameraPos.z, RealTime.Delta * 25.0f );
		}

		camera.Transform.Position = targetCameraPos;
		camera.Transform.Rotation = EyeAngles;
	}


	protected override void OnPreRender()
	{
		UpdateBodyVisibility();

		if ( IsProxy )
			return;

		UpdateCamera();
	}

	private void UpdateAnimation()
	{
		if ( AnimationHelper is null ) return;

		var wv = WishVelocity.Length;

		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( CharacterController.Velocity );
		AnimationHelper.IsGrounded = CharacterController.IsOnGround;
		AnimationHelper.DuckLevel = Crouching ? 1.0f : 0.0f;

		AnimationHelper.MoveStyle = wv < 160f ? CitizenAnimationHelper.MoveStyles.Walk : CitizenAnimationHelper.MoveStyles.Run;

		var lookDir = EyeAngles.ToRotation().Forward * 1024;
		AnimationHelper.WithLook( lookDir, 1, 0.5f, 0.25f );
	}

	private void UpdateBodyVisibility()
	{
		if ( AnimationHelper is null )
			return;

		var renderMode = ModelRenderer.ShadowRenderType.On;
		if ( !IsProxy ) renderMode = ModelRenderer.ShadowRenderType.ShadowsOnly;

		AnimationHelper.Target.RenderType = renderMode;

		foreach ( var clothing in AnimationHelper.Target.Components.GetAll<ModelRenderer>( FindMode.InChildren ) )
		{
			if ( !clothing.Tags.Has( "clothing" ) )
				continue;

			clothing.RenderType = renderMode;
		}
	}

}
