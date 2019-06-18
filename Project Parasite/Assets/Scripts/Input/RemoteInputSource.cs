public class RemoteInputSource : InputSource
{

    #region [Public Methods]

    public RemoteInputSource() : base() {
        // Initialize state AND oldState, so they aren't null references
        oldState = NewState();
        state = NewState();
    }

    public override void UpdateInputState() {
        // DON'T call base.UpdateInputState(), as that will create a new input state each frame
    }

    public void SetInputState(bool up, bool down, bool left, bool right) {
        oldState = state;
        state = NewState();
        state.keyState[Key.up] = up;
        state.keyState[Key.down] = down;
        state.keyState[Key.left] = left;
        state.keyState[Key.right] = right;
    }

    #endregion

}
