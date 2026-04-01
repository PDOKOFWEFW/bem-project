namespace EndpointApi.Data.Enums;

/// <summary>
/// OU bazlı politika kuralı: zorunlu yükleme, engelleme veya izin listesi.
/// </summary>
public enum PolicyRuleAction
{
    ForceInstall = 0,
    Block = 1,
    Allow = 2
}
