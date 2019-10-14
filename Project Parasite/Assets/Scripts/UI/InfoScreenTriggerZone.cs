using System;

public class InfoScreenTriggerZone : TriggerZone
{
    #region [Private Variables]
    
    Action triggerHandlers;
    
    #endregion

    #region [Public Methods]
    
    public void RegisterOnTriggerCallback(Action cb) {
        triggerHandlers += cb;
    }

    public void UnregisterOnTriggerCallback(Action cb) {
        triggerHandlers -= cb;
    }
    
    #endregion

    protected override void OnTrigger() {
        triggerHandlers();
    }
}
