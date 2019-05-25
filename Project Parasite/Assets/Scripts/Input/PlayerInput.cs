using UnityEngine;

public class PlayerInput : InputSource
{

	public PlayerInput() : base() {
		shouldCameraFollowOwner = true;
	}

    #region [Public Methods]

    public override void UpdateInputState() {
        base.UpdateInputState();
		state[Key.right] = Input.GetKey(KeyCode.D);
		state[Key.left] = Input.GetKey(KeyCode.A);
		state[Key.up] = Input.GetKey(KeyCode.W);
		state[Key.down] = Input.GetKey(KeyCode.S);
		state[Key.action1] = Input.GetMouseButton(0);
		state[Key.action2] = Input.GetMouseButton(1);
		state[Key.interact] = Input.GetKey(KeyCode.E);
    }

    #endregion

}
