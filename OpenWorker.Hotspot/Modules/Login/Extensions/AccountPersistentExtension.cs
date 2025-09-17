using OpenWorker.Domain.Persistent;
using OpenWorker.Hotspot.Modules.Login.Components;

namespace OpenWorker.Hotspot.Modules.Login.Extensions;

public static class AccountPersistentExtension
{
    public static SecondPasswordComponent ToSecondPasswordComponent(this AccountPersistent value)
    {
        return new SecondPasswordComponent
        {
            Password = value.SecondPassword
        };
    } 
    
    public static TradePasswordComponent ToTradePasswordComponent(this AccountPersistent value)
    {
        return new TradePasswordComponent
        {
            Password = value.TradePassword
        };
    }
}