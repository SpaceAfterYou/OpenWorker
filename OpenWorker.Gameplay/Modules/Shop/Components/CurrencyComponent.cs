using OpenWorker.Domain.Attributes;
using OpenWorker.Domain.Enums;
using OpenWorker.Hotspot.Enums;
using OpenWorker.Hotspot.Modules.Shop.Enums;

namespace OpenWorker.Gameplay.Modules.Shop.Components;

[EntityComponent(EntityComponentService.District)]
public readonly record struct CurrencyComponent()
{
    private readonly long[] _values = new long[(int)ShopCurrency.Max];

    public long Gold
    {
        get => this[ShopCurrency.Gold];
        set => this[ShopCurrency.Gold] = value;
    }

    public long BattlePoint
    {
        get => this[ShopCurrency.Bp];
        init => this[ShopCurrency.Bp] = value;
    }

    public long Cash
    {
        get => this[ShopCurrency.Cash];
        init => this[ShopCurrency.Cash] = value;
    }

    public long Token
    {
        get => this[ShopCurrency.Token];
        init => this[ShopCurrency.Token] = value;
    }

    public long Ether
    {
        get => this[ShopCurrency.Ether];
        init => this[ShopCurrency.Ether] = value;
    }

    public long Recycle
    {
        get => this[ShopCurrency.Recycle];
        init => this[ShopCurrency.Recycle] = value;
    }

    public long TokenXeno
    {
        get => this[ShopCurrency.TokenXeno];
        init => this[ShopCurrency.TokenXeno] = value;
    }

    public long CashMileageAkashic
    {
        get => this[ShopCurrency.CashMileageAkashic];
        init => this[ShopCurrency.CashMileageAkashic] = value;
    }

    public long CashMileageBroach
    {
        get => this[ShopCurrency.CashMileageBroach];
        init => this[ShopCurrency.CashMileageBroach] = value;
    }

    public long CashMileageTag
    {
        get => this[ShopCurrency.CashMileageTag];
        init => this[ShopCurrency.CashMileageTag] = value;
    }

    public long this[ShopCurrency index]
    {
        get => this[(int)index];
        set => this[(int)index] = value;
    }
    
    public long this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }
}
