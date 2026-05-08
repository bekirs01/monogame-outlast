namespace Outlast2D;

/// <summary>Sandık (karo 3) açılınca verilen ödül türü.</summary>
public enum ChestRewardKind : byte
{
    None = 0,
    Key = 1,
    Lantern = 2,
    Ammo = 3,
    /// <summary>Hız jetonu: koşu/grid adım sıklığı iki katına çıkar.</summary>
    SpeedToken = 4,
}
