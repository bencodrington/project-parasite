public class StationaryNpcInput : DefaultNpcInput
{
    // CLEANUP: Default should inherit from stationary instead of vice versa, since
    // CLEANUP:     it ADDS behaviour instead of removing it

    #region [Public Methods]
    
    public override void StartIdling() {
        // Never start idling
        return;
    }
    
    #endregion

}
