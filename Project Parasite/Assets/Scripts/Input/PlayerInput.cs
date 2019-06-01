using UnityEngine;

public class PlayerInput : InputSource
{

	public PlayerInput() : base() {
		shouldCameraFollowOwner = true;
	}

    #region [Public Methods]

    public override void UpdateInputState() {
        base.UpdateInputState();
		state.keyState[Key.right] 		= Input.GetKey(KeyCode.D);
		state.keyState[Key.left] 		= Input.GetKey(KeyCode.A);
		state.keyState[Key.up] 			= Input.GetKey(KeyCode.W);
		state.keyState[Key.down] 		= Input.GetKey(KeyCode.S);
		state.keyState[Key.action1] 	= Input.GetMouseButton(0);
		state.keyState[Key.action2] 	= Input.GetMouseButton(1);
		state.keyState[Key.interact]	= Input.GetKey(KeyCode.E);
		state.keyState[Key.jump]		= Input.GetKey(KeyCode.Space);
		state.mousePosition				= Utility.GetMousePos();
    }

    #endregion

}
